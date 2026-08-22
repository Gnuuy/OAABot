using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Leaderboard;

public class GiveTokenModule : ModuleBase
{
    private readonly IModeratorRepository _moderatorRepository;
    private readonly IFileOperations _fileOperations;
    private readonly IBlowbucksRepository _blowbucksRepository;

    public override bool HasHelpfulInfo => true;
    public override string Command => "give_token";
    public override string CommandDescription => "(Moderators only) Give a character tokens (and an equal amount of Blowbucks). Usage: !give_token <Character Name> <amount>";

    public GiveTokenModule(
        IOutboundMessageQueue outboundQueue,
        IModeratorRepository moderatorRepository,
        IFileOperations fileOperations,
        IBlowbucksRepository blowbucksRepository)
        : base(outboundQueue)
    {
        _moderatorRepository = moderatorRepository;
        _fileOperations = fileOperations;
        _blowbucksRepository = blowbucksRepository;
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

        // Name can be multiple words (e.g. "Red Shadowscale") - the amount is always the last
        // whitespace-separated token, everything before it is the name.
        var parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await ReplyAsync(message, "Incorrect command syntax.\nUsage: !give_token <Character Name> <amount>");
            return;
        }

        var targetName = string.Join(" ", parts[..^1]);

        if (!int.TryParse(parts[^1], out var amount))
        {
            await ReplyAsync(message, "The amount needs to be a number.");
            return;
        }

        if (string.Equals(targetName, "moo", StringComparison.OrdinalIgnoreCase))
        {
            await ReplyAsync(message, "You can't give moo tokens, silly!");
            return;
        }

        var board = await LeaderboardFormatter.LoadAsync(_fileOperations, LeaderboardFormatter.CurrentFile);

        var key = targetName.ToLowerInvariant();
        board[key] = board.GetValueOrDefault(key) + amount;

        await _fileOperations.WriteToJsonFileAsync(LeaderboardFormatter.CurrentFile, board);

        // Every token grant mirrors into Blowbucks - the future booth purchase/upgrade currency.
        var blowbucksBalance = await _blowbucksRepository.AddAsync(targetName, amount);

        await ReplyAsync(message, $"{targetName} now has {board[key]} tokens and {blowbucksBalance} Blowbucks.");
    }

    private async Task ReplyAsync(InboundMessage message, string text)
    {
        if (message.Type == MessageType.Private && message.CharacterName is not null)
            await SendPrivateAsync(message.CharacterName, text);
        else if (message.ChannelId is not null)
            await SendChannelAsync(message.ChannelId, text);
    }
}
