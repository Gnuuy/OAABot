using Application.Interfaces;
using Domain;
using Domain.Enums;
using Domain.Gloryhole;

namespace Application.Modules.Gloryhole;

public class EnterBoothModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IGloryholeProfileRepository _profileRepository;
    private readonly IBannedUserRepository _bannedUserRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "enter_booth";
    public override string CommandDescription => "Enter a booth.";

    public EnterBoothModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IGloryholeProfileRepository profileRepository,
        IBannedUserRepository bannedUserRepository)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
        _profileRepository = profileRepository;
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
            await SendPrivateAsync(character, "Usage: !enter_booth <booth_number>");
            return;
        }

        string? error = null;
        Booth? entered = null;

        lock (_gloryholeManager.SyncRoot)
        {
            if (boothNumber < 1 || boothNumber > _gloryholeManager.Booths.Count)
            {
                error = $"Invalid booth number. Choose 1-{_gloryholeManager.Booths.Count}.";
            }
            else
            {
                var booth = _gloryholeManager.Booths[boothNumber - 1];
                var currentBooth = _gloryholeManager.Booths.FirstOrDefault(
                    b => b.Occupants.Contains(character) || b.Participants.Contains(character));

                if (currentBooth is not null)
                {
                    error = $"You are already in Booth #{currentBooth.Number}. Leave that booth first.";
                }
                else if (booth.Occupants.Count > 0)
                {
                    error = $"Booth #{booth.Number} is already occupied.";
                }
                else if (!booth.Enter(character))
                {
                    error = $"Could not enter Booth #{booth.Number}.";
                }
                else
                {
                    entered = booth;
                }
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        var profile = await _profileRepository.GetProfileAsync(character);
        if (profile is not null)
        {
            lock (_gloryholeManager.SyncRoot)
            {
                entered!.Description = profile.Description;
                entered.AvailableHoles = profile.MaxParticipants;

                if (profile.PreferredGenders.Count > 0)
                    entered.SetPreferredGenders(profile.PreferredGenders);

                foreach (var (category, value) in profile.EquippedItems)
                    entered.EquippedItems[category] = value;
            }
        }

        var enterMessage =
            "\n" +
            $"[color=pink]You enter Booth #{entered!.Number}, slide the door shut, and set the sign to " +
            "[color=yellow]PREPPING[/color]. Time to do your setup![/color]\n\n" +
            "Tip: You can set your preferred genders with !set_genders <male female transgender etc>.\n" +
            "Tip: You can set your available service holes with !set_holes <number>.\n" +
            "Tip: Set a booth description with !set_description <text>\n\n" +
            "Once you're ready you can use !open to open up for business!\n" +
            "If you'd like to see the status of your booth use the !status command\n\n" +
            "Leave anytime with !leave_booth";

        await SendPrivateAsync(character, enterMessage);
    }
}
