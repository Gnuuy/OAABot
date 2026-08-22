using Domain.Enums;

namespace Domain;

public class InboundMessage
{
    public string Command { get; set;  }
    public MessageType Type { get; set; }
    public string? CharacterName { get; set; }
    public string? ChannelId { get; set; }
    public string Message { get; set; }

    // The exact text as sent, before CommandParser strips the "!command" prefix off Message -
    // needed anywhere the untouched original matters, e.g. logging.
    public string RawMessage { get; set; } = string.Empty;
}