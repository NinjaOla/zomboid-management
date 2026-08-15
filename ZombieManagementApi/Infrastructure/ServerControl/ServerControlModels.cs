namespace ZombieManagementApi.Infrastructure.ServerControl;

public sealed record ServiceOperationResult(bool Success, string Message);
public sealed record ServiceStatusResult(bool IsOnline, string RawStatus);
