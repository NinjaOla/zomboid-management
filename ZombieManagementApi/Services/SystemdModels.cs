namespace ZombieManagementApi.Services;

public sealed record ServiceOperationResult(bool Success, string Message);
public sealed record ServiceStatusResult(bool IsOnline, string RawStatus);
public sealed record PlayersResponse(string Output);
