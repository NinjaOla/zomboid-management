using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using ZombieGateway.Infrastructure.Configuration;

namespace ZombieGateway.Infrastructure.ServerControl;

public sealed class ZomboidRconClient : IRconClient
{
    private readonly ZomboidOptions _options;

    public ZomboidRconClient(IOptions<ZomboidOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> GetPlayersAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RconPassword))
        {
            throw new InvalidOperationException("RCON password is not configured.");
        }

        using var client = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _options.CommandTimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        await client.ConnectAsync(_options.RconHost, _options.RconPort, linked.Token);

        await using var stream = client.GetStream();
        var payload = Encoding.UTF8.GetBytes($"{_options.RconPassword}\nplayers\n");
        await stream.WriteAsync(payload, linked.Token);
        await stream.FlushAsync(linked.Token);

        var buffer = new byte[8192];
        var bytesRead = await stream.ReadAsync(buffer, linked.Token);
        return bytesRead <= 0
            ? "No response from RCON server."
            : Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
    }
}
