using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class BuyItemModule : ModuleBase
{
    private readonly IBoothItemCatalog _catalog;
    private readonly IOwnedBoothItemsRepository _ownedItems;
    private readonly IBlowbucksRepository _blowbucks;

    public override bool HasHelpfulInfo => true;
    public override string Command => "buy";
    public override string CommandDescription => "Buy a booth cosmetic with Blowbucks. Usage: !buy <item id> (see !shop)";

    public BuyItemModule(
        IOutboundMessageQueue outboundQueue,
        IBoothItemCatalog catalog,
        IOwnedBoothItemsRepository ownedItems,
        IBlowbucksRepository blowbucks)
        : base(outboundQueue)
    {
        _catalog = catalog;
        _ownedItems = ownedItems;
        _blowbucks = blowbucks;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;
        var itemId = message.Message.Trim();

        if (itemId.Length == 0)
        {
            await SendPrivateAsync(character, "Usage: !buy <item id> - see !shop for the list.");
            return;
        }

        var item = await _catalog.FindByIdAsync(itemId);
        if (item is null)
        {
            await SendPrivateAsync(character, $"No item called \"{itemId}\" - check !shop for valid ids.");
            return;
        }

        var owned = await _ownedItems.GetOwnedItemIdsAsync(character);
        if (owned.Contains(item.Id))
        {
            await SendPrivateAsync(character, $"You already own {item.Name}. Use !equip {item.Id} to apply it to your booth.");
            return;
        }

        var balance = await _blowbucks.GetBalanceAsync(character);
        if (balance < item.Price)
        {
            await SendPrivateAsync(character, $"{item.Name} costs {item.Price} Blowbucks - you've only got {balance}.");
            return;
        }

        await _blowbucks.AddAsync(character, -item.Price);
        await _ownedItems.AddOwnedItemAsync(character, item.Id);

        await SendPrivateAsync(character, $"Bought {item.Name} for {item.Price} Blowbucks! Use !equip {item.Id} to apply it to your booth.");
    }
}
