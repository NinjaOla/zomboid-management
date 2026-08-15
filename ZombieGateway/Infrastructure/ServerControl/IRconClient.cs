namespace ZombieGateway.Infrastructure.ServerControl;

public interface IRconClient
{
    Task<string> GetPlayersAsync(CancellationToken cancellationToken);
}
