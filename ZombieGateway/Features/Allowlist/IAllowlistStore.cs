namespace ZombieGateway.Features.Allowlist;

public interface IAllowlistStore
{
    Task<bool> IsChannelAllowedAsync(ulong channelId, CancellationToken cancellationToken);
    Task<bool> IsUserAllowedForCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken);
    Task AddUserToCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken);
    Task RemoveUserFromCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken);
    Task AddChannelAsync(ulong channelId, CancellationToken cancellationToken);
    Task RemoveChannelAsync(ulong channelId, CancellationToken cancellationToken);
}
