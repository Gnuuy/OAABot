using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class KickModule : ModuleBase
{
    private readonly IOutboundMessageQueue _outboundQueue;
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IModeratorRepository _moderatorRepository;

    public override bool HasHelpfulInfo => true;
    public override bool ModeratorOnly => true;
    public override string Command => "kick";
    public override string CommandDescription => "(Moderators only) Remove a character from whichever booth or gloryhole they're currently in. Usage: !kick <Character Name>";

    public KickModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IModeratorRepository moderatorRepository)
        : base(outboundQueue)
    {
        _outboundQueue = outboundQueue;
        _gloryholeManager = gloryholeManager;
        _moderatorRepository = moderatorRepository;
    }

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        if (!await _moderatorRepository.IsModeratorAsync(character))
        {
            await ReplyAsync(message, "You don't have permission to use this command.");
            return;
        }

        var target = message.Message.Trim();
        if (target.Length == 0)
        {
            await ReplyAsync(message, "Usage: !kick <Character Name>");
            return;
        }

        var events = _gloryholeManager.RemoveCharacter(target, "by a moderator");

        if (events.Count == 0)
        {
            await ReplyAsync(message, $"{target} isn't currently in any booth or gloryhole.");
            return;
        }

        await GloryholeEventPublisher.PublishAsync(_outboundQueue, events);
        await ReplyAsync(message, $"{target} has been kicked.");
    }

    private async Task ReplyAsync(InboundMessage message, string text)
    {
        if (message.Type == MessageType.Private && message.CharacterName is not null)
            await SendPrivateAsync(message.CharacterName, text);
        else if (message.ChannelId is not null)
            await SendChannelAsync(message.ChannelId, text);
    }
}
