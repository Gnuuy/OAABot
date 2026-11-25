using Application.Interfaces;
using Domain.Gloryhole;

namespace Infrastructure;

public sealed class GloryholeManager : IGloryholeManager
{
    private static readonly TimeSpan OccupantWarnThreshold = TimeSpan.FromMinutes(50);
    private static readonly TimeSpan OccupantKickThreshold = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan ParticipantWarnThreshold = TimeSpan.FromMinutes(50);
    private static readonly TimeSpan ParticipantKickThreshold = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan DisconnectGrace = TimeSpan.FromMinutes(10);

    private const string AfkWarnPm =
        "You haven't posted a message in 50 minutes. Post within ten minutes or you will be removed for being AFK.";

    private enum Role { Occupant, Participant }

    private sealed class AfkState
    {
        public DateTimeOffset Last;
        public bool Warned;
    }

    private sealed class DisconnectRecord
    {
        public required DateTimeOffset Deadline;
        public required int BoothNumber;
        public required Role Role;
        public int? OccupantIndex;
    }

    private readonly string _channelId;
    private readonly List<Booth> _booths;
    private readonly Dictionary<string, DisconnectRecord> _disconnects = new();
    private readonly Dictionary<(Role Role, int BoothNumber, string User), AfkState> _afk = new();

    public object SyncRoot { get; } = new();
    public string ChannelId => _channelId;

    public GloryholeManager(string channelId, int initialBooths = 12)
    {
        _channelId = channelId;
        _booths = Enumerable.Range(1, initialBooths).Select(i => new Booth(i)).ToList();
    }

    public IReadOnlyList<Booth> Booths => _booths;

    public Booth? FindBoothByOccupant(string user)
    {
        lock (SyncRoot)
            return _booths.FirstOrDefault(b => b.Occupants.Contains(user));
    }

    public Booth? FindBoothByParticipant(string user)
    {
        lock (SyncRoot)
            return _booths.FirstOrDefault(b => b.Participants.Contains(user));
    }

    public Booth? FindBoothById(int number)
    {
        lock (SyncRoot)
            return _booths.FirstOrDefault(b => b.Number == number);
    }

    public IReadOnlyList<GHEvent> RemoveCharacter(string user, string reason)
    {
        lock (SyncRoot)
        {
            var occupantBooth = _booths.FirstOrDefault(b => b.Occupants.Contains(user));
            if (occupantBooth is not null)
                return RemoveOccupant(occupantBooth, user, reason);

            var participantBooth = _booths.FirstOrDefault(b => b.Participants.Contains(user));
            if (participantBooth is not null)
                return RemoveParticipant(participantBooth, user, reason);

            return [];
        }
    }

    private List<GHEvent> RemoveOccupant(Booth booth, string user, string reason)
    {
        var events = new List<GHEvent>();

        var occupantsBefore = booth.Occupants.Count;
        var wasOpenBefore = booth.IsOpen;
        var participantsBefore = booth.Participants.Count;
        var occIndex = OccupantIndex(booth, user);

        events.Add(Pm(user, $"You were removed from Booth #{booth.Number} {reason}."));

        booth.Leave(user);

        if (occupantsBefore > 1)
        {
            foreach (var other in booth.Occupants)
                events.Add(Pm(other, $"Occupant #{occIndex} was removed {reason}."));
        }

        if (wasOpenBefore && participantsBefore > 0)
        {
            var chMsg = occupantsBefore > 1
                ? $"Occupant #{occIndex} in Booth #{booth.Number} was removed {reason}."
                : $"An occupant in Booth #{booth.Number} was removed {reason}.";

            if (booth.Occupants.Count == 0)
                chMsg += " The booth is now empty.";

            events.Add(Channel(chMsg));
        }

        ClearAfk(Role.Occupant, booth.Number, user);
        _disconnects.Remove(user);

        return events;
    }

    private List<GHEvent> RemoveParticipant(Booth booth, string user, string reason)
    {
        var events = new List<GHEvent>();
        var index = ParticipantIndex(booth, user);

        events.Add(Pm(user, $"You were removed from Booth #{booth.Number} {reason}."));

        booth.LeaveHole(user);

        events.Add(Channel($"{user} was removed from Booth #{booth.Number} {reason}."));

        foreach (var occ in booth.Occupants)
            events.Add(Pm(occ, $"Participant #{index} was removed {reason}."));

        ClearAfk(Role.Participant, booth.Number, user);
        _disconnects.Remove(user);

        return events;
    }

