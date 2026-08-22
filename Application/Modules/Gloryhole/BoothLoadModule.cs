using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class BoothLoadModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IGloryholeProfileRepository _profileRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "booth_load";
    public override string CommandDescription => "Load your last saved booth configuration into the booth you're in.";

    public BoothLoadModule(
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

        int boothNumber;
        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.IsOwner(character));
            if (booth is null)
            {
                boothNumber = 0;
            }
            else
            {
                boothNumber = booth.Number;
            }
        }

        if (boothNumber == 0)
        {
            await SendPrivateAsync(character, "You must be the owner of a booth to load a saved configuration.");
            return;
        }

        var profile = await _profileRepository.GetProfileAsync(character);
        if (profile is null)
        {
            await SendPrivateAsync(character, "You don't have a saved booth configuration yet. Use !booth_save to create one.");
            return;
        }

        lock (_gloryholeManager.SyncRoot)
        {
            // Re-find the booth: time has passed since the check above (a save/load involves an
            // await in between), so re-verify ownership rather than trusting a stale reference.
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.IsOwner(character));
            if (booth is null)
            {
                boothNumber = 0;
            }
            else
            {
                booth.Description = profile.Description;
                booth.AvailableHoles = profile.MaxParticipants;
                booth.SetPreferredGenders(profile.PreferredGenders);

                booth.EquippedItems.Clear();
                foreach (var (category, value) in profile.EquippedItems)
                    booth.EquippedItems[category] = value;

                boothNumber = booth.Number;
            }
        }

        if (boothNumber == 0)
        {
            await SendPrivateAsync(character, "You must be the owner of a booth to load a saved configuration.");
            return;
        }

        await SendPrivateAsync(character, $"Loaded your saved configuration into Booth #{boothNumber}.");
    }
}
