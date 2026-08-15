using System.Diagnostics;
using Discord.Interactions;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Features.Players;
using ZombieGateway.Features.ServerStart;
using ZombieGateway.Features.ServerStop;
using ZombieGateway.Features.ServerStatus;
using ZombieGateway.Infrastructure.Telemetry;

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
    private readonly CommandMetrics _metrics;
    private readonly ILogger<PzCommandModule> _logger;

    public PzCommandModule(
        ServerStartHandler start,
        ServerStopHandler stop,
        ServerStatusHandler status,
        PlayersHandler players,
        AllowlistAuthorizationService auth,
        IAllowlistStore store,
        CommandMetrics metrics,
        ILogger<PzCommandModule> logger)
    {
        _start = start;
        _stop = stop;
        _status = status;
        _players = players;
        _auth = auth;
        _store = store;
        _metrics = metrics;
        _logger = logger;
    }

    [SlashCommand("start", "Starts the zomboid server if it is offline.")]
    public async Task StartAsync()
    {
        var (userId, username) = UserInfo();
        using var activity = CommandActivitySource.StartCommand("start", userId, username);
        _logger.LogInformation("/pz start invoked by {User} in channel {Channel}", username, Context.Channel.Id);
        _metrics.RecordInvocation("start", userId, username);
        var deny = await _start.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null)
        {
            _logger.LogWarning("/pz start denied for {User}: {Reason}", username, deny);
            _metrics.RecordDenial("start", userId, username, deny);
            activity?.SetTag("denied", true);
            await RespondAsync(deny, ephemeral: true);
            return;
        }
        var result = await _start.ExecuteAsync();
        if (!result.Success) { _metrics.RecordError("start", userId, username); activity?.SetStatus(ActivityStatusCode.Error, result.Message); _logger.LogError(result.Exception, "/pz start failed for {User}.", username); }
        else { activity?.SetTag("result", result.Message); _logger.LogInformation("/pz start result: {Result}", result.Message); }
        await RespondAsync(result.Message, ephemeral: true);
    }

    [SlashCommand("stop", "Stops the zomboid server if it is online.")]
    public async Task StopAsync()
    {
        var (userId, username) = UserInfo();
        using var activity = CommandActivitySource.StartCommand("stop", userId, username);
        _logger.LogInformation("/pz stop invoked by {User} in channel {Channel}", username, Context.Channel.Id);
        _metrics.RecordInvocation("stop", userId, username);
        var deny = await _stop.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null)
        {
            _logger.LogWarning("/pz stop denied for {User}: {Reason}", username, deny);
            _metrics.RecordDenial("stop", userId, username, deny);
            activity?.SetTag("denied", true);
            await RespondAsync(deny, ephemeral: true);
            return;
        }
        var result = await _stop.ExecuteAsync();
        if (!result.Success) { _metrics.RecordError("stop", userId, username); activity?.SetStatus(ActivityStatusCode.Error, result.Message); _logger.LogError(result.Exception, "/pz stop failed for {User}.", username); }
        else { activity?.SetTag("result", result.Message); _logger.LogInformation("/pz stop result: {Result}", result.Message); }
        await RespondAsync(result.Message, ephemeral: true);
    }

    [SlashCommand("status", "Gets the status of the zomboid systemd service.")]
    public async Task StatusAsync()
    {
        var (userId, username) = UserInfo();
        using var activity = CommandActivitySource.StartCommand("status", userId, username);
        _logger.LogInformation("/pz status invoked by {User} in channel {Channel}", username, Context.Channel.Id);
        _metrics.RecordInvocation("status", userId, username);
        var deny = await _status.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null)
        {
            _logger.LogWarning("/pz status denied for {User}: {Reason}", username, deny);
            _metrics.RecordDenial("status", userId, username, deny);
            activity?.SetTag("denied", true);
            await RespondAsync(deny, ephemeral: true);
            return;
        }
        var result = await _status.ExecuteAsync();
        if (!result.Success) { _metrics.RecordError("status", userId, username); activity?.SetStatus(ActivityStatusCode.Error, result.Message); _logger.LogError(result.Exception, "/pz status failed for {User}.", username); }
        else { activity?.SetTag("result", result.Message); _logger.LogInformation("/pz status result: {Result}", result.Message); }
        await RespondAsync(result.Message, ephemeral: true);
    }

    [SlashCommand("players", "Gets player count/list from RCON.")]
    public async Task PlayersAsync()
    {
        var (userId, username) = UserInfo();
        using var activity = CommandActivitySource.StartCommand("players", userId, username);
        _logger.LogInformation("/pz players invoked by {User} in channel {Channel}", username, Context.Channel.Id);
        _metrics.RecordInvocation("players", userId, username);
        var deny = await _players.AuthorizeAsync(Context.User.Id, Context.Channel.Id);
        if (deny is not null)
        {
            _logger.LogWarning("/pz players denied for {User}: {Reason}", username, deny);
            _metrics.RecordDenial("players", userId, username, deny);
            activity?.SetTag("denied", true);
            await RespondAsync(deny, ephemeral: true);
            return;
        }
        var result = await _players.ExecuteAsync();
        if (!result.Success)
        {
            _metrics.RecordError("players", userId, username);
            activity?.SetStatus(ActivityStatusCode.Error, result.Message);
            _logger.LogError(result.Exception, "/pz players failed for {User}.", username);
            await RespondAsync(result.Message, ephemeral: true);
            return;
        }
        activity?.SetTag("player_output_length", result.Message.Length);
        _logger.LogInformation("/pz players returned output ({Length} chars)", result.Message.Length);
        await RespondAsync($"```{result.Message}```", ephemeral: true);
    }

    [SlashCommand("allow", "Allow a Discord user ID to execute a command.")]
    public async Task AllowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        var (userId, username) = UserInfo();
        _logger.LogInformation("/pz allow invoked by {Admin} — id={Id} command={Command}", username, id, command);
        _metrics.RecordInvocation("allow", userId, username);
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ValidCommands.Contains(command)) { await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true); return; }
        if (!ulong.TryParse(id, out var targetUserId)) { await RespondAsync("Invalid user ID.", ephemeral: true); return; }

        await _store.AddUserToCommandAsync(command, targetUserId, CancellationToken.None);
        _logger.LogInformation("Allowlisted user {UserId} for command {Command}.", targetUserId, command);
        await RespondAsync($"Added `{targetUserId}` to `{command}` allowlist.", ephemeral: true);
    }

    [SlashCommand("disallow", "Remove a Discord user ID from a command allowlist.")]
    public async Task DisallowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        var (userId, username) = UserInfo();
        _logger.LogInformation("/pz disallow invoked by {Admin} — id={Id} command={Command}", username, id, command);
        _metrics.RecordInvocation("disallow", userId, username);
        if (!_auth.IsAdmin(Context.User.Id)) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
        if (!ValidCommands.Contains(command)) { await RespondAsync("Invalid command. Use start, stop, status, or players.", ephemeral: true); return; }
        if (!ulong.TryParse(id, out var targetUserId)) { await RespondAsync("Invalid user ID.", ephemeral: true); return; }

        await _store.RemoveUserFromCommandAsync(command, targetUserId, CancellationToken.None);
        _logger.LogInformation("Removed user {UserId} from {Command} allowlist.", targetUserId, command);
        await RespondAsync($"Removed `{targetUserId}` from `{command}` allowlist.", ephemeral: true);
    }

    [SlashCommand("channel", "Add/remove channel allowlist entries.")]
    public async Task ChannelAsync(
        [Summary("action", "add or remove")] string action,
        [Summary("channelid", "Discord channel ID")] string channelId)
    {
        var (userId, username) = UserInfo();
        _logger.LogInformation("/pz channel invoked by {Admin} — action={Action} channelId={ChannelId}", username, action, channelId);
        _metrics.RecordInvocation("channel", userId, username);
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

    private (string Id, string Name) UserInfo() =>
        (Context.User.Id.ToString(), Context.User.Username);
}
