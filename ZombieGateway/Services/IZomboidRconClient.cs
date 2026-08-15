namespace ZombieGateway.Services;

public interface IZomboidRconClient
{
    Task<string> GetPlayersAsync(CancellationToken cancellationToken);
}
