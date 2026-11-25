using Application.Interfaces;

namespace Infrastructure;

// There's no !unban command - lifting a ban is a manual edit to Data/banned.json, same
// convention as moderators.json.
public sealed class BannedUserRepository : IBannedUserRepository
{
    private const string FileName = "banned";

    private readonly IFileOperations _fileOperations;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public BannedUserRepository(IFileOperations fileOperations)
    {
        _fileOperations = fileOperations;
    }

    public async Task<bool> IsBannedAsync(string characterName)
    {
        var banned = await LoadAsync();
        return banned.Any(b => string.Equals(b, characterName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task BanAsync(string characterName)
    {
        await _gate.WaitAsync();
        try
        {
            var banned = await LoadAsync();
            if (!banned.Any(b => string.Equals(b, characterName, StringComparison.OrdinalIgnoreCase)))
                banned.Add(characterName);

            await _fileOperations.WriteToJsonFileAsync(FileName, banned);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<string>> LoadAsync()
    {
        try
        {
            var banned = await _fileOperations.ReadFromJsonFileAsync<string[]>(FileName);
            return banned.ToList();
        }
        catch (FileNotFoundException)
        {
            return [];
        }
    }
}
