using ZombieManagementApi.Infrastructure.ServerControl;

namespace ZombieManagementApi.Features.ServerStatus;

public static class ServerStatusEndpoint
{
    public static IEndpointRouteBuilder MapServerStatus(this IEndpointRouteBuilder app)
    {
        app.MapGet("/status", async (IServerController server, ILogger<ServerStatusEndpoint.Log> logger, CancellationToken cancellationToken) =>
        {
            try
            {
                var status = await server.GetStatusAsync(cancellationToken);
                logger.LogInformation("Status check: isOnline={IsOnline} raw={RawStatus}", status.IsOnline, status.RawStatus);
                return Results.Ok(status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Status check failed");
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Server unreachable");
            }
        });

        return app;
    }

    public sealed class Log;
}
