namespace ZombieManagementApi.Services;

public interface IZomboidRconClient
{
    Task<string> GetPlayersAsync(CancellationToken cancellationToken);
}
