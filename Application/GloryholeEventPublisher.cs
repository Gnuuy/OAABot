using Application.Interfaces;
using Domain;
using Domain.Enums;
using Domain.Gloryhole;

namespace Application;

// Shared by both FChatService (ad-hoc TPN/NLN/FLN activity) and the periodic AFK/disconnect
// sweep worker, since both need to turn a batch of GHEvents into actual outbound chat traffic.
public static class GloryholeEventPublisher
{
    public static async Task PublishAsync(IOutboundMessageQueue outboundQueue, IReadOnlyList<GHEvent> events)
    {
        foreach (var ev in events)
        {
            switch (ev.Kind)
            {
                case GHEventKind.Pm when ev.Recipient is not null:
                    await outboundQueue.EnqueueAsync(new OutboundMessage
                    {
                        Type = MessageType.Private,
                        Character = ev.Recipient,
                        Message = ev.Message
                    });
                    break;

                case GHEventKind.Channel when ev.Channel is not null:
                    await outboundQueue.EnqueueAsync(new OutboundMessage
                    {
                        Type = MessageType.Public,
                        ChannelId = ev.Channel,
                        Message = ev.Message
                    });
                    break;
            }
        }
    }
}
