using Application.Interfaces;
using Domain;
using Domain.Enums;
using Domain.Gloryhole;

namespace Application.Modules.Gloryhole;

public class BoothSaveModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IGloryholeProfileRepository _profileRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "booth_save";
    public override string CommandDescription => "Save your current booth's description, holes, preferred genders, and equipped cosmetics for next time.";

    public BoothSaveModule(
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

        string? error = null;
        GloryholeProfile? snapshot = null;
        var boothNumber = 0;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.IsOwner(character));
            if (booth is null)
            {
                error = "You must be the owner of a booth to save its configuration.";
            }
            else
            {
                boothNumber = booth.Number;
                snapshot = new GloryholeProfile
                {
                    Description = booth.Description,
                    MaxParticipants = booth.AvailableHoles,
                    PreferredGenders = booth.PreferredGenders.ToList(),
                    EquippedItems = new Dictionary<string, string>(booth.EquippedItems)
                };
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        await _profileRepository.SaveProfileAsync(character, snapshot!);

        await SendPrivateAsync(character,
            $"Saved Booth #{boothNumber}'s configuration (description, holes, preferred genders, and equipped cosmetics). " +
            "Use !booth_load next time to restore it instantly.");
    }
}
