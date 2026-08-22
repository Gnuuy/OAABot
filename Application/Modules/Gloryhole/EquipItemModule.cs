using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class EquipItemModule : ModuleBase
{
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IBoothItemCatalog _catalog;
    private readonly IOwnedBoothItemsRepository _ownedItems;
    private readonly IGloryholeProfileRepository _profileRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "equip";
    public override string CommandDescription => "Apply an owned booth cosmetic to the booth you own. Usage: !equip <item id>";

    public EquipItemModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IBoothItemCatalog catalog,
        IOwnedBoothItemsRepository ownedItems,
        IGloryholeProfileRepository profileRepository)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
        _catalog = catalog;
        _ownedItems = ownedItems;
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
            await SendPrivateAsync(character, "Usage: !equip <item id> - see !shop for the list, !buy it first if you don't own it yet.");
            return;
        }

        var item = await _catalog.FindByIdAsync(itemId);
        if (item is null)
        {
            await SendPrivateAsync(character, $"No item called \"{itemId}\" - check !shop for valid ids.");
            return;
        }

        var owned = await _ownedItems.GetOwnedItemIdsAsync(character);
        if (!owned.Contains(item.Id))
        {
            await SendPrivateAsync(character, $"You don't own {item.Name} yet. Use !buy {item.Id} first.");
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
                error = "You must be the owner of a booth to equip cosmetics.";
            }
            else
            {
                booth.EquippedItems[item.Category] = item.Value;
                boothNumber = booth.Number;
                equippedSnapshot = new Dictionary<string, string>(booth.EquippedItems);
            }
        }

        if (error is not null)
        {
            await SendPrivateAsync(character, error);
            return;
        }

        // Carry the equip choice into the saved profile too, same as description/holes/genders,
        // so it survives a !leave_booth -> !enter_booth round trip.
        var existingProfile = await _profileRepository.GetProfileAsync(character);
        await _profileRepository.SaveProfileAsync(character, new GloryholeProfile
        {
            Description = existingProfile?.Description ?? string.Empty,
            MaxParticipants = existingProfile?.MaxParticipants ?? 1,
            PreferredGenders = existingProfile?.PreferredGenders ?? [],
            EquippedItems = equippedSnapshot!
        });

        await SendPrivateAsync(character, $"Equipped {item.Name} on Booth #{boothNumber}.");
    }
}
