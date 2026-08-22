namespace Application;

public static class CommandParser
{
    public static ParsedCommand Parse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ParsedCommand(false, null, string.Empty);
        }

        rawText = rawText.Trim();

        if (!rawText.StartsWith('!'))
        {
            return new ParsedCommand(false, null, rawText);
        }

        var withoutPrefix = rawText[1..].Trim();

        if (string.IsNullOrWhiteSpace(withoutPrefix))
        {
            return new ParsedCommand(true, string.Empty, string.Empty);
        }

        // Any whitespace ends the command word, not just space/tab - F-Chat messages can be
        // multi-line (shift+enter), so a bare "!command\nrest of the text" is valid input too.
        var splitIndex = withoutPrefix.IndexOfAny([' ', '\t', '\r', '\n']);

        if (splitIndex < 0)
        {
            return new ParsedCommand(
                true,
                withoutPrefix.ToLowerInvariant(),
                string.Empty);
        }

        var command = withoutPrefix[..splitIndex].Trim().ToLowerInvariant();
        var message = withoutPrefix[(splitIndex + 1)..].Trim();

        return new ParsedCommand(true, command, message);
    }
}

public sealed record ParsedCommand(
    bool IsCommand,
    string? Command,
    string Message);