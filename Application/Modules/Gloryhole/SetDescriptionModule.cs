using Application.Interfaces;
using Domain;
using Domain.Enums;
using Domain.Gloryhole;

namespace Application.Modules.Gloryhole;

public class SetDescriptionModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IGloryholeProfileRepository _profileRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "set_description";
    public override string CommandDescription => "Change the description of the booth you own.";

    public SetDescriptionModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IGloryholeProfileRepository profileRepository)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
        _profileRepository = profileRepository;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;
        var description = message.Message.Trim();

        if (description.Length == 0)
        {
            await SendPrivateAsync(character, "No description provided. Usage: !set_description <text>");
            return;
        }

        string? error = null;
        string? confirmation = null;
        var availableHoles = 0;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.Owner == character);
            if (booth is null)
            {
                error = "You do not currently own any booth.";
            }
            else
            {
                booth.Description = description;
                availableHoles = booth.AvailableHoles;
                confirmation = $"Booth #{booth.Number} description updated to: \"{description}\"";
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        // Preserve whatever genders/equipped items were last explicitly saved - this only
        // updates the description/holes half of the snapshot, not the whole thing.
        var existingProfile = await _profileRepository.GetProfileAsync(character);

        await _profileRepository.SaveProfileAsync(character, new GloryholeProfile
        {
            Description = description,
            MaxParticipants = availableHoles,
            PreferredGenders = existingProfile?.PreferredGenders ?? [],
            EquippedItems = existingProfile?.EquippedItems ?? new Dictionary<string, string>()
        });

        await SendPrivateAsync(character, confirmation!);
    }
}
