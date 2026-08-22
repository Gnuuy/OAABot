using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class LeaveGloryholeModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "leave_gloryhole";
    public override string CommandDescription => "Leave the gloryhole you're currently using.";

    public LeaveGloryholeModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Public;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;

        string? error = null;
        string? confirmation = null;
        List<string> occupantsToNotify = [];

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.Participants.Contains(character));
            if (booth is null)
            {
                error = "You're not currently using any gloryhole.";
            }
            else if (!booth.LeaveHole(character))
            {
                error = $"Couldn't leave Gloryhole #{booth.Number} right now. Try again.";
            }
            else
            {
                confirmation = $"{character} has stepped away from Gloryhole #{booth.Number}.";
                occupantsToNotify = booth.Occupants.ToList();
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        await SendPrivateAsync(character, confirmation!);

        foreach (var occ in occupantsToNotify)
            await SendPrivateAsync(occ, "Shadows disperse and light once more shine through the empty socket. A customer has left!");
    }
}
