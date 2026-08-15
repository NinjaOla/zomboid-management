namespace ZombieGateway.Infrastructure.Configuration;

public sealed class ZomboidOptions
{
    public const string SectionName = "Zomboid";

    public string SystemdServiceName { get; set; } = "zomboid-server";
    public string RconHost { get; set; } = "127.0.0.1";
    public int RconPort { get; set; } = 27015;
    public string RconPassword { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 10;
}
