using Application.Interfaces;

namespace Infrastructure;

public sealed class RoomProvider : IRoomProvider
{
    public string ChannelId { get; }

    public RoomProvider(string channelId)
    {
        ChannelId = channelId;
    }
}
