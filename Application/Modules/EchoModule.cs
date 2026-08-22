using Application.Interfaces;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Modules;

public class EchoModule(IOutboundMessageQueue outboundQueue, ILogger<EchoModule> logger) : ModuleBase(outboundQueue)
{
    public override bool HasHelpfulInfo => true;
    public override string Command => "echo";
    public override string CommandDescription => "This command will echo whatever you type to it!";

    public override async Task HandleAsync(InboundMessage message)
    {
        logger.LogInformation("[Echo Module]: I got a message from {Character} that said {Message}", message.CharacterName, message.Message);
        
        if (message.Type == MessageType.Private)
        {
            if (message.CharacterName is not null)
                await SendPrivateAsync(message.CharacterName, message.Message);
        }

        if (message.Type == MessageType.Public)
        {
            if (message.ChannelId is not null)
                await SendChannelAsync(message.ChannelId, message.Message);
        }
    }
}