    public IReadOnlyList<GHEvent> MarkTypingActivity(string user)
    {
        lock (SyncRoot)
        {
            var booth = _booths.FirstOrDefault(b => b.Occupants.Contains(user));
            if (booth is null)
                return [];

            return TouchAfk(Role.Occupant, booth, user, DateTimeOffset.UtcNow);
        }
    }

    public IReadOnlyList<GHEvent> MarkMessageActivity(string user)
    {
        lock (SyncRoot)
        {
            var booth = _booths.FirstOrDefault(b => b.Participants.Contains(user));
            if (booth is null)
                return [];

            return TouchAfk(Role.Participant, booth, user, DateTimeOffset.UtcNow);
        }
    }

    private List<GHEvent> TouchAfk(Role role, Booth booth, string user, DateTimeOffset now)
    {
        var events = new List<GHEvent>();
        var key = (role, booth.Number, user);

        var wasWarned = _afk.TryGetValue(key, out var existing) && existing.Warned;

        if (existing is null)
            _afk[key] = new AfkState { Last = now, Warned = false };
        else
            existing.Last = now;

        if (wasWarned)
        {
            existing!.Warned = false;
            events.Add(Pm(user, $"Saw your activity! Your AFK timer for Booth #{booth.Number} has been reset."));
        }

        return events;
    }

    public IReadOnlyList<GHEvent> ProcessAfkOnce()
    {
        lock (SyncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            var events = new List<GHEvent>();
            var disconnectedUsers = new HashSet<string>(_disconnects.Keys);

            foreach (var booth in _booths)
            {
                foreach (var user in booth.Occupants.ToArray())
                {
                    if (disconnectedUsers.Contains(user))
                        continue;

                    var key = (Role.Occupant, booth.Number, user);
                    if (!_afk.TryGetValue(key, out var rec))
                    {
                        rec = new AfkState { Last = now, Warned = false };
                        _afk[key] = rec;
                    }

                    var idle = now - rec.Last;

                    if (idle >= OccupantKickThreshold)
                    {
                        // Captured before removal - a solo-occupant kick resets the booth (closes
                        // it, clears participants), which would otherwise silently suppress the
                        // channel announcement that those participants are owed.
                        var occupantsBefore = booth.Occupants.Count;
                        var wasOpenBefore = booth.IsOpen;
                        var participantsBefore = booth.Participants.Count;
                        var occIndex = OccupantIndex(booth, user);

                        events.Add(Pm(user, $"You were removed from Booth #{booth.Number} for being AFK (60 minutes)."));

                        booth.Leave(user);

                        if (occupantsBefore > 1)
                        {
                            foreach (var other in booth.Occupants)
                                events.Add(Pm(other, $"Occupant #{occIndex} was removed for being AFK."));
                        }

                        if (wasOpenBefore && participantsBefore > 0)
                        {
                            var chMsg = occupantsBefore > 1
                                ? $"Occupant #{occIndex} in Booth #{booth.Number} was removed for being AFK."
                                : $"An occupant in Booth #{booth.Number} was removed for being AFK.";

                            if (booth.Occupants.Count == 0)
                                chMsg += " The booth is now empty.";

                            events.Add(Channel(chMsg));
                        }

                        _afk.Remove(key);
                        continue;
                    }

                    if (idle >= OccupantWarnThreshold && !rec.Warned)
                    {
                        rec.Warned = true;
                        events.Add(Pm(user, AfkWarnPm));
                    }
                }

                foreach (var user in booth.Participants.ToArray())
                {
                    if (disconnectedUsers.Contains(user))
                        continue;

                    var key = (Role.Participant, booth.Number, user);
                    if (!_afk.TryGetValue(key, out var rec))
                    {
                        rec = new AfkState { Last = now, Warned = false };
                        _afk[key] = rec;
                    }

                    var idle = now - rec.Last;

                    if (idle >= ParticipantKickThreshold)
                    {
                        var index = ParticipantIndex(booth, user);

                        events.Add(Pm(user, $"You were removed from Booth #{booth.Number} for being AFK (60 minutes)."));

                        booth.LeaveHole(user);

                        events.Add(Channel($"{user} was removed from Booth #{booth.Number} for being AFK."));

                        foreach (var occ in booth.Occupants)
                            events.Add(Pm(occ, $"Participant #{index} was removed for being AFK."));

                        _afk.Remove(key);
                        continue;
                    }

                    if (idle >= ParticipantWarnThreshold && !rec.Warned)
                    {
                        rec.Warned = true;
                        events.Add(Pm(user, AfkWarnPm));
                    }
                }
            }

            SweepStaleAfk();
            return events;
        }
    }

