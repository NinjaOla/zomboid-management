using Discord.Interactions;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Features.Players;
using ZombieGateway.Features.ServerStart;
using ZombieGateway.Features.ServerStop;
using ZombieGateway.Features.ServerStatus;

namespace ZombieGateway.Infrastructure.Discord;

[Group("pz", "Project Zomboid server management")]
public sealed class PzCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly HashSet<string> ValidCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "start", "stop", "status", "players"
    };

    private readonly ServerStartHandler _start;
    private readonly ServerStopHandler _stop;
    private readonly ServerStatusHandler _status;
    private readonly PlayersHandler _players;
    private readonly AllowlistAuthorizationService _auth;
    private readonly IAllowlistStore _store;
    private readonly ILogger<PzCommandModule> _logger;

    public PzCommandModule(
        ServerStartHandler start,
        ServerStopHandler stop,
        ServerStatusHandler status,
        PlayersHandler players,
        AllowlistAuthorizationService auth,
        IAllowlistStore store,
        ILogger<PzCommandModule> logger)
    {
        _start = start;
        _stop = stop;
        _status = status;
        _players = players;
        _auth = auth;
        _store = store;
        _logger = logger;
    }

    [SlashCommand("start", "Starts the zomboid server if it is offline.")]
    public async Task StartAsync()
    {
        var deny = await _start.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { await RespondAsync(deny, ephemeral: true); return; }
        await RespondAsync(await _start.ExecuteAsync(), ephemeral: true);
    }

    [SlashCommand("stop", "Stops the zomboid server if it is online.")]
    public async Task StopAsync()
    {
        var deny = await _stop.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { await RespondAsync(deny, ephemeral: true); return; }
        await RespondAsync(await _stop.ExecuteAsync(), ephemeral: true);
    }

    [SlashCommand("status", "Gets the status of the zomboid systemd service.")]
    public async Task StatusAsync()
    {
        var deny = await _status.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { await RespondAsync(deny, ephemeral: true); return; }
        await RespondAsync(await _status.ExecuteAsync(), ephemeral: true);
    }

    [SlashCommand("players", "Gets player count/list from RCON.")]
    public async Task PlayersAsync()
    {
        var deny = await _players.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { await RespondAsync(deny, ephemeral: true); return; }
        try
        {
            var output = await _players.ExecuteAsync();
            await RespondAsync($"```{output}```", ephemeral: true);
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
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ValidCommands.Contains(command)) { await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true); return; }
        if (!ulong.TryParse(id, out var userId)) { await RespondAsync("Invalid user ID.", ephemeral: true); return; }

        await _store.AddUserToCommandAsync(command, userId, CancellationToken.None);
        await RespondAsync($"Added `{userId}` to `{command}` allowlist.", ephemeral: true);
    }

    [SlashCommand("disallow", "Remove a Discord user ID from a command allowlist.")]
    public async Task DisallowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ValidCommands.Contains(command)) { await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true); return; }
        if (!ulong.TryParse(id, out var userId)) { await RespondAsync("Invalid user ID.", ephemeral: true); return; }

        await _store.RemoveUserFromCommandAsync(command, userId, CancellationToken.None);
        await RespondAsync($"Removed `{userId}` from `{command}` allowlist.", ephemeral: true);
    }

    [SlashCommand("channel", "Add/remove channel allowlist entries.")]
    public async Task ChannelAsync(
        [Summary("action", "add or remove")] string action,
        [Summary("channelid", "Discord channel ID")] string channelId)
    {
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ulong.TryParse(channelId, out var parsedId)) { await RespondAsync("Invalid channel ID.", ephemeral: true); return; }

        switch (action.Trim().ToLowerInvariant())
        {
            case "add":
                await _store.AddChannelAsync(parsedId, CancellationToken.None);
                await RespondAsync($"Added channel `{parsedId}` to channel allowlist.", ephemeral: true);
                break;
            case "remove":
                await _store.RemoveChannelAsync(parsedId, CancellationToken.None);
                await RespondAsync($"Removed channel `{parsedId}` from channel allowlist.", ephemeral: true);
                break;
            default:
                await RespondAsync("Invalid action. Use add or remove.", ephemeral: true);
                break;
        }
    }
}
