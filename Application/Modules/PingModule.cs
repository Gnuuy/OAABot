using Application.Interfaces;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Modules;

public class PingModule(IOutboundMessageQueue outboundQueue, ILogger<PingModule> logger) : ModuleBase(outboundQueue)
{
    public override bool HasHelpfulInfo => true;
    public override string Command => "ping";
    public override string CommandDescription => "This command will pong if you ping!";
    
    public override async Task HandleAsync(InboundMessage message)
    {
        if (message.Type == MessageType.Public)
            if (message.ChannelId is not null)
                await SendChannelAsync(message.ChannelId, "Pong!");

        if (message.Type == MessageType.Private)
            if (message.CharacterName is not null)
                await SendPrivateAsync(message.CharacterName, "Pong!");
    }
}