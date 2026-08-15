namespace ZombieGateway.Services;

public sealed record ServiceOperationResult(bool Success, string Message);
public sealed record ServiceStatusResult(bool IsOnline, string RawStatus);
