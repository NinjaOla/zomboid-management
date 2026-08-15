namespace ZombieGateway.Services;

public sealed class AllowlistState
{
    public HashSet<ulong> AllowedChannels { get; set; } = [];
    public Dictionary<string, HashSet<ulong>> AllowedUsersByCommand { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
