using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStatus;

public static class ServerStatusEndpoint
{
    public static IEndpointRouteBuilder MapServerStatus(this IEndpointRouteBuilder app)
    {
        app.MapGet("/status", async (IServerController server, CancellationToken cancellationToken) =>
        {
            try
            {
                var status = await server.GetStatusAsync(cancellationToken);
                return Results.Ok(status);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Server unreachable");
            }
        });

        return app;
    }
}
