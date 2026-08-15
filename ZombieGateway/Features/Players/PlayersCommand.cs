using Discord.Interactions;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.ServerControl;

namespace ZombieGateway.Features.Players;

public sealed class PlayersCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AllowlistAuthorizationService _auth;
    private readonly IRconClient _rcon;
    private readonly ILogger<PlayersCommand> _logger;

    public PlayersCommand(AllowlistAuthorizationService auth, IRconClient rcon, ILogger<PlayersCommand> logger)
    {
        _auth = auth;
        _rcon = rcon;
        _logger = logger;
    }

    [SlashCommand("players", "Gets player count/list from RCON.")]
    public async Task ExecuteAsync()
    {
        var (allowed, reason) = await _auth.AuthorizeAsync("players", Context.User.Id, Context.Channel.Id, CancellationToken.None);
        if (!allowed) { await RespondAsync(reason, ephemeral: true); return; }

        try
        {
            var output = await _rcon.GetPlayersAsync(CancellationToken.None);
            await RespondAsync($"```{output}```", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch players via RCON.");
            await RespondAsync("Failed to query players via RCON.", ephemeral: true);
        }
    }
}
