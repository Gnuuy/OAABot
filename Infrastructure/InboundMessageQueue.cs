using System.Threading.Channels;
using Application.Interfaces;
using Domain;

namespace Infrastructure;

public class InboundMessageQueue : IInboundMessageQueue
{
    private readonly Channel<InboundMessage> _channel =
        Channel.CreateUnbounded<InboundMessage>();

    public ValueTask EnqueueAsync(InboundMessage message)
    {
        return _channel.Writer.WriteAsync(message);
    }

    public ValueTask<InboundMessage> DequeueAsync()
    {
        return _channel.Reader.ReadAsync();
    }
}