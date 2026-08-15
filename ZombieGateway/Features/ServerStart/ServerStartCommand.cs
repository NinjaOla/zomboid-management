using Discord.Interactions;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.ServerStart;

public sealed class ServerStartCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IServerController _server;

    public ServerStartCommand(AllowlistAuthorizationService auth, IServerController server)
    {
        _auth = auth;
        _server = server;
    }

    [SlashCommand("start", "Starts the zomboid server if it is offline.")]
    public async Task ExecuteAsync()
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("start", Context.User.Id, Context.Channel.Id, CancellationToken.None);
        if (!allowed) { await RespondAsync(reason, ephemeral: true); return; }

        var result = await _server.StartAsync(CancellationToken.None);
        await RespondAsync(result.Message, ephemeral: true);
    }
}
