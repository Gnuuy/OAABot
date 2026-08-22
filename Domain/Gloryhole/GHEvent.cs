namespace Domain.Gloryhole;

// What the AFK/disconnect logic wants to say; the caller decides how to actually send it.
public class GHEvent
{
    public required GHEventKind Kind { get; init; }
    public required string Message { get; init; }
    public string? Channel { get; init; }
    public string? Recipient { get; init; }
}
