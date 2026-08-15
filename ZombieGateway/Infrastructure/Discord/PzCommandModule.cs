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
        _logger.LogInformation("/pz start invoked by {User} in channel {Channel}", Context.User, Context.Channel.Id);
        var deny = await _start.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { _logger.LogWarning("/pz start denied for {User}: {Reason}", Context.User, deny); await RespondAsync(deny, ephemeral: true); return; }
        var result = await _start.ExecuteAsync();
        _logger.LogInformation("/pz start result: {Result}", result);
        await RespondAsync(result, ephemeral: true);
    }

    [SlashCommand("stop", "Stops the zomboid server if it is online.")]
    public async Task StopAsync()
    {
        _logger.LogInformation("/pz stop invoked by {User} in channel {Channel}", Context.User, Context.Channel.Id);
        var deny = await _stop.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { _logger.LogWarning("/pz stop denied for {User}: {Reason}", Context.User, deny); await RespondAsync(deny, ephemeral: true); return; }
        var result = await _stop.ExecuteAsync();
        _logger.LogInformation("/pz stop result: {Result}", result);
        await RespondAsync(result, ephemeral: true);
    }

    [SlashCommand("status", "Gets the status of the zomboid systemd service.")]
    public async Task StatusAsync()
    {
        _logger.LogInformation("/pz status invoked by {User} in channel {Channel}", Context.User, Context.Channel.Id);
        var deny = await _status.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { _logger.LogWarning("/pz status denied for {User}: {Reason}", Context.User, deny); await RespondAsync(deny, ephemeral: true); return; }
        var result = await _status.ExecuteAsync();
        _logger.LogInformation("/pz status result: {Result}", result);
        await RespondAsync(result, ephemeral: true);
    }

    [SlashCommand("players", "Gets player count/list from RCON.")]
    public async Task PlayersAsync()
    {
        _logger.LogInformation("/pz players invoked by {User} in channel {Channel}", Context.User, Context.Channel.Id);
        var deny = await _players.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null) { _logger.LogWarning("/pz players denied for {User}: {Reason}", Context.User, deny); await RespondAsync(deny, ephemeral: true); return; }
        try
        {
            var output = await _players.ExecuteAsync();
            _logger.LogInformation("/pz players returned output ({Length} chars)", output.Length);
            await RespondAsync($"```{output}```", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "/pz players failed for {User}.", Context.User);
            await RespondAsync("Failed to query players via RCON.", ephemeral: true);
        }
    }

    [SlashCommand("allow", "Allow a Discord user ID to execute a command.")]
    public async Task AllowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        _logger.LogInformation("/pz allow invoked by {Admin} — id={Id} command={Command}", Context.User, id, command);
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ValidCommands.Contains(command)) { await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true); return; }
        if (!ulong.TryParse(id, out var userId)) { await RespondAsync("Invalid user ID.", ephemeral: true); return; }

        await _store.AddUserToCommandAsync(command, userId, CancellationToken.None);
        _logger.LogInformation("Allowlisted user {UserId} for command {Command}.", userId, command);
        await RespondAsync($"Added `{userId}` to `{command}` allowlist.", ephemeral: true);
    }

    [SlashCommand("disallow", "Remove a Discord user ID from a command allowlist.")]
    public async Task DisallowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        _logger.LogInformation("/pz disallow invoked by {Admin} — id={Id} command={Command}", Context.User, id, command);
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ValidCommands.Contains(command)) { await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true); return; }
        if (!ulong.TryParse(id, out var userId)) { await RespondAsync("Invalid user ID.", ephemeral: true); return; }

        await _store.RemoveUserFromCommandAsync(command, userId, CancellationToken.None);
        _logger.LogInformation("Removed user {UserId} from {Command} allowlist.", userId, command);
        await RespondAsync($"Removed `{userId}` from `{command}` allowlist.", ephemeral: true);
    }

    [SlashCommand("channel", "Add/remove channel allowlist entries.")]
    public async Task ChannelAsync(
        [Summary("action", "add or remove")] string action,
        [Summary("channelid", "Discord channel ID")] string channelId)
    {
        _logger.LogInformation("/pz channel invoked by {Admin} — action={Action} channelId={ChannelId}", Context.User, action, channelId);
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ulong.TryParse(channelId, out var parsedId)) { await RespondAsync("Invalid channel ID.", ephemeral: true); return; }

        switch (action.Trim().ToLowerInvariant())
        {
            case "add":
                await _store.AddChannelAsync(parsedId, CancellationToken.None);
                _logger.LogInformation("Added channel {ChannelId} to allowlist.", parsedId);
                await RespondAsync($"Added channel `{parsedId}` to channel allowlist.", ephemeral: true);
                break;
            case "remove":
                await _store.RemoveChannelAsync(parsedId, CancellationToken.None);
                _logger.LogInformation("Removed channel {ChannelId} from allowlist.", parsedId);
                await RespondAsync($"Removed channel `{parsedId}` from channel allowlist.", ephemeral: true);
                break;
            default:
                await RespondAsync("Invalid action. Use add or remove.", ephemeral: true);
                break;
        }
    }
}
