using System.Text;
using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class ShopModule : ModuleBase
{
    private readonly IBoothItemCatalog _catalog;

    public override bool HasHelpfulInfo => true;
    public override string Command => "shop";
    public override string CommandDescription => "See what booth cosmetics are available to buy with Blowbucks.";

    public ShopModule(IOutboundMessageQueue outboundQueue, IBoothItemCatalog catalog) : base(outboundQueue)
    {
        _catalog = catalog;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        var items = await _catalog.GetAllAsync();
        if (items.Count == 0)
        {
            await SendPrivateAsync(character, "The shop is empty right now.");
            return;
        }

        var sb = new StringBuilder();
        sb.Append("[b]Booth Cosmetics Shop[/b]\n\n");

        foreach (var group in items.GroupBy(i => i.Category, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append($"[b]{CapitalizeFirst(group.Key)}[/b]\n");
            // Shows the literal command to type, rather than a bare id, so it's unambiguous what
            // to copy - paste.
            foreach (var item in group)
                sb.Append($"  !buy {item.Id} - {item.Name} ({item.Price} Blowbucks)\n");
            sb.Append('\n');
        }

        sb.Append("Once you've bought one, use !equip <id> to apply it to your booth (e.g. !equip red).");

        await SendPrivateAsync(character, sb.ToString());
    }

    private static string CapitalizeFirst(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
