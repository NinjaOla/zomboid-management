using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using ZombieManagementApi.Infrastructure.Configuration;

namespace ZombieManagementApi.Infrastructure.ServerControl;

public sealed class ZomboidRconClient : IRconClient
{
    private readonly ZomboidOptions _options;
    private readonly ILogger<ZomboidRconClient> _logger;

    public ZomboidRconClient(IOptions<ZomboidOptions> options, ILogger<ZomboidRconClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetPlayersAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RconPassword))
            throw new InvalidOperationException("RCON password is not configured.");

        _logger.LogInformation("RCON connecting to {Host}:{Port}", _options.RconHost, _options.RconPort);

        using var client = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _options.CommandTimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        await client.ConnectAsync(_options.RconHost, _options.RconPort, linked.Token);
        _logger.LogInformation("RCON connected, sending 'players' command");

        await using var stream = client.GetStream();

        var payload = Encoding.UTF8.GetBytes($"{_options.RconPassword}\nplayers\n");
        await stream.WriteAsync(payload, linked.Token);
        await stream.FlushAsync(linked.Token);

        var buffer = new byte[8192];
        var bytesRead = await stream.ReadAsync(buffer, linked.Token);

        if (bytesRead <= 0)
        {
            _logger.LogWarning("RCON returned empty response");
            return "No response from RCON server.";
        }

        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        _logger.LogInformation("RCON response ({Bytes} bytes): {Response}", bytesRead, response);
        return response;
    }
}
