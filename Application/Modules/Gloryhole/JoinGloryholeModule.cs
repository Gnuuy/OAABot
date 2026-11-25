using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class JoinGloryholeModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IBannedUserRepository _bannedUserRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "join_gloryhole";
    public override string CommandDescription => "Join an occupied, open booth as a participant.";

    public JoinGloryholeModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IBannedUserRepository bannedUserRepository)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
        _bannedUserRepository = bannedUserRepository;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Public;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;
        var channelId = message.ChannelId ?? _gloryholeManager.ChannelId;

        if (await _bannedUserRepository.IsBannedAsync(character))
        {
            await SendPrivateAsync(character, "You are banned from using the booths and gloryholes.");
            return;
        }

        var parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 1 || !int.TryParse(parts[0], out var boothNumber))
        {
            await SendChannelAsync(channelId, "[b]Usage:[/b] !join_gloryhole <number>");
            return;
        }

        string? channelError = null;
        string? pmError = null;
        List<string> occupantsToNotify = [];
        string? channelMessage = null;

        lock (_gloryholeManager.SyncRoot)
        {
            if (boothNumber < 1 || boothNumber > _gloryholeManager.Booths.Count)
            {
                channelError = $"Invalid Gloryhole number. Choose 1-{_gloryholeManager.Booths.Count}.";
            }
            else
            {
                var booth = _gloryholeManager.Booths[boothNumber - 1];

                if (_gloryholeManager.Booths.Any(b => b.Participants.Contains(character)))
                {
                    channelError = "You're already using a gloryhole! How many cocks do you think you have!?";
                }
                else if (booth.Occupants.Count == 0)
                {
                    channelError = $"Gloryhole #{booth.Number} is empty. Ask someone to occupy it or get in there yourself by DMing !enter_booth {booth.Number}.";
                }
                else if (!booth.IsOpen)
                {
                    pmError = $"Gloryhole #{booth.Number} isn't open yet. The occupant is still preparing.";
                }
                else if (booth.Participants.Count >= booth.AvailableHoles)
                {
                    pmError = $"Gloryhole #{booth.Number} is full right now ([{booth.Participants.Count}/{booth.AvailableHoles}]). Try again soon.";
                }
                else if (!booth.ClaimHole(character))
                {
                    channelError = $"Could not join Gloryhole #{booth.Number} at this time.";
                }
                else
                {
                    occupantsToNotify = booth.Occupants.ToList();
                    channelMessage = $"{character} has joined Gloryhole #{booth.Number}.";
                }
            }
        }

        if (channelError is not null)
        {
            await SendChannelAsync(channelId, channelError);
            return;
        }

        if (pmError is not null)
        {
            await SendPrivateAsync(character, pmError);
            return;
        }

        foreach (var occ in occupantsToNotify)
            await SendPrivateAsync(occ, "A shadow flickers on the other side. Seems you have another customer!");

        await SendChannelAsync(channelId, channelMessage!);
    }
}
