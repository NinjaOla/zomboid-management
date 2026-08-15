using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using ZombieManagementApi.Infrastructure.Configuration;

namespace ZombieManagementApi.Infrastructure.ServerControl;

// Implements the Source RCON protocol used by Project Zomboid
// Packet layout: int32 size | int32 id | int32 type | string body | 0x00 0x00
public sealed class ZomboidRconClient : IRconClient
{
    private const int TypeAuth = 3;
    private const int TypeExecCommand = 2;
    private const int TypeAuthResponse = 2;

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

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _options.CommandTimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        using var client = new TcpClient();
        await client.ConnectAsync(_options.RconHost, _options.RconPort, linked.Token);
        await using var stream = client.GetStream();

        _logger.LogInformation("RCON connected, authenticating");

        // Authenticate — server sends an empty packet then the auth response
        await SendPacketAsync(stream, 1, TypeAuth, _options.RconPassword, linked.Token);
        var firstResponse = await ReadPacketAsync(stream, linked.Token);
        // Some servers send an empty SERVERDATA_RESPONSE_VALUE before the auth response
        var authResponse = firstResponse.Type == TypeAuthResponse ? firstResponse : await ReadPacketAsync(stream, linked.Token);

        if (authResponse.Id == -1)
            throw new InvalidOperationException("RCON authentication failed — wrong password.");

        _logger.LogInformation("RCON authenticated, sending 'players' command");

        // Send command
        await SendPacketAsync(stream, 2, TypeExecCommand, "players", linked.Token);
        var response = await ReadPacketAsync(stream, linked.Token);

        var output = response.Body.Trim();
        _logger.LogInformation("RCON response ({Bytes} bytes): {Response}", output.Length, output);
        return output;
    }

    private static async Task SendPacketAsync(Stream stream, int id, int type, string body, CancellationToken ct)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var size = 4 + 4 + bodyBytes.Length + 2; // id + type + body + 2 null bytes
        var packet = new byte[4 + size];

        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0), size);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4), id);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8), type);
        bodyBytes.CopyTo(packet, 12);
        // last two bytes are already 0x00 0x00

        await stream.WriteAsync(packet, ct);
        await stream.FlushAsync(ct);
    }

    private static async Task<(int Id, int Type, string Body)> ReadPacketAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[12];
        await ReadExactAsync(stream, header, ct);

        var size = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0));
        var id = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
        var type = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8));

        var bodyLength = size - 4 - 4 - 2;
        var bodyBytes = new byte[Math.Max(0, bodyLength)];
        if (bodyBytes.Length > 0)
            await ReadExactAsync(stream, bodyBytes, ct);

        await ReadExactAsync(stream, new byte[2], ct);

        return (id, type, Encoding.UTF8.GetString(bodyBytes));
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), ct);
            if (read == 0) throw new EndOfStreamException("RCON connection closed unexpectedly.");
            offset += read;
        }
    }
}
