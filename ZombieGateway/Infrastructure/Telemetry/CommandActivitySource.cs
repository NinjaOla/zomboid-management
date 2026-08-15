using System.Diagnostics;

namespace ZombieGateway.Infrastructure.Telemetry;

public static class CommandActivitySource
{
    public const string Name = "ZombieGateway.Discord";

    private static readonly ActivitySource Source = new(Name);

    public static Activity? StartCommand(string commandName, string userId, string username) =>
        Source.StartActivity(
            $"pz {commandName}",
            ActivityKind.Internal,
            default(ActivityContext),
            [
                new("command", commandName),
                new("user.id", userId),
                new("user.name", username)
            ]);
}
