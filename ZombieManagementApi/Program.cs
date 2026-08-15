using ZombieManagementApi.Features.Players;
using ZombieManagementApi.Features.ServerStart;
using ZombieManagementApi.Features.ServerStatus;
using ZombieManagementApi.Features.ServerStop;
using ZombieManagementApi.Infrastructure.Auth;
using ZombieManagementApi.Infrastructure.Configuration;
using ZombieManagementApi.Infrastructure.ServerControl;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<ManagementAuthOptions>(builder.Configuration.GetSection(ManagementAuthOptions.SectionName));
builder.Services.Configure<ZomboidOptions>(builder.Configuration.GetSection(ZomboidOptions.SectionName));

builder.Services.AddSingleton<IServerController, SystemdServerController>();
builder.Services.AddSingleton<IRconClient, ZomboidRconClient>();
builder.Services.AddSingleton<ApiKeyEndpointFilter>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
    errorApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        ctx.Response.StatusCode = 503;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "Server unreachable" });
    }));

app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Ok("ZombieManagementApi is running."));

var server = app.MapGroup("/server").AddEndpointFilter<ApiKeyEndpointFilter>();
server.MapServerStatus();
server.MapServerStart();
server.MapServerStop();
server.MapPlayers();

app.Run();
