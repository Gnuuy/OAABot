using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class JoinBoothModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IBannedUserRepository _bannedUserRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "join_booth";
    public override string CommandDescription => "Request to join an already occupied booth (adds you to its waiting list).";

    public JoinBoothModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IBannedUserRepository bannedUserRepository)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
        _bannedUserRepository = bannedUserRepository;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;

        if (await _bannedUserRepository.IsBannedAsync(character))
        {
            await SendPrivateAsync(character, "You are banned from using the booths and gloryholes.");
            return;
        }

        var parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 1 || !int.TryParse(parts[0], out var boothNumber))
        {
            await SendPrivateAsync(character, "Usage: !join_booth <booth_number>");
            return;
        }

        string? error = null;
        string? confirmation = null;
        string? ownerNotifyTarget = null;
        string? ownerMessage = null;

        lock (_gloryholeManager.SyncRoot)
        {
            if (boothNumber < 1 || boothNumber > _gloryholeManager.Booths.Count)
            {
                error = $"Invalid booth number. Choose 1-{_gloryholeManager.Booths.Count}.";
            }
            else
            {
                var booth = _gloryholeManager.Booths[boothNumber - 1];

                if (booth.Occupants.Count == 0)
                {
                    error = $"Booth #{booth.Number} is empty. If you want to host, use !enter_booth {booth.Number}.";
                }
                else
                {
                    var currentBooth = _gloryholeManager.Booths.FirstOrDefault(
                        b => b.Occupants.Contains(character) || b.Participants.Contains(character));

                    if (currentBooth is not null)
                    {
                        error = $"You're already in Booth #{currentBooth.Number}. Leave that booth first.";
                    }
                    else if (booth.WaitingList.Contains(character))
                    {
                        error = $"You've already requested to join Booth #{booth.Number}. Please wait for approval.";
                    }
                    else
                    {
                        booth.WaitingList.Add(character);
                        ownerNotifyTarget = booth.Owner.Length > 0 ? booth.Owner : booth.Occupants[0];
                        ownerMessage =
                            $"Someone would like to join your Booth #{booth.Number}.\n" +
                            $"Waiting list now: {booth.WaitingList.Count}.\n" +
                            "Use !accept to accept everyone waiting.";
                        confirmation = $"Request sent to the owner of Booth #{booth.Number}. You've been added to the waiting list.";
                    }
                }
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        await SendPrivateAsync(ownerNotifyTarget!, ownerMessage!);
        await SendPrivateAsync(character, confirmation!);
    }
}
