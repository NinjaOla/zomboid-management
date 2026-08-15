using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStop;

public static class ServerStopEndpoint
{
    public static IEndpointRouteBuilder MapServerStop(this IEndpointRouteBuilder app)
    {
        app.MapPost("/stop", async (IServerController server, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await server.StopAsync(cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Server unreachable");
            }
        });

        return app;
    }
}
