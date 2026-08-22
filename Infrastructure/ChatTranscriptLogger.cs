using System.Collections.Concurrent;
using Application.Interfaces;

namespace Infrastructure;

public sealed class ChatTranscriptLogger : IChatTranscriptLogger
{
    private readonly IDataPathProvider _pathProvider;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new(StringComparer.OrdinalIgnoreCase);

    public ChatTranscriptLogger(IDataPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task LogAsync(string characterName, string message)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            throw new ArgumentException(
                "The character name cannot be empty or contain only whitespace.", nameof(characterName));
        }

        var logDirectory = Path.Combine(_pathProvider.DataRoot, "logs");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, $"{SanitizeFileName(characterName)}.log");
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {characterName}: {message}{Environment.NewLine}";

        var fileLock = _fileLocks.GetOrAdd(logPath, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(logPath, line);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalidChars.Contains(c) ? '_' : c));
    }
}
