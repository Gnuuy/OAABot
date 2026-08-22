using Application.Interfaces;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Modules;

public class GnuuyModule : ModuleBase
{
    private const string TrainIcon = "[eicon]elrout[/eicon]";
    private static readonly TimeSpan TrainWindow = TimeSpan.FromSeconds(30);

    private readonly IFileOperations _fileOperations;
    private readonly ILogger<GnuuyModule> _logger;

    // A gnuuy-train is a single, bot-wide event tied to whichever channel started it - the bot
    // only ever auto-joins one room today, so there's no need to track one per channel.
    private readonly object _trainLock = new();
    private Timer? _trainTimer;
    private HashSet<string>? _trainParticipants;
    private string? _trainChannelId;

    private string? _lastMessage;

    public override bool HasHelpfulInfo => true;
    public override string Command => "gnuuy";
    public override string CommandDescription => "Get a random message from the gnuuy!";

    public GnuuyModule(IOutboundMessageQueue outboundQueue, IFileOperations fileOperations, ILogger<GnuuyModule> logger)
        : base(outboundQueue)
    {
        _fileOperations = fileOperations;
        _logger = logger;
    }

    public override bool CanHandle(InboundMessage message)
    {
        if (base.CanHandle(message))
            return true;

        // Passive listener for gnuuy-train participation in channel chatter, independent of
        // whether this particular message is the !gnuuy command itself.
        return message.Type == MessageType.Public
            && !string.IsNullOrEmpty(message.Message)
            && message.Message.Contains(TrainIcon, StringComparison.OrdinalIgnoreCase);
    }

    public override async Task HandleAsync(InboundMessage message)
    {
        if (string.Equals(message.Command, Command, StringComparison.OrdinalIgnoreCase))
        {
            await HandleGnuuyCommandAsync(message);
            return;
        }

        HandleTrainParticipation(message);
    }

    private async Task HandleGnuuyCommandAsync(InboundMessage message)
    {
        var responses = await LoadResponsesAsync();
        if (responses.Length == 0)
        {
            _logger.LogWarning("[Gnuuy]: No responses are configured in gnuuy-responses.json.");
            return;
        }

        var candidates = Array.FindAll(responses, r => r != _lastMessage);
        if (candidates.Length == 0)
            candidates = responses;

        var selected = candidates[Random.Shared.Next(candidates.Length)];
        _lastMessage = selected;

        if (message.CharacterName is not null)
            selected = selected.Replace("%s", message.CharacterName);

        if (message.Type == MessageType.Private)
        {
            if (message.CharacterName is not null)
                await SendPrivateAsync(message.CharacterName, selected);

            return;
        }

        if (message.ChannelId is null)
            return;

        await SendChannelAsync(message.ChannelId, selected);

        // Any gnuuy response that happens to feature the "train" icon kicks one off - tied to the
        // icon itself rather than one hardcoded response string, so editing gnuuy-responses.json
        // later can't silently break this.
        if (selected.Contains(TrainIcon, StringComparison.OrdinalIgnoreCase))
        {
            StartOrExtendTrain(message.ChannelId);
        }
    }

    private void StartOrExtendTrain(string channelId)
    {
        lock (_trainLock)
        {
            if (_trainParticipants is null)
            {
                _trainParticipants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _trainChannelId = channelId;
                _logger.LogInformation("[Gnuuy]: A gnuuy-train has started in channel {ChannelId}.", channelId);
            }

            _trainTimer ??= new Timer(_ => OnTrainEnded(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _trainTimer.Change(TrainWindow, Timeout.InfiniteTimeSpan);
        }
    }

    private void HandleTrainParticipation(InboundMessage message)
    {
        lock (_trainLock)
        {
            if (_trainParticipants is null || message.ChannelId != _trainChannelId)
                return;

            if (message.CharacterName is not null)
                _trainParticipants.Add(message.CharacterName);

            // Every participation resets the window, not just new participants.
            _trainTimer?.Change(TrainWindow, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnTrainEnded()
    {
        HashSet<string> participants;
        string? channelId;

        lock (_trainLock)
        {
            if (_trainParticipants is null)
                return;

            participants = _trainParticipants;
            channelId = _trainChannelId;

            _trainParticipants = null;
            _trainChannelId = null;
            _trainTimer?.Dispose();
            _trainTimer = null;
        }

        _ = AnnounceTrainResultAsync(channelId!, participants.Count);
    }

    private async Task AnnounceTrainResultAsync(string channelId, int participantCount)
    {
        try
        {
            var previous = await LoadLastTrainAsync();

            var summary =
                $"[b]All aboard![/b] The gnuuy-train just wrapped up with {participantCount} {PersonWord(participantCount)} joining in.";

            summary += previous is null
                ? " No previous gnuuy-train on record - this one's the first!"
                : $" The last gnuuy-train had {previous.ParticipantCount} {PersonWord(previous.ParticipantCount)}.";

            await SendChannelAsync(channelId, summary);

            await _fileOperations.WriteToJsonFileAsync("gnuuy-train", new GnuuyTrainRecord
            {
                ParticipantCount = participantCount,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Gnuuy]: Failed to announce gnuuy-train results.");
        }
    }

    private static string PersonWord(int count) => count == 1 ? "person" : "people";

    private async Task<string[]> LoadResponsesAsync()
    {
        try
        {
            return await _fileOperations.ReadFromJsonFileAsync<string[]>("gnuuy-responses");
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("[Gnuuy]: gnuuy-responses.json was not found.");
            return [];
        }
    }

    private async Task<GnuuyTrainRecord?> LoadLastTrainAsync()
    {
        try
        {
            return await _fileOperations.ReadFromJsonFileAsync<GnuuyTrainRecord>("gnuuy-train");
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
