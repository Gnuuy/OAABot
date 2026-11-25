using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class BanModule : ModuleBase
{
    private readonly IOutboundMessageQueue _outboundQueue;
    private readonly IGloryholeManager _gloryholeManager;
    private readonly IModeratorRepository _moderatorRepository;
    private readonly IBannedUserRepository _bannedUserRepository;

    public override bool HasHelpfulInfo => true;
    public override bool ModeratorOnly => true;
    public override string Command => "ban";
    public override string CommandDescription => "(Moderators only) Kick a character and bar them from using booths/gloryholes. Usage: !ban <Character Name>";

    public BanModule(
        IOutboundMessageQueue outboundQueue,
        IGloryholeManager gloryholeManager,
        IModeratorRepository moderatorRepository,
        IBannedUserRepository bannedUserRepository)
        : base(outboundQueue)
    {
        _outboundQueue = outboundQueue;
        _gloryholeManager = gloryholeManager;
        _moderatorRepository = moderatorRepository;
        _bannedUserRepository = bannedUserRepository;
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
            await ReplyAsync(message, "Usage: !ban <Character Name>");
            return;
        }

        // Ban regardless of whether they're currently present - a moderator may want to
        // preemptively ban someone who isn't in a booth right now.
        var events = _gloryholeManager.RemoveCharacter(target, "by a moderator and banned");
        await GloryholeEventPublisher.PublishAsync(_outboundQueue, events);

        await _bannedUserRepository.BanAsync(target);

        await ReplyAsync(message, $"{target} has been kicked and banned from using booths and gloryholes.");
    }

    private async Task ReplyAsync(InboundMessage message, string text)
    {
        if (message.Type == MessageType.Private && message.CharacterName is not null)
            await SendPrivateAsync(message.CharacterName, text);
        else if (message.ChannelId is not null)
            await SendChannelAsync(message.ChannelId, text);
    }
}
