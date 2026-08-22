namespace Application.Interfaces;

public interface IChatTranscriptLogger
{
    Task LogAsync(string characterName, string message);
}
