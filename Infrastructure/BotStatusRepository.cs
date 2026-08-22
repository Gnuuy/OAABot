using Application.Interfaces;

namespace Infrastructure;

public sealed class BotStatusRepository : IBotStatusRepository
{
    private const string FileName = "bot-status";

    private readonly IFileOperations _fileOperations;

    public BotStatusRepository(IFileOperations fileOperations)
    {
        _fileOperations = fileOperations;
    }

    public async Task<string?> GetStatusMessageAsync()
    {
        try
        {
            return await _fileOperations.ReadFromJsonFileAsync<string>(FileName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public Task SaveStatusMessageAsync(string statusMessage) =>
        _fileOperations.WriteToJsonFileAsync(FileName, statusMessage);
}
