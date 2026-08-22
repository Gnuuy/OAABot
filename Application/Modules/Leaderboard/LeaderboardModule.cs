using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Leaderboard;

public class LeaderboardModule : ModuleBase
{
    private readonly IFileOperations _fileOperations;

    public override bool HasHelpfulInfo => true;
    public override string Command => "leaderboard";
    public override string CommandDescription => "Shows the top 3 token holders in channel, or the full leaderboard in a PM.";

    public LeaderboardModule(IOutboundMessageQueue outboundQueue, IFileOperations fileOperations)
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

        if (message.Type == MessageType.Public && message.ChannelId is not null)
        {
            var top3 = LeaderboardFormatter.Format(board, take: 3);
            await SendChannelAsync(message.ChannelId,
                $"Hey there {character}! The top 3 of the leaderboard are below. Say \"!complete_leaderboard\" to me privately if you'd like the entire thing in your PMs!\n{top3}");
        }
        else if (message.Type == MessageType.Private)
        {
            var full = LeaderboardFormatter.Format(board);
            await SendPrivateAsync(character, $"Hiya {character}, here's the leaderboard, just for you:\n\n{full}");
        }
    }
}
