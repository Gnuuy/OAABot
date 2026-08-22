using Application;
using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Presentation;

// Periodic AFK/disconnect sweep, independent of the chat-message dispatch pipeline - this is
// what actually warns/kicks idle booth occupants and expires disconnect grace windows.
public sealed class GloryholeWatcherWorker : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private readonly IGloryholeManager _gloryholeManager;
    private readonly IOutboundMessageQueue _outboundQueue;
    private readonly ILogger<GloryholeWatcherWorker> _logger;

    public GloryholeWatcherWorker(
        IGloryholeManager gloryholeManager,
        IOutboundMessageQueue outboundQueue,
        ILogger<GloryholeWatcherWorker> logger)
    {
        _gloryholeManager = gloryholeManager;
        _outboundQueue = outboundQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Gloryhole]: AFK/disconnect watcher started.");

        using var timer = new PeriodicTimer(TickInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await GloryholeEventPublisher.PublishAsync(_outboundQueue, _gloryholeManager.ProcessAfkOnce());
                    await GloryholeEventPublisher.PublishAsync(_outboundQueue, _gloryholeManager.ProcessDisconnectTimeouts());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Gloryhole]: Failed during AFK/disconnect sweep.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("[Gloryhole]: AFK/disconnect watcher stopped.");
    }
}
