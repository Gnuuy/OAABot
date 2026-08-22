using System.Threading.Channels;
using Application.Interfaces;
using Domain;

namespace Infrastructure;

public sealed class OutboundMessageQueue : IOutboundMessageQueue
{
    private readonly Channel<OutboundMessage> _channel =
        Channel.CreateUnbounded<OutboundMessage>();

    public ValueTask EnqueueAsync(OutboundMessage message)
        => _channel.Writer.WriteAsync(message);

    public ValueTask<OutboundMessage> DequeueAsync()
        => _channel.Reader.ReadAsync();
}