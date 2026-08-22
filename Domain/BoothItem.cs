namespace Domain;

// A purchasable booth cosmetic, loaded from Data/booth-items.json. Category groups items that
// occupy the same "slot" on a booth (only one item per category can be equipped at once) - e.g.
// "color" wraps the booth's label in [color=Value][/color], "icon" prefixes/suffixes it with Value.
public class BoothItem
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public string Value { get; set; } = string.Empty;
}
