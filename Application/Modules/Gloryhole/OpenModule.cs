using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class OpenModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "open";
    public override string CommandDescription => "Open your occupied booth for gloryhole access (owner only).";

    public OpenModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
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
        List<string> occupantsToNotify = [];
        string? occupantMessage = null;
        string? channelMessage = null;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.Owner == character);
            if (booth is null)
            {
                error = "You don't currently own any booth. Use !enter_booth <number> first.";
            }
            else if (booth.Occupants.Count == 0)
            {
                error = $"Booth #{booth.Number} is empty. Occupy it before opening.";
            }
            else if (booth.IsOpen)
            {
                error = $"Booth #{booth.Number} is already open.";
            }
            else
            {
                booth.IsOpen = true;

                var cap = $"({booth.Participants.Count}/{booth.AvailableHoles})";
                var pref = string.Join("/", booth.PreferredGenders);

                occupantsToNotify = booth.Occupants.ToList();
                occupantMessage =
                    "You slide the latch to [b][color=green]OPEN[/color][/b]. " +
                    $"Booth #{booth.Number} is now accepting customers.\n" +
                    $"Capacity: {cap}, Preferred Genders: ({pref}).";
                channelMessage = $"{cap}({pref}) Gloryhole #{booth.Number} is now open for service.";
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        foreach (var occ in occupantsToNotify)
            await SendPrivateAsync(occ, occupantMessage!);

        await SendChannelAsync(_gloryholeManager.ChannelId, channelMessage!);
    }
}
