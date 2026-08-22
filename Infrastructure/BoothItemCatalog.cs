using Application.Interfaces;
using Domain;

namespace Infrastructure;

public sealed class BoothItemCatalog : IBoothItemCatalog
{
    private const string FileName = "booth-items";

    private readonly IFileOperations _fileOperations;

    public BoothItemCatalog(IFileOperations fileOperations)
    {
        _fileOperations = fileOperations;
    }

    // Read fresh every call, same convention as gnuuy-responses.json and moderators.json - edits
    // to Data/booth-items.json (new items, price changes) take effect immediately.
    public async Task<IReadOnlyList<BoothItem>> GetAllAsync()
    {
        try
        {
            return await _fileOperations.ReadFromJsonFileAsync<List<BoothItem>>(FileName);
        }
        catch (FileNotFoundException)
        {
            return [];
        }
    }

    public async Task<BoothItem?> FindByIdAsync(string itemId)
    {
        var items = await GetAllAsync();
        return items.FirstOrDefault(i => string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase));
    }
}
