using Discord.Interactions;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.ServerStop;

public sealed class ServerStopCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IServerController _server;

    public ServerStopCommand(AllowlistAuthorizationService auth, IServerController server)
    {
        _auth = auth;
        _server = server;
    }

    [SlashCommand("stop", "Stops the zomboid server if it is online.")]
    public async Task ExecuteAsync()
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("stop", Context.User.Id, Context.Channel.Id, CancellationToken.None);
        if (!allowed) { await RespondAsync(reason, ephemeral: true); return; }

        var result = await _server.StopAsync(CancellationToken.None);
        await RespondAsync(result.Message, ephemeral: true);
    }
}
