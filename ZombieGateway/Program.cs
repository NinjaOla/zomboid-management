using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ZombieGateway.Features.Allowlist;
using ZombieGateway.Infrastructure.Configuration;
using ZombieGateway.Infrastructure.Discord;
using ZombieGateway.Infrastructure.ServerControl;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));
builder.Services.Configure<ZomboidOptions>(builder.Configuration.GetSection(ZomboidOptions.SectionName));
builder.Services.Configure<AllowlistOptions>(builder.Configuration.GetSection(AllowlistOptions.SectionName));
builder.Services.Configure<ManagementApiOptions>(builder.Configuration.GetSection(ManagementApiOptions.SectionName));

// Allowlist feature
builder.Services.AddSingleton<IAllowlistStore, FileAllowlistStore>();
builder.Services.AddSingleton<AllowlistAuthorizationService>();

// Server control — remote if BaseUrl configured, local otherwise
var managementApiOptions = builder.Configuration
    .GetSection(ManagementApiOptions.SectionName)
    .Get<ManagementApiOptions>() ?? new ManagementApiOptions();

if (!string.IsNullOrWhiteSpace(managementApiOptions.BaseUrl))
{
    builder.Services.AddHttpClient<RemoteServerController>(client =>
    {
        client.BaseAddress = new Uri(managementApiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    });
    builder.Services.AddSingleton<IServerController>(sp => sp.GetRequiredService<RemoteServerController>());
    builder.Services.AddSingleton<IRconClient>(sp => sp.GetRequiredService<RemoteServerController>());
}
else
{
    builder.Services.AddSingleton<IServerController, SystemdServerController>();
    builder.Services.AddSingleton<IRconClient, ZomboidRconClient>();
}

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

// OpenTelemetry
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ZombieGateway"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();

        var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });

var app = builder.Build();

app.MapGet("/", () => Results.Ok("ZombieGateway is running."));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
