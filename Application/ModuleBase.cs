using System.Reflection.PortableExecutable;
using Application.Interfaces;
using Domain;
using Domain.Enums;

public abstract class ModuleBase
{
    private readonly IOutboundMessageQueue _outboundQueue;

    protected ModuleBase(IOutboundMessageQueue outboundQueue)
    {
        _outboundQueue = outboundQueue;
    }

    public virtual bool HasHelpfulInfo => false;
    public abstract string Command { get; }
    public virtual string CommandDescription => "";

    // Hides this command from !commands for anyone the CommandListModule doesn't confirm is a
    // moderator - the module itself is still responsible for rejecting non-moderators who invoke
    // it directly, this only controls whether it's advertised.
    public virtual bool ModeratorOnly => false;

    public abstract Task HandleAsync(InboundMessage message);

    public virtual bool CanHandle(InboundMessage message)
    {
        if (string.IsNullOrWhiteSpace(Command))
            return false;

        return string.Equals(
            message.Command,
            Command,
            StringComparison.OrdinalIgnoreCase);
    }
    
    protected ValueTask SendAsync(OutboundMessage message)
    {
        return _outboundQueue.EnqueueAsync(message);
    }

    protected ValueTask SendPrivateAsync(string character, string message)
    {
        return SendAsync(new OutboundMessage
        {
            Type = MessageType.Private,
            Character = character,
            Message = message
        });
    }

    protected ValueTask SendChannelAsync(string channelId, string message)
    {
        return SendAsync(new OutboundMessage
        {
            Type = MessageType.Public,
            ChannelId = channelId,
            Message = message
        });
    }

    protected ValueTask SendStatusAsync(string statusMessage)
    {
        return SendAsync(new OutboundMessage
        {
            Type = MessageType.Status,
            Message = statusMessage
        });
    }
}