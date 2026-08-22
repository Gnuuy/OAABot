namespace Domain;

public class GloryholeProfile
{
    public string Description { get; set; } = string.Empty;
    public int MaxParticipants { get; set; } = 1;
    public List<string> PreferredGenders { get; set; } = [];
    public Dictionary<string, string> EquippedItems { get; set; } = new();
}
