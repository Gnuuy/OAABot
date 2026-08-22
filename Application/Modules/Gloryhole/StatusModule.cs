using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class StatusModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "status";
    public override string CommandDescription => "Check the status of the booth you're currently in.";

    public StatusModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
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
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.Occupants.Contains(character))
                        ?? _gloryholeManager.Booths.FirstOrDefault(b => b.Participants.Contains(character));

            if (booth is null)
            {
                text = "You are not currently in any booth.";
            }
            else
            {
                var participantsList = booth.Participants.Count > 0 ? string.Join(", ", booth.Participants) : "None";
                var desc = booth.Description.Length > 0 ? $"\"{booth.Description}\"" : "(none)";
                var pref = booth.PreferredGenders.Count > 0 ? string.Join("/", booth.PreferredGenders) : "[color=green]ALL[/color]";
                var door = booth.IsOpen ? "[b][color=green]OPEN[/color][/b]" : "[b][color=red]CLOSED[/color][/b]";

                text =
                    "\n" +
                    "[color=pink]==== Booth Status ====[/color]\n" +
                    $"Occupants: {booth.Occupants.Count}\n" +
                    $"Participants: {participantsList}\n\n" +
                    $"Preference: ({pref})\n" +
                    $"Status: {door}\n" +
                    $"Available Holes: {booth.AvailableHoles - booth.Participants.Count}/{booth.AvailableHoles}\n\n" +
                    $"Description: {desc}\n";
            }
        }

        await SendPrivateAsync(character, text);
    }
}
