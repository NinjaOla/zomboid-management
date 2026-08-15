using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.Players;

public static class PlayersEndpoint
{
    public static IEndpointRouteBuilder MapPlayers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players", async (IRconClient rcon, CancellationToken cancellationToken) =>
        {
            try
            {
                var output = await rcon.GetPlayersAsync(cancellationToken);
                return Results.Ok(new PlayersResponse(output));
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Server unreachable");
            }
        });

        return app;
    }

    private sealed record PlayersResponse(string Output);
}
