using ZombieManagementApi.Auth;
using ZombieManagementApi.Options;
using ZombieManagementApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ManagementAuthOptions>(builder.Configuration.GetSection(ManagementAuthOptions.SectionName));
builder.Services.Configure<ZomboidOptions>(builder.Configuration.GetSection(ZomboidOptions.SectionName));

builder.Services.AddSingleton<ISystemdServiceController, SystemdServiceController>();
builder.Services.AddSingleton<IZomboidRconClient, ZomboidRconTcpClient>();
builder.Services.AddSingleton<ApiKeyEndpointFilter>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("ZombieManagementApi is running."));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var server = app.MapGroup("/server").AddEndpointFilter<ApiKeyEndpointFilter>();

server.MapGet("/status", async (ISystemdServiceController systemd, CancellationToken cancellationToken) =>
{
    var status = await systemd.GetStatusAsync(cancellationToken);
    return Results.Ok(status);
});

server.MapPost("/start", async (ISystemdServiceController systemd, CancellationToken cancellationToken) =>
{
    var result = await systemd.StartAsync(cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

server.MapPost("/stop", async (ISystemdServiceController systemd, CancellationToken cancellationToken) =>
{
    var result = await systemd.StopAsync(cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

server.MapGet("/players", async (IZomboidRconClient rcon, CancellationToken cancellationToken) =>
{
    try
    {
        var output = await rcon.GetPlayersAsync(cancellationToken);
        return Results.Ok(new PlayersResponse(output));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new ServiceOperationResult(false, ex.Message));
    }
});

app.Run();
