using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.ServerStart;

public sealed class ServerStartHandler
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IServerController _server;

    public ServerStartHandler(AllowlistAuthorizationService auth, IServerController server)
    {
        _auth = auth;
        _server = server;
    }

    public async Task<string?> AuthorizeAsync(ulong userId, ulong channelId)
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("start", userId, channelId, CancellationToken.None);
        return allowed ? null : reason;
    }

    public async Task<HandlerResult> ExecuteAsync()
    {
        try
        {
            var result = await _server.StartAsync(CancellationToken.None);
            return result.Success
                ? HandlerResult.Ok(result.Message)
                : HandlerResult.Fail(result.Message);
        }
        catch (Exception ex)
        {
            return HandlerResult.Unreachable(ex);
        }
    }
}
