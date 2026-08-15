using Discord;
using Discord.Interactions;
using ZombieGateway.Services;

namespace ZombieGateway.Discord;

public sealed class ZomboidInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly HashSet<string> UserCommandNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "start",
        "stop",
        "status",
        "players"
    };

    private readonly ICommandAuthorizationService _authorizationService;
    private readonly ISystemdServiceController _systemd;
    private readonly IZomboidRconClient _rconClient;
    private readonly IAllowlistStore _allowlistStore;
    private readonly ILogger<ZomboidInteractionModule> _logger;

    public ZomboidInteractionModule(
        ICommandAuthorizationService authorizationService,
        ISystemdServiceController systemd,
        IZomboidRconClient rconClient,
        IAllowlistStore allowlistStore,
        ILogger<ZomboidInteractionModule> logger)
    {
        _authorizationService = authorizationService;
        _systemd = systemd;
        _rconClient = rconClient;
        _allowlistStore = allowlistStore;
        _logger = logger;
    }

    [SlashCommand("start", "Starts the zomboid server if it is offline.")]
    public async Task StartServerAsync()
    {
        var auth = await AuthorizeUserCommandAsync("start");
        if (!auth.Allowed)
        {
            await RespondAsync(auth.DenyReason, ephemeral: true);
            return;
        }

        var result = await _systemd.StartAsync(CancellationToken.None);
        await RespondAsync(result.Message, ephemeral: true);
    }

    [SlashCommand("stop", "Stops the zomboid server if it is online.")]
    public async Task StopServerAsync()
    {
        var auth = await AuthorizeUserCommandAsync("stop");
        if (!auth.Allowed)
        {
            await RespondAsync(auth.DenyReason, ephemeral: true);
            return;
        }

        var result = await _systemd.StopAsync(CancellationToken.None);
        await RespondAsync(result.Message, ephemeral: true);
    }

    [SlashCommand("status", "Gets the status of the zomboid systemd service.")]
    public async Task StatusAsync()
    {
        var auth = await AuthorizeUserCommandAsync("status");
        if (!auth.Allowed)
        {
            await RespondAsync(auth.DenyReason, ephemeral: true);
            return;
        }

        var status = await _systemd.GetStatusAsync(CancellationToken.None);
        var emoji = status.IsOnline ? "🟢" : "🔴";
        await RespondAsync($"{emoji} `{status.RawStatus}`", ephemeral: true);
    }

    [SlashCommand("players", "Gets player count/player list from RCON.")]
    public async Task PlayersAsync()
    {
        var auth = await AuthorizeUserCommandAsync("players");
        if (!auth.Allowed)
        {
            await RespondAsync(auth.DenyReason, ephemeral: true);
            return;
        }

        try
        {
            var playersOutput = await _rconClient.GetPlayersAsync(CancellationToken.None);
            await RespondAsync($"```{playersOutput}```", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch players via RCON.");
            await RespondAsync("Failed to query players via RCON.", ephemeral: true);
        }
    }

    [SlashCommand("allow", "Allow a Discord user ID to execute a command.")]
    public async Task AllowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        if (!EnsureAdmin())
        {
            await RespondAsync("Only the configured admin can run this command.", ephemeral: true);
            return;
        }

        if (!TryParseUserCommand(command, out var normalizedCommand))
        {
            await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(id, out var userId))
        {
            await RespondAsync("Invalid user ID.", ephemeral: true);
            return;
        }

        await _allowlistStore.AddUserToCommandAllowlistAsync(normalizedCommand, userId, CancellationToken.None);
        await RespondAsync($"Added `{userId}` to `{normalizedCommand}` allowlist.", ephemeral: true);
    }

    [SlashCommand("disallow", "Remove a Discord user ID from a command allowlist.")]
    public async Task DisallowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        if (!EnsureAdmin())
        {
            await RespondAsync("Only the configured admin can run this command.", ephemeral: true);
            return;
        }

        if (!TryParseUserCommand(command, out var normalizedCommand))
        {
            await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(id, out var userId))
        {
            await RespondAsync("Invalid user ID.", ephemeral: true);
            return;
        }

        await _allowlistStore.RemoveUserFromCommandAllowlistAsync(normalizedCommand, userId, CancellationToken.None);
        await RespondAsync($"Removed `{userId}` from `{normalizedCommand}` allowlist.", ephemeral: true);
    }

    [SlashCommand("channel", "Add/remove channel allowlist entries.")]
    public async Task ChannelAsync(
        [Summary("action", "add or remove")] string action,
        [Summary("channelid", "Discord channel ID")] string channelId)
    {
        if (!EnsureAdmin())
        {
            await RespondAsync("Only the configured admin can run this command.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(channelId, out var parsedChannelId))
        {
            await RespondAsync("Invalid channel ID.", ephemeral: true);
            return;
        }

        switch (action.Trim().ToLowerInvariant())
        {
            case "add":
                await _allowlistStore.AddAllowedChannelAsync(parsedChannelId, CancellationToken.None);
                await RespondAsync($"Added channel `{parsedChannelId}` to channel allowlist.", ephemeral: true);
                break;
            case "remove":
                await _allowlistStore.RemoveAllowedChannelAsync(parsedChannelId, CancellationToken.None);
                await RespondAsync($"Removed channel `{parsedChannelId}` from channel allowlist.", ephemeral: true);
                break;
            default:
                await RespondAsync("Invalid action. Use add or remove.", ephemeral: true);
                break;
        }
    }

    private bool EnsureAdmin() => _authorizationService.IsAdmin(Context.User.Id);

    private async Task<AuthorizationCheck> AuthorizeUserCommandAsync(string commandName)
    {
        return await _authorizationService.CanRunCommandAsync(
            commandName,
            Context.User.Id,
            Context.Channel.Id,
            CancellationToken.None);
    }

    private static bool TryParseUserCommand(string commandName, out string normalized)
    {
        normalized = commandName.Trim().TrimStart('/').ToLowerInvariant();
        return UserCommandNames.Contains(normalized);
    }
}
