using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Presentation;

public sealed class DispatcherWorker : BackgroundService
{
    private readonly IInboundMessageQueue _inboundQueue;
    private readonly IModuleDispatcher _moduleDispatcher;
    private readonly ILogger<DispatcherWorker> _logger;

    public DispatcherWorker(
        IInboundMessageQueue inboundQueue,
        IModuleDispatcher moduleDispatcher,
        ILogger<DispatcherWorker> logger)
    {
        _inboundQueue = inboundQueue ?? throw new ArgumentNullException(nameof(inboundQueue));
        _moduleDispatcher = moduleDispatcher ?? throw new ArgumentNullException(nameof(moduleDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Modules]: Dispatcher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _inboundQueue.DequeueAsync();

                _logger.LogInformation(
                    "[Modules]: Dequeued inbound message. Command={Command}, Message={Message}, Character={Character}",
                    message.Command,
                    message.Message,
                    message.CharacterName);
                
                await _moduleDispatcher.DispatchAsync(message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Modules]: Failed while dispatching inbound message.");
            }
        }

        _logger.LogInformation("[Modules]: Dispatcher worker stopped.");
    }
}