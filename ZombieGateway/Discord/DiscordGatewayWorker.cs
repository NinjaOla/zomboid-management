using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using ZombieGateway.Options;

namespace ZombieGateway.Discord;

public sealed class DiscordGatewayWorker : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<DiscordGatewayWorker> _logger;
    private readonly DiscordOptions _options;
    private bool _modulesAdded;
    private bool _commandsRegistered;

    public DiscordGatewayWorker(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IOptions<DiscordOptions> options,
        ILogger<DiscordGatewayWorker> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogWarning("Discord bot token not configured. Discord worker is disabled.");
            return;
        }

        _client.Log += OnDiscordLogAsync;
        _interactions.Log += OnDiscordLogAsync;
        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;

        await _client.LoginAsync(TokenType.Bot, _options.BotToken);
        await _client.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= OnReadyAsync;
        _client.InteractionCreated -= OnInteractionCreatedAsync;
        _client.Log -= OnDiscordLogAsync;
        _interactions.Log -= OnDiscordLogAsync;

        await _client.StopAsync();
        await _client.LogoutAsync();

        await base.StopAsync(cancellationToken);
    }

    private async Task OnReadyAsync()
    {
        if (!_modulesAdded)
        {
            await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
            _modulesAdded = true;
        }

        if (_commandsRegistered)
        {
            return;
        }

        if (_options.RegisterCommandsGlobally || _options.GuildId == 0)
        {
            await _interactions.RegisterCommandsGloballyAsync(true);
            _logger.LogInformation("Registered Discord slash commands globally.");
        }
        else
        {
            await _interactions.RegisterCommandsToGuildAsync(_options.GuildId, true);
            _logger.LogInformation("Registered Discord slash commands to guild {GuildId}.", _options.GuildId);
        }

        _commandsRegistered = true;
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        var result = await _interactions.ExecuteCommandAsync(context, _services);

        if (!result.IsSuccess && !interaction.HasResponded)
        {
            await interaction.RespondAsync($"Command failed: {result.ErrorReason}", ephemeral: true);
        }
    }

    private Task OnDiscordLogAsync(LogMessage logMessage)
    {
        _logger.Log(
            ToLogLevel(logMessage.Severity),
            logMessage.Exception,
            "{Source}: {Message}",
            logMessage.Source,
            logMessage.Message);

        return Task.CompletedTask;
    }

    private static LogLevel ToLogLevel(LogSeverity severity) => severity switch
    {
        LogSeverity.Critical => LogLevel.Critical,
        LogSeverity.Error => LogLevel.Error,
        LogSeverity.Warning => LogLevel.Warning,
        LogSeverity.Info => LogLevel.Information,
        LogSeverity.Verbose => LogLevel.Trace,
        LogSeverity.Debug => LogLevel.Debug,
        _ => LogLevel.Information
    };
}
