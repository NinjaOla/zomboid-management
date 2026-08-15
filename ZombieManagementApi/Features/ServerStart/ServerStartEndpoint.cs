using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStart;

public static class ServerStartEndpoint
{
    public static IEndpointRouteBuilder MapServerStart(this IEndpointRouteBuilder app)
    {
        app.MapPost("/start", async (IServerController server, CancellationToken cancellationToken) =>
        {
            var result = await server.StartAsync(cancellationToken);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        return app;
    }
}
