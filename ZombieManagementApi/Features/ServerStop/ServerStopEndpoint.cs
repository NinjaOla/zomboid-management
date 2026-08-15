using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStop;

public static class ServerStopEndpoint
{
    public static IEndpointRouteBuilder MapServerStop(this IEndpointRouteBuilder app)
    {
        app.MapPost("/stop", async (IServerController server, ILogger<ServerStopEndpoint.Log> logger, CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Stop requested");
            try
            {
                var result = await server.StopAsync(cancellationToken);
                logger.LogInformation("Stop result: success={Success} message={Message}", result.Success, result.Message);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stop failed");
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Server unreachable");
            }
        });

        return app;
    }

    public sealed class Log;
}
