using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStatus;

public static class ServerStatusEndpoint
{
    public static IEndpointRouteBuilder MapServerStatus(this IEndpointRouteBuilder app)
    {
        app.MapGet("/server/status", async (IServerController server, CancellationToken cancellationToken) =>
        {
            var status = await server.GetStatusAsync(cancellationToken);
            return Results.Ok(status);
        });

        return app;
    }
}
