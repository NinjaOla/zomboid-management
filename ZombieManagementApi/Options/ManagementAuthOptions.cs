namespace ZombieManagementApi.Options;

public sealed class ManagementAuthOptions
{
    public const string SectionName = "ManagementAuth";

    public string ApiKey { get; set; } = string.Empty;
}
