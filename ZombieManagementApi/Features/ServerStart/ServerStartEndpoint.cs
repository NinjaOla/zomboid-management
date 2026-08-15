using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStart;

public static class ServerStartEndpoint
{
    public static IEndpointRouteBuilder MapServerStart(this IEndpointRouteBuilder app)
    {
        app.MapPost("/start", async (IServerController server, ILogger<ServerStartEndpoint.Log> logger, CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Start requested");
            try
            {
                var result = await server.StartAsync(cancellationToken);
                logger.LogInformation("Start result: success={Success} message={Message}", result.Success, result.Message);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Start failed");
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Server unreachable");
            }
        });

        return app;
    }

    public sealed class Log;
}
