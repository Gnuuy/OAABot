namespace Domain.Gloryhole;

public class Booth
{
    // Only the gender keys the reference bot's command modules actually normalize input against.
    private static readonly IReadOnlyDictionary<string, string> GenderTags = new Dictionary<string, string>
    {
        ["female"] = "[color=pink]F[/color]",
        ["male"] = "[color=blue]M[/color]",
        ["transgender"] = "[color=orange]T[/color]",
        ["herm"] = "[color=gray]H[/color]",
        ["shemale"] = "[color=purple]S[/color]",
        ["maleherm"] = "[color=cyan]MH[/color]",
        ["cuntboy"] = "[color=green]CB[/color]",
        ["unspecified"] = "[color=black]U[/color]",
        ["all"] = "[color=green]ALL[/color]",
    };

    private static readonly string AllTag = GenderTags["all"];

    public int Number { get; }
    public List<string> Occupants { get; } = [];
    public List<string> Participants { get; } = [];
    public int AvailableHoles { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
    public List<string> Scribbles { get; } = [];
    public List<string> PreferredGenders { get; private set; } = [AllTag];
    public List<string> WaitingList { get; } = [];
    public bool IsOpen { get; set; }
    public string Owner { get; set; } = string.Empty;

    // Keyed by BoothItem.Category (e.g. "color", "icon") - only one item per category can be
    // equipped at a time, so equipping a new one in the same category just overwrites the old.
    public Dictionary<string, string> EquippedItems { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Booth(int number)
    {
        Number = number;
    }

    public bool Enter(string user)
    {
        if (Occupants.Contains(user))
            return false;

        if (Occupants.Count == 0)
            Owner = user;

        Occupants.Add(user);
        return true;
    }

    public bool Leave(string user)
    {
        var index = Occupants.IndexOf(user);
        if (index < 0)
            return false;

        Occupants.RemoveAt(index);

        if (Owner == user)
        {
            if (Occupants.Count > 0)
            {
                var nextIndex = index < Occupants.Count ? index : 0;
                Owner = Occupants[nextIndex];
            }
            else
            {
                ResetBooth();
            }
        }

        return true;
    }

    // Returns the booth to its pristine state once the last occupant leaves.
    private void ResetBooth()
    {
        Owner = string.Empty;
        IsOpen = false;
        Description = string.Empty;
        Participants.Clear();
        WaitingList.Clear();
        EquippedItems.Clear();
        AvailableHoles = 1;
        PreferredGenders = [AllTag];
        Scribbles.Clear();
    }

    public bool ClaimHole(string user)
    {
        if (!Participants.Contains(user) && AvailableHoles > Participants.Count)
        {
            Participants.Add(user);
            return true;
        }

        return false;
    }

    public bool LeaveHole(string user) => Participants.Remove(user);

    public void AddScribble(string message) => Scribbles.Add(message);

    public string? RemoveScribble(int number)
    {
        var index = number - 1;
        if (index < 0 || index >= Scribbles.Count)
            return null;

        var removed = Scribbles[index];
        Scribbles.RemoveAt(index);
        return removed;
    }

    // Wholesale replace, used to restore a saved configuration (!booth_load) rather than the
    // incremental add/remove the live !set_genders command uses.
    public void SetPreferredGenders(IReadOnlyCollection<string> genderTags)
    {
        PreferredGenders = genderTags.Count > 0 ? genderTags.ToList() : [AllTag];
    }

    public bool AddPreferredGender(string gender)
    {
        var tag = ResolveGenderTag(gender);

        if (tag == AllTag)
        {
            if (IsOnlyAll())
                return false;

            PreferredGenders = [AllTag];
            return true;
        }

        if (IsOnlyAll())
        {
            PreferredGenders = [tag];
            return true;
        }

        if (PreferredGenders.Contains(tag))
            return false;

        PreferredGenders.Add(tag);
        return true;
    }

    public bool RemovePreferredGender(string gender)
    {
        var tag = ResolveGenderTag(gender);

        if (tag == AllTag)
        {
            if (IsOnlyAll())
                return false;

            PreferredGenders = [AllTag];
            return true;
        }

        if (!PreferredGenders.Remove(tag))
            return false;

        if (PreferredGenders.Count == 0)
            PreferredGenders = [AllTag];

        return true;
    }

    public bool IsOwner(string user) => user == Owner;

    private bool IsOnlyAll() => PreferredGenders.Count == 1 && PreferredGenders[0] == AllTag;

    private static string ResolveGenderTag(string gender)
    {
        var key = gender.ToLowerInvariant().Replace("-", "").Replace(" ", "");
        if (!GenderTags.TryGetValue(key, out var tag))
            throw new ArgumentException($"Unknown gender: {gender}", nameof(gender));

        return tag;
    }
}
