using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.ServerStop;

public sealed class ServerStopHandler
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IServerController _server;

    public ServerStopHandler(AllowlistAuthorizationService auth, IServerController server)
    {
        _auth = auth;
        _server = server;
    }

    public async Task<string?> AuthorizeAsync(ulong userId, ulong channelId)
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("stop", userId, channelId, CancellationToken.None);
        return allowed ? null : reason;
    }

    public async Task<string> ExecuteAsync() =>
        (await _server.StopAsync(CancellationToken.None)).Message;
}
