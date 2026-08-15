namespace ZombieManagementApi.Infrastructure.ServerControl;

public interface IRconClient
{
    Task<string> GetPlayersAsync(CancellationToken cancellationToken);
}
