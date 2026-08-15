using Discord.Interactions;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.ServerStatus;

public sealed class ServerStatusCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IServerController _server;

    public ServerStatusCommand(AllowlistAuthorizationService auth, IServerController server)
    {
        _auth = auth;
        _server = server;
    }

    [SlashCommand("status", "Gets the status of the zomboid systemd service.")]
    public async Task ExecuteAsync()
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("status", Context.User.Id, Context.Channel.Id, CancellationToken.None);
        if (!allowed) { await RespondAsync(reason, ephemeral: true); return; }

        var status = await _server.GetStatusAsync(CancellationToken.None);
        var emoji = status.IsOnline ? "🟢" : "🔴";
        await RespondAsync($"{emoji} `{status.RawStatus}`", ephemeral: true);
    }
}
