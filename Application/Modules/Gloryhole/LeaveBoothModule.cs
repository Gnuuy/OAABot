using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class LeaveBoothModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "leave_booth";
    public override string CommandDescription => "Leave the booth you are currently occupying.";

    public LeaveBoothModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;

        string? error = null;
        string? confirmation = null;
        string? remainingMessage = null;
        List<string> remainingToNotify = [];
        string? vacancyAnnouncement = null;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.Occupants.Contains(character));
            if (booth is null)
            {
                error = "You are not currently occupying any booth.";
            }
            else
            {
                var priorOccupants = booth.Occupants.ToList();
                var wasOpen = booth.IsOpen;
                var boothNumber = booth.Number;

                if (!booth.Leave(character))
                {
                    error = "Couldn't leave the booth. Try again.";
                }
                else
                {
                    confirmation = $"You have left Booth #{boothNumber}.";
                    remainingMessage = $"An occupant has left Booth #{boothNumber}.";
                    remainingToNotify = priorOccupants.Where(o => o != character && booth.Occupants.Contains(o)).ToList();

                    // Captured wasOpen BEFORE Leave() ran: if that was the last occupant, Leave()
                    // already reset IsOpen to false, which would otherwise silently suppress this
                    // announcement every time - the exact case it exists for.
                    if (booth.Occupants.Count == 0 && wasOpen)
                        vacancyAnnouncement = $"Gloryhole #{boothNumber} is once more vacant.";
                }
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        await SendPrivateAsync(character, confirmation!);

        foreach (var occ in remainingToNotify)
            await SendPrivateAsync(occ, remainingMessage!);

        if (vacancyAnnouncement is not null)
            await SendChannelAsync(_gloryholeManager.ChannelId, vacancyAnnouncement);
    }
}
