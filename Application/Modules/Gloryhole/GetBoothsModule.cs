using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class GetBoothsModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "get_booths";
    public override string CommandDescription => "Get a list of all available booths!";

    public GetBoothsModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;
        string text;

        lock (_gloryholeManager.SyncRoot)
        {
            var lines = new List<string> { "Here are the booths:" };

            foreach (var booth in _gloryholeManager.Booths)
            {
                var status = booth.Occupants.Count == 0
                    ? "[b][color=green]AVAILABLE[/color][/b]"
                    : booth.IsOpen ? "[b][color=red]OCCUPIED[/color][/b]" : "[b][color=yellow]PREPPING[/color][/b]";

                var scribbles = string.Join(", ", booth.Scribbles);
                var description = booth.Description.Length > 0 ? $"\n Description: \"{booth.Description}\"" : "";

                lines.Add($"Booth #{booth.Number} - {status} - scribble: \"{scribbles}\"{description}");
            }

            text = string.Join("\n", lines);
        }

        await SendPrivateAsync(character, text);
    }
}
