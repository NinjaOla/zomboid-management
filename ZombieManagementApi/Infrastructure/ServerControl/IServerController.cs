namespace ZombieManagementApi.Infrastructure.ServerControl;

public interface IServerController
{
    Task<ServiceStatusResult> GetStatusAsync(CancellationToken cancellationToken);
    Task<ServiceOperationResult> StartAsync(CancellationToken cancellationToken);
    Task<ServiceOperationResult> StopAsync(CancellationToken cancellationToken);
}
