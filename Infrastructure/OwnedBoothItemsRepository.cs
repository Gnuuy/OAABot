using Application.Interfaces;

namespace Infrastructure;

public sealed class OwnedBoothItemsRepository : IOwnedBoothItemsRepository
{
    private const string FileName = "owned-booth-items";

    private readonly IFileOperations _fileOperations;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public OwnedBoothItemsRepository(IFileOperations fileOperations)
    {
        _fileOperations = fileOperations;
    }

    public async Task<IReadOnlySet<string>> GetOwnedItemIdsAsync(string characterName)
    {
        var all = await LoadAsync();
        return all.TryGetValue(characterName, out var items)
            ? new HashSet<string>(items, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task AddOwnedItemAsync(string characterName, string itemId)
    {
        await _gate.WaitAsync();
        try
        {
            var all = await LoadAsync();
            var key = characterName.ToLowerInvariant();

            if (!all.TryGetValue(key, out var items))
            {
                items = [];
                all[key] = items;
            }

            if (!items.Contains(itemId, StringComparer.OrdinalIgnoreCase))
                items.Add(itemId);

            await _fileOperations.WriteToJsonFileAsync(FileName, all);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, List<string>>> LoadAsync()
    {
        try
        {
            var data = await _fileOperations.ReadFromJsonFileAsync<Dictionary<string, List<string>>>(FileName);
            return new Dictionary<string, List<string>>(data, StringComparer.OrdinalIgnoreCase);
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
