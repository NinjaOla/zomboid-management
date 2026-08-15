using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Features.Players;
using ZombieGateway.Features.ServerStart;
using ZombieGateway.Features.ServerStop;
using ZombieGateway.Features.ServerStatus;
using ZombieGateway.Infrastructure.Configuration;
using ZombieGateway.Infrastructure.Discord;
using ZombieGateway.Infrastructure.ServerControl;
using ZombieGateway.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));
builder.Services.Configure<ZomboidOptions>(builder.Configuration.GetSection(ZomboidOptions.SectionName));
builder.Services.Configure<AllowlistOptions>(builder.Configuration.GetSection(AllowlistOptions.SectionName));
builder.Services.Configure<ManagementApiOptions>(builder.Configuration.GetSection(ManagementApiOptions.SectionName));

// Allowlist feature
builder.Services.AddSingleton<IAllowlistStore, FileAllowlistStore>();
builder.Services.AddSingleton<AllowlistAuthorizationService>();

// Feature handlers
builder.Services.AddSingleton<ServerStartHandler>();
builder.Services.AddSingleton<ServerStopHandler>();
builder.Services.AddSingleton<ServerStatusHandler>();
builder.Services.AddSingleton<PlayersHandler>();

// Telemetry — register custom meter and activity source
builder.Services.AddSingleton<CommandMetrics>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(CommandMetrics.MeterName))
    .WithTracing(tracing => tracing.AddSource(CommandActivitySource.Name));

// Server control — always via ZombieManagementApi
var managementApiOptions = builder.Configuration
    .GetSection(ManagementApiOptions.SectionName)
    .Get<ManagementApiOptions>() ?? new ManagementApiOptions();

if (string.IsNullOrWhiteSpace(managementApiOptions.BaseUrl))
    throw new InvalidOperationException(
        "ManagementApi:BaseUrl must be configured. " +
        "Set it to the base URL of the ZombieManagementApi (e.g. http://192.168.1.50:5005).");

builder.Services.AddHttpClient<RemoteServerController>(client =>
{
    client.BaseAddress = new Uri(managementApiOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddSingleton<IServerController>(sp => sp.GetRequiredService<RemoteServerController>());
builder.Services.AddSingleton<IRconClient>(sp => sp.GetRequiredService<RemoteServerController>());

// Discord infrastructure
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds,
    LogGatewayIntentWarnings = false
}));
builder.Services.AddSingleton(sp => new InteractionService(
    sp.GetRequiredService<DiscordSocketClient>(),
    new InteractionServiceConfig
    {
        LogLevel = LogSeverity.Info,
        DefaultRunMode = RunMode.Async
    }));
builder.Services.AddHostedService<DiscordGatewayWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Ok("ZombieGateway is running."));

app.Run();
