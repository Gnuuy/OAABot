using System.Net;
using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules;

public class TalkModule : ModuleBase
{
    private readonly IModeratorRepository _moderatorRepository;
    private readonly IRoomProvider _roomProvider;

    public override bool HasHelpfulInfo => true;
    public override string Command => "talk";
    public override string CommandDescription => "(Moderators only) Makes the bot say something in the room.";

    public TalkModule(IOutboundMessageQueue outboundQueue, IModeratorRepository moderatorRepository, IRoomProvider roomProvider)
        : base(outboundQueue)
    {
        _moderatorRepository = moderatorRepository;
        _roomProvider = roomProvider;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;

        if (!await _moderatorRepository.IsModeratorAsync(character))
        {
            await SendPrivateAsync(character, "You do not have the privilege to use this command.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Message))
            return;

        await SendChannelAsync(_roomProvider.ChannelId, WebUtility.HtmlDecode(message.Message));
    }
}
