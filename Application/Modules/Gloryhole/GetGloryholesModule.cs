using Application.Interfaces;
using Domain;
using Domain.Enums;
using Domain.Gloryhole;

namespace Application.Modules.Gloryhole;

public class GetGloryholesModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "get_gloryholes";
    public override string CommandDescription => "Get a list of all available gloryholes!";

    public GetGloryholesModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Public;

    public override async Task HandleAsync(InboundMessage message)
    {
        if (message.ChannelId is null)
            return;

        string text;

        lock (_gloryholeManager.SyncRoot)
        {
            var lines = new List<string> { "Available gloryholes:\n" };
            Booth? firstEmpty = null;

            foreach (var booth in _gloryholeManager.Booths)
            {
                if (booth.Occupants.Count == 0 && firstEmpty is null)
                    firstEmpty = booth;

                // Skip PREPPING booths (occupied, not yet open).
                if (booth.Occupants.Count > 0 && !booth.IsOpen)
                    continue;

                if (booth.Occupants.Count > 0 && booth.IsOpen)
                {
                    var graffiti = string.Join(", ", booth.Scribbles);
                    var descLine = booth.Description.Length > 0 ? $"\nDescription: \"{booth.Description}\"" : "";
                    var prefLabel = string.Join("/", booth.PreferredGenders);
                    var status = booth.Participants.Count == 0
                        ? "[b][color=yellow]WAITING[/color][/b]"
                        : "[b][color=green]IN USE[/color][/b]";
                    var participantsInfo = booth.Participants.Count > 0
                        ? $" [color=green][Participants: \"{string.Join(", ", booth.Participants)}\"][/color]"
                        : "";

                    lines.Add(
                        $"({booth.Participants.Count}/{booth.AvailableHoles})({prefLabel}) {FormatBoothLabel(booth)} - {status}{participantsInfo} - Scribbles: \"{graffiti}\"{descLine}");
                }
            }

            if (firstEmpty is not null)
            {
                var graffiti = string.Join(", ", firstEmpty.Scribbles);
                var descLine = firstEmpty.Description.Length > 0 ? $"\nDescription: \"{firstEmpty.Description}\"" : "";
                var prefLabel = string.Join("/", firstEmpty.PreferredGenders);

                lines.Add(
                    $"(0/{firstEmpty.AvailableHoles})({prefLabel}) {FormatBoothLabel(firstEmpty)} - [b]EMPTY[/b] - Scribbles: \"{graffiti}\"{descLine}");
            }

            text = "[spoiler]" + string.Join("\n", lines) + "[/spoiler]";
        }

        await SendChannelAsync(message.ChannelId, text);
    }

    // Renders "Gloryhole #N" wrapped/decorated by whatever cosmetics the owner has equipped -
    // "color" wraps it in [color=Value][/color], "icon" bookends it with Value on both sides.
    // A booth with nothing equipped (including any EMPTY booth, since ResetBooth clears these)
    // just renders plain.
    private static string FormatBoothLabel(Booth booth)
    {
        var label = $"Gloryhole #{booth.Number}";

        if (booth.EquippedItems.TryGetValue("color", out var color))
            label = $"[color={color}]{label}[/color]";

        if (booth.EquippedItems.TryGetValue("icon", out var icon))
            label = $"{icon}{label}{icon}";

        return label;
    }
}
