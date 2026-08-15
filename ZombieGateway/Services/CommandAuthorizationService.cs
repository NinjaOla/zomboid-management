using Microsoft.Extensions.Options;
using ZombieGateway.Options;

namespace ZombieGateway.Services;

public sealed class CommandAuthorizationService : ICommandAuthorizationService
{
    private readonly IAllowlistStore _allowlistStore;
    private readonly DiscordOptions _discordOptions;

    public CommandAuthorizationService(IAllowlistStore allowlistStore, IOptions<DiscordOptions> discordOptions)
    {
        _allowlistStore = allowlistStore;
        _discordOptions = discordOptions.Value;
    }

    public bool IsAdmin(ulong userId) => _discordOptions.AdminUserId != 0 && userId == _discordOptions.AdminUserId;

    public async Task<AuthorizationCheck> CanRunCommandAsync(
        string commandName,
        ulong userId,
        ulong channelId,
        CancellationToken cancellationToken)
    {
        if (IsAdmin(userId))
        {
            return new AuthorizationCheck(true);
        }

        var channelAllowed = await _allowlistStore.IsChannelAllowedAsync(channelId, cancellationToken);
        if (!channelAllowed)
        {
            return new AuthorizationCheck(false, "This channel is not allowed.");
        }

        var userAllowed = await _allowlistStore.IsUserAllowedForCommandAsync(commandName, userId, cancellationToken);
        if (!userAllowed)
        {
            return new AuthorizationCheck(false, "You are not allowlisted for this command.");
        }

        return new AuthorizationCheck(true);
    }
}
