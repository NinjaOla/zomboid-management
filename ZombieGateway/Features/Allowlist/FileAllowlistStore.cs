using System.Text.Json;
using Microsoft.Extensions.Options;
using ZombieGateway.Infrastructure.Configuration;

namespace ZombieGateway.Features.Allowlist;

public sealed class FileAllowlistStore : IAllowlistStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly SemaphoreSlim _guard = new(1, 1);
    private readonly ILogger<FileAllowlistStore> _logger;
    private readonly string _path;
    private AllowlistState? _cache;

    public FileAllowlistStore(
        IOptions<AllowlistOptions> options,
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
        try { return (await LoadAsync(cancellationToken)).AllowedChannels.Contains(channelId); }
        finally { _guard.Release(); }
    }

    public async Task<bool> IsUserAllowedForCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadAsync(cancellationToken);
            return state.AllowedUsersByCommand.TryGetValue(Normalize(commandName), out var users) && users.Contains(userId);
        }
        finally { _guard.Release(); }
    }

    public async Task AddUserToCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadAsync(cancellationToken);
            var key = Normalize(commandName);
            if (!state.AllowedUsersByCommand.TryGetValue(key, out var users))
            {
                users = [];
                state.AllowedUsersByCommand[key] = users;
            }
            users.Add(userId);
            await SaveAsync(state, cancellationToken);
        }
        finally { _guard.Release(); }
    }

    public async Task RemoveUserFromCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadAsync(cancellationToken);
            var key = Normalize(commandName);
            if (state.AllowedUsersByCommand.TryGetValue(key, out var users))
            {
                users.Remove(userId);
                if (users.Count == 0) state.AllowedUsersByCommand.Remove(key);
            }
            await SaveAsync(state, cancellationToken);
        }
        finally { _guard.Release(); }
    }

    public async Task AddChannelAsync(ulong channelId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try { var s = await LoadAsync(cancellationToken); s.AllowedChannels.Add(channelId); await SaveAsync(s, cancellationToken); }
        finally { _guard.Release(); }
    }

    public async Task RemoveChannelAsync(ulong channelId, CancellationToken cancellationToken)
    {
        await _guard.WaitAsync(cancellationToken);
        try { var s = await LoadAsync(cancellationToken); s.AllowedChannels.Remove(channelId); await SaveAsync(s, cancellationToken); }
        finally { _guard.Release(); }
    }

    private async Task<AllowlistState> LoadAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null) return _cache;

        if (!File.Exists(_path))
        {
            _cache = new AllowlistState();
            await SaveAsync(_cache, cancellationToken);
            return _cache;
        }

        await using var stream = File.OpenRead(_path);
        _cache = await JsonSerializer.DeserializeAsync<AllowlistState>(stream, SerializerOptions, cancellationToken)
            ?? new AllowlistState();
        _cache.AllowedUsersByCommand = new Dictionary<string, HashSet<ulong>>(
            _cache.AllowedUsersByCommand, StringComparer.OrdinalIgnoreCase);
        return _cache;
    }

    private async Task SaveAsync(AllowlistState state, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
        _logger.LogInformation("Persisted allowlist to {Path}", _path);
    }

    private static string Normalize(string cmd) => cmd.Trim().TrimStart('/').ToLowerInvariant();
}
