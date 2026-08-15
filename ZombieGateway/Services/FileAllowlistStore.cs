using System.Text.Json;
using Microsoft.Extensions.Options;
using ZombieGateway.Options;

namespace ZombieGateway.Services;

public sealed class FileAllowlistStore : IAllowlistStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _guard = new(1, 1);
    private readonly ILogger<FileAllowlistStore> _logger;
    private readonly string _path;
    private AllowlistState? _cache;

    public FileAllowlistStore(
        IOptions<AuthorizationOptions> options,
        IWebHostEnvironment environment,
        ILogger<FileAllowlistStore> logger)
    {
        _logger = logger;
        var configuredPath = options.Value.StoragePath;
        _path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }

    public async Task<bool> IsChannelAllowedAsync(ulong channelId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await GetOrLoadAsync(cancellationToken);
            return state.AllowedChannels.Contains(channelId);
        }
        finally
        {
            _guard.Release();
        }
    }

    public async Task<bool> IsUserAllowedForCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await GetOrLoadAsync(cancellationToken);
            var normalized = NormalizeCommand(commandName);
            return state.AllowedUsersByCommand.TryGetValue(normalized, out var users) && users.Contains(userId);
        }
        finally
        {
            _guard.Release();
        }
    }

    public async Task AddUserToCommandAllowlistAsync(string commandName, ulong userId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await GetOrLoadAsync(cancellationToken);
            var normalized = NormalizeCommand(commandName);

            if (!state.AllowedUsersByCommand.TryGetValue(normalized, out var users))
            {
                users = [];
                state.AllowedUsersByCommand[normalized] = users;
            }

            users.Add(userId);
            await PersistAsync(state, cancellationToken);
        }
        finally
        {
            _guard.Release();
        }
    }

    public async Task RemoveUserFromCommandAllowlistAsync(string commandName, ulong userId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await GetOrLoadAsync(cancellationToken);
            var normalized = NormalizeCommand(commandName);

            if (state.AllowedUsersByCommand.TryGetValue(normalized, out var users))
            {
                users.Remove(userId);

                if (users.Count == 0)
                {
                    state.AllowedUsersByCommand.Remove(normalized);
                }
            }

            await PersistAsync(state, cancellationToken);
        }
        finally
        {
            _guard.Release();
        }
    }

    public async Task AddAllowedChannelAsync(ulong channelId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await GetOrLoadAsync(cancellationToken);
            state.AllowedChannels.Add(channelId);
            await PersistAsync(state, cancellationToken);
        }
        finally
        {
            _guard.Release();
        }
    }

    public async Task RemoveAllowedChannelAsync(ulong channelId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await GetOrLoadAsync(cancellationToken);
            state.AllowedChannels.Remove(channelId);
            await PersistAsync(state, cancellationToken);
        }
        finally
        {
            _guard.Release();
        }
    }

    private async Task<AllowlistState> GetOrLoadAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        if (!File.Exists(_path))
        {
            _cache = new AllowlistState();
            await PersistAsync(_cache, cancellationToken);
            return _cache;
        }

        await using var stream = File.OpenRead(_path);
        _cache = await JsonSerializer.DeserializeAsync<AllowlistState>(stream, SerializerOptions, cancellationToken)
            ?? new AllowlistState();

        _cache.AllowedUsersByCommand = new Dictionary<string, HashSet<ulong>>(
            _cache.AllowedUsersByCommand,
            StringComparer.OrdinalIgnoreCase);

        return _cache;
    }

    private async Task PersistAsync(AllowlistState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
        _logger.LogInformation("Persisted allowlist to {Path}", _path);
    }

    private static string NormalizeCommand(string commandName) => commandName.Trim().TrimStart('/').ToLowerInvariant();
}
