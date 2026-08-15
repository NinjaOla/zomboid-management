using Microsoft.Extensions.Options;
using ZombieGateway.Infrastructure.Configuration;

namespace ZombieGateway.Features.Allowlist;

public sealed class AllowlistAuthorizationService
{
    private readonly IAllowlistStore _store;
    private readonly DiscordOptions _discordOptions;

    public AllowlistAuthorizationService(IAllowlistStore store, IOptions<DiscordOptions> discordOptions)
    {
        _store = store;
        _discordOptions = discordOptions.Value;
    }

    public bool IsAdmin(ulong userId) =>
        _discordOptions.AdminUserId != 0 && userId == _discordOptions.AdminUserId;

    public async Task<(bool Allowed, string? DenyReason)> AuthorizeAsync(
        string commandName, ulong userId, ulong channelId, CancellationToken cancellationToken)
    {
        if (IsAdmin(userId)) return (true, null);

        if (!await _store.IsChannelAllowedAsync(channelId, cancellationToken))
            return (false, "This channel is not allowed.");

        if (!await _store.IsUserAllowedForCommandAsync(commandName, userId, cancellationToken))
            return (false, "You are not allowlisted for this command.");

        return (true, null);
    }
}
