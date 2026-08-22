using Domain.Enums;

namespace Domain;

public class OutboundMessage
{
    public MessageType Type { get; set; }
    public string? Character { get; set; }
    public string? ChannelId { get; set; }
    public string Message { get; set; }
}