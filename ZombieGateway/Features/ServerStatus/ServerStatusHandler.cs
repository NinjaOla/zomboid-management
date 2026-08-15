using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.ServerStatus;

public sealed class ServerStatusHandler
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IServerController _server;

    public ServerStatusHandler(AllowlistAuthorizationService auth, IServerController server)
    {
        _auth = auth;
        _server = server;
    }

    public async Task<string?> AuthorizeAsync(ulong userId, ulong channelId)
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("status", userId, channelId, CancellationToken.None);
        return allowed ? null : reason;
    }

    public async Task<HandlerResult> ExecuteAsync()
    {
        try
        {
            var status = await _server.GetStatusAsync(CancellationToken.None);
            var emoji = status.IsOnline ? "🟢" : "🔴";
            return HandlerResult.Ok($"{emoji} `{status.RawStatus}`");
        }
        catch (Exception ex)
        {
            return HandlerResult.Unreachable(ex);
        }
    }
}
