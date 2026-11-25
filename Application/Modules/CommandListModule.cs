using Application.Interfaces;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules;

public class CommandListModule(
    IOutboundMessageQueue outboundQueue,
    IServiceProvider serviceProvider,
    IModeratorRepository moderatorRepository)
    : ModuleBase(outboundQueue)
{
    public override bool HasHelpfulInfo => true;
    public override string Command => "commands";
    public override string CommandDescription => "Lists the available commands.";

    public override async Task HandleAsync(InboundMessage message)
    {
        var isModerator = message.CharacterName is not null
            && await moderatorRepository.IsModeratorAsync(message.CharacterName);

        var lines = serviceProvider.GetServices<ModuleBase>()
            .Where(module => module.HasHelpfulInfo)
            .Where(module => !module.ModeratorOnly || isModerator)
            .OrderBy(module => module.Command, StringComparer.OrdinalIgnoreCase)
            .Select(module => $"!{module.Command} - {module.CommandDescription}");

        string reply = "Available commands:\n" + string.Join("\n", lines);

        if (message.Type == MessageType.Private)
        {
            if (message.CharacterName is not null)
                await SendPrivateAsync(message.CharacterName, reply);
        }
        else if (message.ChannelId is not null)
        {
            await SendChannelAsync(message.ChannelId, reply);
        }
    }
}
