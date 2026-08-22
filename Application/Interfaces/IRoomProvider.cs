namespace Application.Interfaces;

// The single room the bot operates in, exposed to Application-layer modules that need to
// announce there without coupling to gloryhole-specific machinery (see IGloryholeManager.ChannelId
// for that subsystem's own copy of the same value).
public interface IRoomProvider
{
    string ChannelId { get; }
}
