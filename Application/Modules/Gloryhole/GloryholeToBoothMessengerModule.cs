using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

// Passive: forwards a participant's "/me ..." channel message privately to everyone else sharing
// their booth - occupants AND fellow participants alike - anonymized as "Participant #N" rather
// than by name.
public class GloryholeToBoothMessengerModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => false;
    public override string Command => string.Empty;

    public GloryholeToBoothMessengerModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) =>
        message.Type == MessageType.Public && message.Message.StartsWith("/me", StringComparison.Ordinal);

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        var emoteBody = message.Message[3..].TrimStart();

        string? nobodyToNotifyError = null;
        List<string> recipients = [];
        string? anonymizedMessage = null;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.Participants.Contains(character));
            if (booth is null)
                return;

            var index = booth.Participants.IndexOf(character);
            var senderId = $"[color=green][Participant {index + 1}][/color]";
            anonymizedMessage = $"/me {senderId} {emoteBody}";

            recipients = booth.Occupants
                .Concat(booth.Participants.Where(p => p != character))
                .Distinct()
                .ToList();

            if (recipients.Count == 0)
                nobodyToNotifyError = "There's no one else in your booth yet. Your emote wasn't forwarded.";
        }

        if (nobodyToNotifyError is not null)
        {
            await SendPrivateAsync(character, nobodyToNotifyError);
            return;
        }

        foreach (var recipient in recipients)
            await SendPrivateAsync(recipient, anonymizedMessage!);
    }
}
