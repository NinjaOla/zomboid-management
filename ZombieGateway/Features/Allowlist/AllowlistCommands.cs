using Discord.Interactions;
using ZombieGateway.Features.Allowlist;

namespace ZombieGateway.Features.Allowlist;

[Group("pz", "Project Zomboid server management")]
public sealed class AllowlistCommands : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly HashSet<string> ValidCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "start", "stop", "status", "players"
    };

    private readonly AllowlistAuthorizationService _auth;
    private readonly IAllowlistStore _store;

    public AllowlistCommands(AllowlistAuthorizationService auth, IAllowlistStore store)
    {
        _auth = auth;
        _store = store;
    }

    [SlashCommand("allow", "Allow a Discord user ID to execute a command.")]
    public async Task AllowAsync(
        [Summary("id", "Discord user ID")] string id,
        [Summary("command", "One of: start, stop, status, players")] string command)
    {
        if (!EnsureAdmin()) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
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
        if (!EnsureAdmin()) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
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
        if (!EnsureAdmin()) { await RespondAsync("Only the configured admin can run this command.", ephemeral: true); return; }
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

    private bool EnsureAdmin() => _auth.IsAdmin(Context.User.Id);
}
