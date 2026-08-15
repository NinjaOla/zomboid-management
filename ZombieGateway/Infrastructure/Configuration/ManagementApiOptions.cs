namespace ZombieGateway.Infrastructure.Configuration;

public sealed class ManagementApiOptions
{
    public const string SectionName = "ManagementApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
