namespace ZombieGateway.Services;

public interface ISystemdServiceController
{
    Task<ServiceStatusResult> GetStatusAsync(CancellationToken cancellationToken);
    Task<ServiceOperationResult> StartAsync(CancellationToken cancellationToken);
    Task<ServiceOperationResult> StopAsync(CancellationToken cancellationToken);
}
