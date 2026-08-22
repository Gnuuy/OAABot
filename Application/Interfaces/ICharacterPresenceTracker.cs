namespace Application.Interfaces;

public interface ICharacterPresenceTracker
{
    bool IsOnline(string characterName);
    void MarkOnline(string characterName);
    void MarkOffline(string characterName);
    void AddToInitialList(IEnumerable<string> characterNames);
    void Clear();
}
