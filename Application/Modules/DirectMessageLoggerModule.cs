using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules;

// Passive: logs every DM sent to the bot to Data/logs/{character}.log, one file per sender -
// matches chat_write_to_logfile.py's format and per-character filing, scoped to PMs only.
public class DirectMessageLoggerModule : ModuleBase
{
    private readonly IChatTranscriptLogger _transcriptLogger;

    public override bool HasHelpfulInfo => false;
    public override string Command => string.Empty;

    public DirectMessageLoggerModule(IOutboundMessageQueue outboundQueue, IChatTranscriptLogger transcriptLogger)
        : base(outboundQueue)
    {
        _transcriptLogger = transcriptLogger;
    }

    public override bool CanHandle(InboundMessage message) => message.Type == MessageType.Private;

    public override Task HandleAsync(InboundMessage message)
    {
        if (message.CharacterName is null)
            return Task.CompletedTask;

        return _transcriptLogger.LogAsync(message.CharacterName, message.RawMessage);
    }
}
