using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

// Passive: forwards an occupant's "/me ..." PM either to their fellow occupants privately, or to
// the public channel (anonymized behind the booth number) if their booth has participants.
public class BoothToGloryholeMessengerModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => false;
    public override string Command => string.Empty;

    public BoothToGloryholeMessengerModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) =>
        message.Type == MessageType.Private && message.Message.StartsWith("/me", StringComparison.Ordinal);

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        var emoteBody = message.Message[3..].TrimStart();

        string? notOccupantError = null;
        List<string> internalRecipients = [];
        string? internalMessage = null;
        string? publicMessage = null;
        var warnNoRecipients = false;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.Occupants.Contains(character));
            if (booth is null)
            {
                notOccupantError = "You're not currently occupying a booth.";
            }
            else
            {
                var occIndex = booth.Occupants.IndexOf(character) + 1;
                var occLabel = $"Occupant #{occIndex}";

                internalRecipients = booth.Occupants.Where(o => o != character).ToList();
                if (internalRecipients.Count > 0)
                    internalMessage = $"/me [{occLabel}] {emoteBody}";

                if (booth.Participants.Count > 0)
                {
                    var occDisplay = booth.Occupants.Count > 1 ? $" [{occLabel}]" : "";
                    var participantsTag = $"[color=green][Participants: \"{string.Join(", ", booth.Participants)}\"][/color]";
                    publicMessage = $"/me {participantsTag} Gloryhole #{booth.Number}{occDisplay}: {emoteBody}";
                }
                else if (internalRecipients.Count == 0)
                {
                    warnNoRecipients = true;
                }
            }
        }

        if (notOccupantError is not null)
        {
            await SendPrivateAsync(character, notOccupantError);
            return;
        }

        if (internalMessage is not null)
        {
            foreach (var rec in internalRecipients)
                await SendPrivateAsync(rec, internalMessage);
        }

        if (publicMessage is not null)
        {
            await SendChannelAsync(_gloryholeManager.ChannelId, publicMessage);
        }
        else if (warnNoRecipients)
        {
            await SendPrivateAsync(character, "There's no one to receive your emote yet. Wait for another occupant or a participant.");
        }
    }
}
