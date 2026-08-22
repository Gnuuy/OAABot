using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Leaderboard;

public class TokensModule : ModuleBase
{
    private readonly IFileOperations _fileOperations;

    public override bool HasHelpfulInfo => true;
    public override string Command => "tokens";
    public override string CommandDescription => "Check how many tokens you've got.";

    public TokensModule(IOutboundMessageQueue outboundQueue, IFileOperations fileOperations)
        : base(outboundQueue)
    {
        _fileOperations = fileOperations;
    }

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        var board = await LeaderboardFormatter.LoadAsync(_fileOperations, LeaderboardFormatter.CurrentFile);

        var reply = board.TryGetValue(character, out var count)
            ? $"[user]{character}[/user], You've got {count} {(count == 1 ? "token" : "tokens")}."
            : "Sorry friend, I'm not seeing any tokens recorded for your name in my database.";

        if (message.Type == MessageType.Public && message.ChannelId is not null)
            await SendChannelAsync(message.ChannelId, reply);
        else if (message.Type == MessageType.Private)
            await SendPrivateAsync(character, reply);
    }
}
