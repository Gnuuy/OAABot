using Application.Interfaces;
using Domain;
using Domain.Enums;
using Domain.Gloryhole;

namespace Application.Modules.Gloryhole;

// Passive: any channel message from a participant resets their AFK timer. Occupants reset theirs
// via typing (TPN) instead, handled directly in FChatService - see IGloryholeManager.MarkTypingActivity.
public class GloryholeActivityModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => false;
    public override string Command => string.Empty;

    public GloryholeActivityModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) => message.Type == MessageType.Public;

    public override async Task HandleAsync(InboundMessage message)
    {
        if (message.CharacterName is null)
            return;

        foreach (var ev in _gloryholeManager.MarkMessageActivity(message.CharacterName))
        {
            if (ev.Kind == GHEventKind.Pm && ev.Recipient is not null)
                await SendPrivateAsync(ev.Recipient, ev.Message);
            else if (ev.Kind == GHEventKind.Channel && ev.Channel is not null)
                await SendChannelAsync(ev.Channel, ev.Message);
        }
    }
}
