using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Leaderboard;

public class CompleteLeaderboardModule : ModuleBase
{
    private readonly IFileOperations _fileOperations;

    public override bool HasHelpfulInfo => true;
    public override string Command => "complete_leaderboard";
    public override string CommandDescription => "Sends you the entire current leaderboard in a PM.";

    public CompleteLeaderboardModule(IOutboundMessageQueue outboundQueue, IFileOperations fileOperations)
        : base(outboundQueue)
    {
        _fileOperations = fileOperations;
    }

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        if (message.Type == MessageType.Public && message.ChannelId is not null)
            await SendChannelAsync(message.ChannelId, "I'll send you the whole leaderboard privately. Check your PMs!");

        var board = await LeaderboardFormatter.LoadAsync(_fileOperations, LeaderboardFormatter.CurrentFile);
        var full = LeaderboardFormatter.Format(board);

        await SendPrivateAsync(character, $"Hiya {character}, here's the leaderboard, just for you:\n\n{full}");
    }
}
