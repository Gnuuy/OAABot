using System.Collections.Concurrent;
using Application.Interfaces;

namespace Infrastructure;

public sealed class CharacterPresenceTracker : ICharacterPresenceTracker
{
    private readonly ConcurrentDictionary<string, byte> _onlineCharacters = new(StringComparer.OrdinalIgnoreCase);

    public bool IsOnline(string characterName) => _onlineCharacters.ContainsKey(characterName);

    public void MarkOnline(string characterName) => _onlineCharacters[characterName] = 0;

    public void MarkOffline(string characterName) => _onlineCharacters.TryRemove(characterName, out _);

    public void AddToInitialList(IEnumerable<string> characterNames)
    {
        foreach (var name in characterNames)
        {
            _onlineCharacters[name] = 0;
        }
    }

    public void Clear() => _onlineCharacters.Clear();
}
