using Application.Commands.FChat;
using Application.Interfaces;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Presentation.Clients;
using Presentation.Configs;

namespace Presentation;

public sealed class OutboundWorker : BackgroundService
{
    private readonly IOutboundMessageQueue _outboundQueue;
    private readonly IFChatClient _fChatClient;
    private readonly FChatConfig _config;
    private readonly ILogger<OutboundWorker> _logger;

    public OutboundWorker(
        IOutboundMessageQueue outboundQueue,
        IFChatClient fChatClient,
        IOptions<FChatConfig> config,
        ILogger<OutboundWorker> logger)
    {
        _outboundQueue = outboundQueue ?? throw new ArgumentNullException(nameof(outboundQueue));
        _fChatClient = fChatClient ?? throw new ArgumentNullException(nameof(fChatClient));
        _config = (config ?? throw new ArgumentNullException(nameof(config))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Outbound]: Outbound message worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _outboundQueue.DequeueAsync();

                await SendAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Outbound]: Failed to send outbound message.");
            }
        }

        _logger.LogInformation("[Outbound]: Outbound message worker stopped.");
    }

    private async Task SendAsync(OutboundMessage message, CancellationToken ct)
    {
        switch (message.Type)
        {
            case MessageType.Private:
                if (string.IsNullOrWhiteSpace(message.Character))
                    throw new InvalidOperationException("F-Chat private message requires RecipientName.");

                await _fChatClient.SendCommandAsync(new PRI
                {
                    recipient = message.Character,
                    message = message.Message,
                    character = _config.Character
                }, ct);
                break;

            case MessageType.Public:
                if (string.IsNullOrWhiteSpace(message.ChannelId))
                    throw new InvalidOperationException("F-Chat channel message requires ChannelId.");

                await _fChatClient.SendCommandAsync(new MSG
                {
                    channel = message.ChannelId,
                    message = message.Message
                }, ct);
                break;

            case MessageType.Status:
                await _fChatClient.SendCommandAsync(new STA
                {
                    character = _config.Character,
                    status = "online",
                    statusmsg = message.Message
                }, ct);
                break;
        }
    }
}