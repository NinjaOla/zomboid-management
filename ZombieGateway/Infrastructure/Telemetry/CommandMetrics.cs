using System.Diagnostics.Metrics;

namespace ZombieGateway.Infrastructure.Telemetry;

public sealed class CommandMetrics : IDisposable
{
    public const string MeterName = "ZombieGateway";

    private readonly Meter _meter;
    private readonly Counter<long> _commandInvocations;
    private readonly Counter<long> _commandDenials;
    private readonly Counter<long> _commandErrors;

    public CommandMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _commandInvocations = _meter.CreateCounter<long>(
            "discord.command.invocations",
            unit: "{invocation}",
            description: "Number of /pz slash command invocations.");

        _commandDenials = _meter.CreateCounter<long>(
            "discord.command.denials",
            unit: "{denial}",
            description: "Number of /pz slash command authorization denials.");

        _commandErrors = _meter.CreateCounter<long>(
            "discord.command.errors",
            unit: "{error}",
            description: "Number of /pz slash command execution errors.");
    }

    public void RecordInvocation(string command, string userId, string username) =>
        _commandInvocations.Add(1,
            new KeyValuePair<string, object?>("command", command),
            new KeyValuePair<string, object?>("user.id", userId),
            new KeyValuePair<string, object?>("user.name", username));

    public void RecordDenial(string command, string userId, string username, string reason) =>
        _commandDenials.Add(1,
            new KeyValuePair<string, object?>("command", command),
            new KeyValuePair<string, object?>("user.id", userId),
            new KeyValuePair<string, object?>("user.name", username),
            new KeyValuePair<string, object?>("reason", reason));

    public void RecordError(string command, string userId, string username) =>
        _commandErrors.Add(1,
            new KeyValuePair<string, object?>("command", command),
            new KeyValuePair<string, object?>("user.id", userId),
            new KeyValuePair<string, object?>("user.name", username));

    public void Dispose() => _meter.Dispose();
}
