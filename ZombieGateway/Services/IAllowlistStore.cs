namespace ZombieGateway.Services;

public interface IAllowlistStore
{
    Task<bool> IsChannelAllowedAsync(ulong channelId, CancellationToken cancellationToken);
    Task<bool> IsUserAllowedForCommandAsync(string commandName, ulong userId, CancellationToken cancellationToken);
    Task AddUserToCommandAllowlistAsync(string commandName, ulong userId, CancellationToken cancellationToken);
    Task RemoveUserFromCommandAllowlistAsync(string commandName, ulong userId, CancellationToken cancellationToken);
    Task AddAllowedChannelAsync(ulong channelId, CancellationToken cancellationToken);
    Task RemoveAllowedChannelAsync(ulong channelId, CancellationToken cancellationToken);
}
