using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStart;

public static class ServerStartEndpoint
{
    public static IEndpointRouteBuilder MapServerStart(this IEndpointRouteBuilder app)
    {
        app.MapPost("/start", async (IServerController server, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await server.StartAsync(cancellationToken);
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
