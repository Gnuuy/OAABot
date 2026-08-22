using Application.Interfaces;
using Domain;
using Domain.Enums;
using Domain.Gloryhole;

namespace Application.Modules.Gloryhole;

public class SetHolesModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IGloryholeProfileRepository _profileRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "set_holes";
    public override string CommandDescription => "Set the number of available participant holes for your booth.";

    public SetHolesModule(
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
        var parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 1 || !int.TryParse(parts[0], out var holes) || holes < 1)
        {
            await SendPrivateAsync(character, "Usage: !set_holes <number_of_holes> (must be at least 1)");
            return;
        }

        string? error = null;
        string? confirmation = null;
        string? description = null;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.IsOwner(character));
            if (booth is null)
            {
                error = "You must be the owner of a booth to change the number of holes.";
            }
            else
            {
                booth.AvailableHoles = holes;
                description = booth.Description;
                confirmation = $"Booth #{booth.Number} now has {holes} available hole{(holes != 1 ? "s" : "")}.";
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
            Description = description!,
            MaxParticipants = holes,
            PreferredGenders = existingProfile?.PreferredGenders ?? [],
            EquippedItems = existingProfile?.EquippedItems ?? new Dictionary<string, string>()
        });

        await SendPrivateAsync(character, confirmation!);
    }
}
