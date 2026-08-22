using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class UnequipItemModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IBoothItemCatalog _catalog;
    private readonly IGloryholeProfileRepository _profileRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "unequip";
    public override string CommandDescription => "Remove a cosmetic from the booth you own. Usage: !unequip <item id>";

    public UnequipItemModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IBoothItemCatalog catalog,
        IGloryholeProfileRepository profileRepository)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
        _catalog = catalog;
        _profileRepository = profileRepository;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;
        var itemId = message.Message.Trim();

        if (itemId.Length == 0)
        {
            await SendPrivateAsync(character, "Usage: !unequip <item id> - see !shop for ids, or !status to see what's currently equipped.");
            return;
        }

        var item = await _catalog.FindByIdAsync(itemId);
        if (item is null)
        {
            await SendPrivateAsync(character, $"No item called \"{itemId}\" - check !shop for valid ids.");
            return;
        }

        string? error = null;
        var boothNumber = 0;
        Dictionary<string, string>? equippedSnapshot = null;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.IsOwner(character));
            if (booth is null)
            {
                error = "You must be the owner of a booth to unequip cosmetics.";
            }
            else if (!booth.EquippedItems.TryGetValue(item.Category, out var equippedValue) || equippedValue != item.Value)
            {
                error = $"You don't have {item.Name} equipped.";
            }
            else
            {
                booth.EquippedItems.Remove(item.Category);
                boothNumber = booth.Number;
                equippedSnapshot = new Dictionary<string, string>(booth.EquippedItems);
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        // Keep the saved profile in sync, same as !equip does.
        var existingProfile = await _profileRepository.GetProfileAsync(character);
        await _profileRepository.SaveProfileAsync(character, new GloryholeProfile
        {
            Description = existingProfile?.Description ?? string.Empty,
            MaxParticipants = existingProfile?.MaxParticipants ?? 1,
            PreferredGenders = existingProfile?.PreferredGenders ?? [],
            EquippedItems = equippedSnapshot!
        });

        await SendPrivateAsync(character, $"Unequipped {item.Name} from Booth #{boothNumber}.");
    }
}
