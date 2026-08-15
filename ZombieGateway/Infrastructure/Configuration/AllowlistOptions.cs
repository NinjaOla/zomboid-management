namespace ZombieGateway.Infrastructure.Configuration;

public sealed class AllowlistOptions
{
    public const string SectionName = "Authorization";

    public string StoragePath { get; set; } = "Data/allowlist.json";
}
