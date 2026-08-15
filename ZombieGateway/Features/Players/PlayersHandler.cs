using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.Players;

public sealed class PlayersHandler
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IRconClient _rcon;

    public PlayersHandler(AllowlistAuthorizationService auth, IRconClient rcon)
    {
        _auth = auth;
        _rcon = rcon;
    }

    public async Task<string?> AuthorizeAsync(ulong userId, ulong channelId)
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("players", userId, channelId, CancellationToken.None);
        return allowed ? null : reason;
    }

    public async Task<string> ExecuteAsync() =>
        await _rcon.GetPlayersAsync(CancellationToken.None);
}
