using Domain;

namespace Application.Interfaces;

public interface IInboundMessageQueue
{
    ValueTask EnqueueAsync(InboundMessage message);
    ValueTask<InboundMessage> DequeueAsync();
}