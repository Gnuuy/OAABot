using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Leaderboard;

public class BlowbucksModule : ModuleBase
{
    private readonly IBlowbucksRepository _blowbucksRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "blowbucks";
    public override string CommandDescription => "Check how many Blowbucks you've got.";

    public BlowbucksModule(IOutboundMessageQueue outboundQueue, IBlowbucksRepository blowbucksRepository)
        : base(outboundQueue)
    {
        _blowbucksRepository = blowbucksRepository;
    }

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        var balance = await _blowbucksRepository.GetBalanceAsync(character);
        var reply = balance > 0
            ? $"[user]{character}[/user], You've got {balance} {(balance == 1 ? "Blowbuck" : "Blowbucks")}."
            : "Sorry friend, I'm not seeing any Blowbucks recorded for your name in my database.";

        if (message.Type == MessageType.Public && message.ChannelId is not null)
            await SendChannelAsync(message.ChannelId, reply);
        else if (message.Type == MessageType.Private)
            await SendPrivateAsync(character, reply);
    }
}
