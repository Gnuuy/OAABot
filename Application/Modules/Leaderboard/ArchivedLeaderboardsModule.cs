using System.Text;
using Application.Interfaces;
using Domain;

namespace Application.Modules.Leaderboard;

public class ArchivedLeaderboardsModule : ModuleBase
{
    // The reference bot only listed 2021-2023 (whatever existed when it was written); Data/leaderboards
    // now also has 2024 and 2025 archived, so those are included too rather than staying stuck at 2023.
    private static readonly int[] ArchivedYears = [2025, 2024, 2023, 2022, 2021];

    private readonly IFileOperations _fileOperations;

    public override bool HasHelpfulInfo => true;
    public override string Command => "archived_leaderboards";
    public override string CommandDescription => "Sends you the archived leaderboards from past years in a PM.";

    public ArchivedLeaderboardsModule(IOutboundMessageQueue outboundQueue, IFileOperations fileOperations)
        : base(outboundQueue)
    {
        _fileOperations = fileOperations;
    }

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName;
        if (character is null)
            return;

        var sb = new StringBuilder();
        sb.Append($"Hiya {character}, here's the archived leaderboards, just for you:\n\n");

        foreach (var year in ArchivedYears)
        {
            var board = await LeaderboardFormatter.LoadAsync(_fileOperations, $"leaderboards/{year}");
            sb.Append($"[b][b]{year}'s Leaderboard:[/b][/b]\n{LeaderboardFormatter.Format(board)}\n\n");
        }

        await SendPrivateAsync(character, sb.ToString().TrimEnd());
    }
}
