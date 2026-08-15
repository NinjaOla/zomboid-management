namespace ZombieGateway.Infrastructure.Configuration;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    public string BotToken { get; set; } = string.Empty;
    public ulong AdminUserId { get; set; }
    public ulong GuildId { get; set; }
    public bool RegisterCommandsGlobally { get; set; }
}
