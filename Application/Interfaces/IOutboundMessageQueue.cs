using Domain;

namespace Application.Interfaces;

public interface IOutboundMessageQueue
{
    ValueTask EnqueueAsync(OutboundMessage message);
    ValueTask<OutboundMessage> DequeueAsync();
}