using Application.Interfaces;

namespace Infrastructure;

public sealed class BlowbucksRepository : IBlowbucksRepository
{
    private const string FileName = "blowbucks";

    private readonly IFileOperations _fileOperations;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public BlowbucksRepository(IFileOperations fileOperations)
    {
        _fileOperations = fileOperations;
    }

    public async Task<int> GetBalanceAsync(string characterName)
    {
        var board = await LoadAsync();
        return board.GetValueOrDefault(characterName);
    }

    public async Task<int> AddAsync(string characterName, int amount)
    {
        await _gate.WaitAsync();
        try
        {
            var board = await LoadAsync();
            var key = characterName.ToLowerInvariant();
            board[key] = board.GetValueOrDefault(key) + amount;

            await _fileOperations.WriteToJsonFileAsync(FileName, board);
            return board[key];
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, int>> LoadAsync()
    {
        try
        {
            var board = await _fileOperations.ReadFromJsonFileAsync<Dictionary<string, int>>(FileName);
            return new Dictionary<string, int>(board, StringComparer.OrdinalIgnoreCase);
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