    public IReadOnlyList<GHEvent> MarkDisconnected(string character)
    {
        lock (SyncRoot)
        {
            var (booth, role, occIndex) = FindBoothRole(character);
            if (booth is null || role is null)
                return [];

            var events = new List<GHEvent>();
            var isOpenBefore = booth.IsOpen;
            var participantsBefore = booth.Participants.Count;
            var occupantsBefore = booth.Occupants.Count;
            var resolvedOccIndex = occIndex ?? (role == Role.Occupant ? OccupantIndex(booth, character) : null);

            var deadline = DateTimeOffset.UtcNow + DisconnectGrace;

            if (role == Role.Participant)
            {
                events.Add(Channel($"{character} disconnected from Booth #{booth.Number}. They have 10 minutes to reconnect before being removed."));

                var participantIndex = ParticipantIndex(booth, character);
                foreach (var occ in booth.Occupants)
                    events.Add(Pm(occ, $"Participant #{participantIndex} disconnected. They have 10 minutes to reconnect before being removed."));

                ClearAfk(Role.Participant, booth.Number, character);
            }
            else
            {
                if (isOpenBefore && participantsBefore > 0)
                {
                    var chMsg = occupantsBefore > 1
                        ? $"Occupant #{resolvedOccIndex} in Booth #{booth.Number} disconnected. They have 10 minutes to reconnect before being removed."
                        : $"An occupant in Booth #{booth.Number} disconnected. They have 10 minutes to reconnect before being removed.";

                    events.Add(Channel(chMsg));
                }

                ClearAfk(Role.Occupant, booth.Number, character);
            }

            _disconnects[character] = new DisconnectRecord
            {
                Deadline = deadline,
                BoothNumber = booth.Number,
                Role = role.Value,
                OccupantIndex = role == Role.Occupant ? resolvedOccIndex : null
            };

            return events;
        }
    }

    public IReadOnlyList<GHEvent> MarkReconnected(string character)
    {
        lock (SyncRoot)
        {
            if (!_disconnects.Remove(character, out var record))
                return [];

            var booth = _booths.FirstOrDefault(b => b.Number == record.BoothNumber);
            if (booth is null)
                return [];

            var events = new List<GHEvent>();
            var now = DateTimeOffset.UtcNow;

            if (record.Role == Role.Participant)
            {
                events.Add(Channel($"{character} has returned to Booth #{booth.Number}."));

                var index = ParticipantIndex(booth, character);
                foreach (var occ in booth.Occupants)
                    events.Add(Pm(occ, $"Participant #{index} has returned."));

                _afk[(Role.Participant, booth.Number, character)] = new AfkState { Last = now, Warned = false };
            }
            else
            {
                var occupantCount = booth.Occupants.Count;
                var occIndex = OccupantIndex(booth, character);

                if (booth.IsOpen && booth.Participants.Count > 0)
                {
                    var chMsg = occupantCount > 1
                        ? $"Occupant #{occIndex} has returned to Booth #{booth.Number}."
                        : $"The occupant has returned to Booth #{booth.Number}.";

                    events.Add(Channel(chMsg));
                }

                if (occupantCount > 1)
                {
                    foreach (var other in booth.Occupants.Where(o => o != character))
                        events.Add(Pm(other, $"Occupant #{occIndex} has returned."));
                }

                events.Add(Pm(character, $"Welcome back. You're still in Booth #{booth.Number}."));

                _afk[(Role.Occupant, booth.Number, character)] = new AfkState { Last = now, Warned = false };
            }

            return events;
        }
    }

