using System.Text;
using Application.Interfaces;

namespace Application.Modules.Leaderboard;

internal static class LeaderboardFormatter
{
    public const string CurrentFile = "leaderboards/leaderboard";

    public static async Task<Dictionary<string, int>> LoadAsync(IFileOperations fileOperations, string fileName)
    {
        try
        {
            var board = await fileOperations.ReadFromJsonFileAsync<Dictionary<string, int>>(fileName);
            return new Dictionary<string, int>(board, StringComparer.OrdinalIgnoreCase);
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string Format(Dictionary<string, int> board, int? take = null)
    {
        IEnumerable<KeyValuePair<string, int>> ordered = board.OrderByDescending(kv => kv.Value);
        if (take is not null)
            ordered = ordered.Take(take.Value);

        var sb = new StringBuilder();
        foreach (var (name, count) in ordered)
            sb.Append($"[user]{name}[/user], with {count} tokens.\n");

        return sb.ToString();
    }
}
