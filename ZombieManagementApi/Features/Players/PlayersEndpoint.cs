using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.Players;

public static class PlayersEndpoint
{
    public static IEndpointRouteBuilder MapPlayers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players", async (IRconClient rcon, ILogger<PlayersEndpoint.Log> logger, CancellationToken cancellationToken) =>
        {
            try
            {
                var output = await rcon.GetPlayersAsync(cancellationToken);
                logger.LogInformation("Players RCON response: {Output}", output);
                return Results.Ok(new PlayersResponse(output));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Players RCON failed");
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Server unreachable");
            }
        });

        return app;
    }

    private sealed record PlayersResponse(string Output);
    public sealed class Log;
}