    public IReadOnlyList<GHEvent> ProcessDisconnectTimeouts()
    {
        lock (SyncRoot)
        {
            var events = new List<GHEvent>();

            if (_disconnects.Count == 0)
            {
                SweepStaleAfk();
                return events;
            }

            var now = DateTimeOffset.UtcNow;
            var expired = _disconnects.Where(kv => kv.Value.Deadline <= now).Select(kv => kv.Key).ToList();

            foreach (var character in expired)
            {
                var record = _disconnects[character];
                _disconnects.Remove(character);

                var booth = _booths.FirstOrDefault(b => b.Number == record.BoothNumber);
                if (booth is null)
                    continue;

                if (record.Role == Role.Participant)
                {
                    var partIndex = ParticipantIndex(booth, character);
                    booth.LeaveHole(character);

                    events.Add(Channel($"{character} was removed from Booth #{booth.Number} after disconnecting for 10 minutes."));

                    foreach (var occ in booth.Occupants)
                        events.Add(Pm(occ, $"Participant #{partIndex} has disconnected."));

                    ClearAfk(Role.Participant, booth.Number, character);
                    continue;
                }

                var occupantsBefore = booth.Occupants.Count;
                var participantsBefore = booth.Participants.Count;
                var wasOpenBefore = booth.IsOpen;
                var wasLastOccupant = occupantsBefore == 1;
                var occIndex = record.OccupantIndex ?? OccupantIndex(booth, character);

                string? chMsg = null;
                if (wasOpenBefore && participantsBefore > 0)
                {
                    chMsg = occupantsBefore > 1
                        ? $"Occupant #{occIndex} in Booth #{booth.Number} was removed after disconnecting for 10 minutes."
                        : $"The occupant in Booth #{booth.Number} was removed after disconnecting for 10 minutes.";

                    if (wasLastOccupant)
                        chMsg += " The booth is now empty.";
                }

                booth.Leave(character);

                if (occupantsBefore > 1)
                {
                    foreach (var other in booth.Occupants)
                        events.Add(Pm(other, $"Occupant #{occIndex} was removed after being disconnected for 10 minutes."));
                }

                if (chMsg is not null)
                    events.Add(Channel(chMsg));

                ClearAfk(Role.Occupant, booth.Number, character);
            }

            SweepStaleAfk();
            return events;
        }
    }

    private (Booth? Booth, Role? Role, int? OccupantIndex) FindBoothRole(string character)
    {
        var occupied = _booths.FirstOrDefault(b => b.Occupants.Contains(character));
        if (occupied is not null)
            return (occupied, Role.Occupant, OccupantIndex(occupied, character));

        var participated = _booths.FirstOrDefault(b => b.Participants.Contains(character));
        if (participated is not null)
            return (participated, Role.Participant, null);

        return (null, null, null);
    }

    private void ClearAfk(Role role, int boothNumber, string user) => _afk.Remove((role, boothNumber, user));

    // Drops AFK records for anyone no longer present in any booth (e.g. removed by a command
    // module directly, outside the AFK/disconnect machinery).
    private void SweepStaleAfk()
    {
        var present = new HashSet<(Role, int, string)>();
        foreach (var booth in _booths)
        {
            foreach (var u in booth.Occupants)
                present.Add((Role.Occupant, booth.Number, u));
            foreach (var u in booth.Participants)
                present.Add((Role.Participant, booth.Number, u));
        }

        foreach (var key in _afk.Keys.Where(k => !present.Contains(k)).ToList())
            _afk.Remove(key);
    }

    private static int OccupantIndex(Booth booth, string user)
    {
        var index = booth.Occupants.IndexOf(user);
        return index < 0 ? 1 : index + 1;
    }

    private static int ParticipantIndex(Booth booth, string user)
    {
        var index = booth.Participants.IndexOf(user);
        return index < 0 ? 1 : index + 1;
    }

    private GHEvent Pm(string recipient, string message) =>
        new() { Kind = GHEventKind.Pm, Recipient = recipient, Message = message };

    private GHEvent Channel(string message) =>
        new() { Kind = GHEventKind.Channel, Channel = _channelId, Message = message };
}
