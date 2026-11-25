using Domain.Gloryhole;

namespace Application.Interfaces;

public interface IGloryholeManager
{
    IReadOnlyList<Booth> Booths { get; }

    // The single room the gloryhole system operates in - lets PM-triggered commands (which have
    // no channel of their own) announce to the room without Application needing to know about
    // Presentation's FChatConfig.
    string ChannelId { get; }

    // A single monitor shared by every AFK/disconnect sweep the manager runs internally.
    // Command modules that mutate a Booth returned from a finder below must lock on this too -
    // otherwise a periodic AFK/disconnect sweep can enumerate a booth's occupant/participant
    // lists at the exact moment a command mutates them, which throws in .NET (unlike Python's
    // more forgiving list iteration, which is what let the reference bot get away without any
    // locking at all).
    object SyncRoot { get; }

    Booth? FindBoothByOccupant(string user);
    Booth? FindBoothByParticipant(string user);
    Booth? FindBoothById(int number);

    // Moderator-driven removal: finds the user wherever they are (occupant or participant) and
    // removes them, producing the same style of notifications as an AFK kick. `reason` is a
    // clause appended to "You were removed from Booth #N ..." (e.g. "by a moderator"). Returns an
    // empty list if the user isn't currently in any booth.
    IReadOnlyList<GHEvent> RemoveCharacter(string user, string reason);

    // Occupants only reset their AFK timer by typing (TPN), never by sending a message.
    IReadOnlyList<GHEvent> MarkTypingActivity(string user);

    // Participants only reset their AFK timer by sending a channel message, never a PM.
    IReadOnlyList<GHEvent> MarkMessageActivity(string user);

    // Periodic sweep: warns/kicks anyone idle past the AFK thresholds. Call this on a fixed tick.
    IReadOnlyList<GHEvent> ProcessAfkOnce();

    // F-Chat presence transitions (FLN/NLN) start/cancel a reconnect grace window per person.
    IReadOnlyList<GHEvent> MarkDisconnected(string character);
    IReadOnlyList<GHEvent> MarkReconnected(string character);

    // Periodic sweep: removes anyone whose disconnect grace window has expired.
    IReadOnlyList<GHEvent> ProcessDisconnectTimeouts();
}
