using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Commands.FChat;
using Application.Interfaces;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application;

public class FChatService : IFChatService
{
    private readonly IInboundMessageQueue _inboundMessageQueue;
    private readonly ICharacterPresenceTracker _presenceTracker;
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IOutboundMessageQueue _outboundMessageQueue;
    private readonly ILogger<FChatService> _logger;

    public FChatService(
        IInboundMessageQueue inboundMessageQueue,
        ICharacterPresenceTracker presenceTracker,
        IGloryholeManager gloryholeManager,
        IOutboundMessageQueue outboundMessageQueue,
        ILogger<FChatService> logger)
    {
        _inboundMessageQueue = inboundMessageQueue;
        _presenceTracker = presenceTracker;
        _gloryholeManager = gloryholeManager;
        _outboundMessageQueue = outboundMessageQueue;
        _logger = logger;
    }

    public async Task HandleMessageAsync(string message)
    {
        InboundMessage inboundMessage;
        
        var (protocol, json) = ParsePacket(message);

        if (string.IsNullOrWhiteSpace(protocol))
        {
            return;
        }

        
        switch (protocol)
        {
            case "PRI":
            {
                var payload = JsonSerializer.Deserialize<PRI>(json);
                var record = CommandParser.Parse(payload.message);

                // Non-command PMs (e.g. a booth occupant's "/me ..." emote) still need to reach
                // the module dispatcher - only MSG previously did this, so gloryhole's passive
                // messenger modules would otherwise never see them.
                inboundMessage = new InboundMessage()
                {
                    Command = record.Command,
                    Type = MessageType.Private,
                    CharacterName = payload.character,
                    Message = record.Message,
                    RawMessage = payload.message,
                };
                break;
            }
            case "MSG":
            {
                var payload = JsonSerializer.Deserialize<MSG>(json);
                var record = CommandParser.Parse(payload.message);

                inboundMessage = new InboundMessage()
                {
                    Command = record.Command,
                    Type = MessageType.Public,
                    CharacterName = payload.character,
                    ChannelId = payload.channel,
                    Message = record.Message,
                    RawMessage = payload.message,
                };
                break;
            }
            case "LIS":
            {
                var payload = JsonSerializer.Deserialize<LIS>(json);
                if (payload?.characters is not null)
                {
                    var names = payload.characters.Where(entry => entry.Count > 0).Select(entry => entry[0]).ToArray();
                    _presenceTracker.AddToInitialList(names);
                    _logger.LogInformation("[F-Chat]: LIS added {Count} characters to the presence tracker.", names.Length);
                }
                else
                {
                    _logger.LogWarning("[F-Chat]: LIS payload did not contain a parsable 'characters' list: {Json}", json);
                }
                return;
            }
            case "NLN":
            {
                var payload = JsonSerializer.Deserialize<NLN>(json);
                _presenceTracker.MarkOnline(payload.identity);
                await GloryholeEventPublisher.PublishAsync(_outboundMessageQueue, _gloryholeManager.MarkReconnected(payload.identity));
                return;
            }
            case "FLN":
            {
                var payload = JsonSerializer.Deserialize<FLN>(json);
                _presenceTracker.MarkOffline(payload.character);
                await GloryholeEventPublisher.PublishAsync(_outboundMessageQueue, _gloryholeManager.MarkDisconnected(payload.character));
                return;
            }
            case "TPN":
            {
                var payload = JsonSerializer.Deserialize<TPN>(json);
                if (payload.status == "typing")
                {
                    await GloryholeEventPublisher.PublishAsync(_outboundMessageQueue, _gloryholeManager.MarkTypingActivity(payload.character));
                }
                return;
            }
            case "ERR":
            {
                var payload = JsonSerializer.Deserialize<ERR>(json);
                var code = Enum.IsDefined(typeof(FChatErrorCode), payload.number)
                    ? ((FChatErrorCode)payload.number).ToString()
                    : "Unrecognized";

                _logger.LogError("[F-Chat]: ERR {Number} ({Code}): {Message}",
                    payload.number, code, payload.message);
                return;
            }
            default:
                return;
        }

        _logger.LogInformation("[F-Chat]: {Protocol}: {Json}", protocol, json);
        await _inboundMessageQueue.EnqueueAsync(inboundMessage);
    }


    private static (string Protocol, string PayloadJson) ParsePacket(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("[F-Chat]: Incoming packet was empty.", nameof(raw));

        var firstSpace = raw.IndexOf(' ');
        if (firstSpace < 0)
        {
            return (raw.Trim(), string.Empty);
        }

        var protocol = raw[..firstSpace].Trim();
        var payloadJson = raw[(firstSpace + 1)..].Trim();

        return (protocol, payloadJson);
    }
}