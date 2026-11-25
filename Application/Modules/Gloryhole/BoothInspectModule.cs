using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

// Moderator-only window into a booth's true occupants - everywhere else (!get_gloryholes,
// !status) occupants are deliberately anonymized behind "Gloryhole #N".
public class BoothInspectModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IModeratorRepository _moderatorRepository;

    public override bool HasHelpfulInfo => true;
    public override bool ModeratorOnly => true;
    public override string Command => "booth_inspect";
    public override string CommandDescription => "(Moderators only) See who is occupying and participating in a booth. Usage: !booth_inspect <booth_number>";

    public BoothInspectModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IModeratorRepository moderatorRepository)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
        _moderatorRepository = moderatorRepository;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;

        if (!await _moderatorRepository.IsModeratorAsync(character))
        {
            await SendPrivateAsync(character, "You do not have the privilege to use this command.");
            return;
        }

        var parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 1 || !int.TryParse(parts[0], out var boothNumber))
        {
            await SendPrivateAsync(character, "Usage: !booth_inspect <booth_number>");
            return;
        }

        string text;

        lock (_gloryholeManager.SyncRoot)
        {
            if (boothNumber < 1 || boothNumber > _gloryholeManager.Booths.Count)
            {
                text = $"Invalid booth number. Choose 1-{_gloryholeManager.Booths.Count}.";
            }
            else
            {
                var booth = _gloryholeManager.Booths[boothNumber - 1];

                var occupants = booth.Occupants.Count > 0
                    ? string.Join(", ", booth.Occupants.Select(o => o == booth.Owner ? $"{o} (owner)" : o))
                    : "None";
                var participants = booth.Participants.Count > 0 ? string.Join(", ", booth.Participants) : "None";
                var waiting = booth.WaitingList.Count > 0 ? string.Join(", ", booth.WaitingList) : "None";
                var door = booth.IsOpen ? "[b][color=green]OPEN[/color][/b]" : "[b][color=red]CLOSED[/color][/b]";

                text =
                    "\n" +
                    $"[color=pink]==== Booth #{booth.Number} Inspection ====[/color]\n" +
                    $"Status: {door}\n" +
                    $"Occupants: {occupants}\n" +
                    $"Participants: {participants}\n" +
                    $"Waiting List: {waiting}\n";
            }
        }

        await SendPrivateAsync(character, text);
    }
}
