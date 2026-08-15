using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ZombieGateway.Discord;
using ZombieGateway.Options;
using ZombieGateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));
builder.Services.Configure<ZomboidOptions>(builder.Configuration.GetSection(ZomboidOptions.SectionName));
builder.Services.Configure<AuthorizationOptions>(builder.Configuration.GetSection(AuthorizationOptions.SectionName));
builder.Services.Configure<ManagementApiOptions>(builder.Configuration.GetSection(ManagementApiOptions.SectionName));

builder.Services.AddSingleton<IAllowlistStore, FileAllowlistStore>();
builder.Services.AddSingleton<ICommandAuthorizationService, CommandAuthorizationService>();

var managementApiOptions = builder.Configuration
    .GetSection(ManagementApiOptions.SectionName)
    .Get<ManagementApiOptions>() ?? new ManagementApiOptions();

if (!string.IsNullOrWhiteSpace(managementApiOptions.BaseUrl))
{
    builder.Services.AddHttpClient<RemoteManagementClient>(client =>
    {
        client.BaseAddress = new Uri(managementApiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    });
    builder.Services.AddSingleton<ISystemdServiceController>(sp => sp.GetRequiredService<RemoteManagementClient>());
    builder.Services.AddSingleton<IZomboidRconClient>(sp => sp.GetRequiredService<RemoteManagementClient>());
}
else
{
    builder.Services.AddSingleton<ISystemdServiceController, SystemdServiceController>();
    builder.Services.AddSingleton<IZomboidRconClient, ZomboidRconTcpClient>();
}

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
