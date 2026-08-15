using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.Players;

public static class PlayersEndpoint
{
    public static IEndpointRouteBuilder MapPlayers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/server/players", async (IRconClient rcon, CancellationToken cancellationToken) =>
        {
            try
            {
                var output = await rcon.GetPlayersAsync(cancellationToken);
                return Results.Ok(new PlayersResponse(output));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }

    private sealed record PlayersResponse(string Output);
}
