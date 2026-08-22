using Application;
using Application.Commands.FChat;
using Application.Interfaces;
using Domain;
using Domain.Enums;
using Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Presentation.Clients;
using Presentation.Configs;

namespace Presentation;

public class FChatWorker : BackgroundService
{
    private readonly ILogger<FChatWorker> _logger;
    private readonly IInboundMessageQueue _inboundQueue;
    private readonly IFChatClient _client;
    private readonly IBotStatusRepository _statusRepository;
    private readonly FChatConfig _config;

    public FChatWorker
        (ILogger<FChatWorker> logger,
        IInboundMessageQueue inboundQueue,
        IFChatClient client,
        IBotStatusRepository statusRepository,
        IOptions<FChatConfig> config)

    {
        _logger = logger;
        _client = client;
        _inboundQueue = inboundQueue;
        _statusRepository = statusRepository;
        _config = config.Value;
    }
    
    // Starting delay and cap for reconnect backoff. Once the delay needed before the next
    // attempt would exceed MaxReconnectDelay, we stop retrying rather than hammering F-List's
    // servers indefinitely - this requires an external process restart to recover.
    private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentDelay = InitialReconnectDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _client.ConnectAsync(stoppingToken);
                _logger.LogInformation("[F-Chat]: Connected to websocket");
                currentDelay = InitialReconnectDelay;

                await _client.AutoJoinChannelOnStartUpAsync(stoppingToken);
                await RestoreSavedStatusAsync(stoppingToken);
                await _client.ReceiveLoopAsync(stoppingToken);

                _logger.LogWarning("[F-Chat]: Connection to F-Chat ended.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[F-Chat]: Connection attempt failed.");
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            if (currentDelay > MaxReconnectDelay)
            {
                _logger.LogCritical(
                    "[F-Chat]: Reconnect backoff exceeded {MaxDelaySeconds}s without a stable connection. Giving up.",
                    MaxReconnectDelay.TotalSeconds);
                throw new InvalidOperationException(
                    "[F-Chat]: Unable to maintain a stable connection to F-Chat after repeated reconnect attempts.");
            }

            _logger.LogInformation("[F-Chat]: Reconnecting in {DelaySeconds}s...", currentDelay.TotalSeconds);

            try
            {
                await Task.Delay(currentDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            currentDelay *= 2;
        }

        _logger.LogInformation("[F-Chat]: F-Chat worker stopped.");
    }

    // Re-applies whatever status a moderator last set via !set_status, so it survives restarts
    // instead of resetting to F-Chat's blank default every time the bot reconnects.
    private async Task RestoreSavedStatusAsync(CancellationToken cancellationToken)
    {
        var statusMessage = await _statusRepository.GetStatusMessageAsync();
        if (string.IsNullOrWhiteSpace(statusMessage))
            return;

        await _client.SendCommandAsync(new STA
        {
            character = _config.Character,
            status = "online",
            statusmsg = statusMessage
        }, cancellationToken);
    }
}