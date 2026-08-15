namespace ZombieGateway.Services;

public interface ICommandAuthorizationService
{
    bool IsAdmin(ulong userId);
    Task<AuthorizationCheck> CanRunCommandAsync(string commandName, ulong userId, ulong channelId, CancellationToken cancellationToken);
}

public sealed record AuthorizationCheck(bool Allowed, string? DenyReason = null);
