using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class AcceptModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "accept";
    public override string CommandDescription => "Accept all users from your booth's waiting list.";

    public AcceptModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
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
        List<string> accepted = [];
        var boothNumber = 0;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.IsOwner(character));
            if (booth is null)
            {
                error = "You do not currently own any booth.";
            }
            else if (booth.WaitingList.Count == 0)
            {
                error = "There is nobody waiting to join your booth.";
            }
            else
            {
                boothNumber = booth.Number;
                foreach (var user in booth.WaitingList.ToList())
                {
                    booth.Enter(user);
                    booth.WaitingList.Remove(user);
                    accepted.Add(user);
                }

                confirmation = $"Accepted all waiting users into Booth #{boothNumber}.";
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        foreach (var user in accepted)
            await SendPrivateAsync(user, $"You have been accepted into Booth #{boothNumber}!");

        await SendPrivateAsync(character, confirmation!);
    }
}
