namespace ZombieGateway.Options;

public sealed class AuthorizationOptions
{
    public const string SectionName = "Authorization";

    public string StoragePath { get; set; } = "Data/allowlist.json";
}
