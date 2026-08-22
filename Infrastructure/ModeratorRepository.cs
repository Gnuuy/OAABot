using Application.Interfaces;

namespace Infrastructure;

public sealed class ModeratorRepository : IModeratorRepository
{
    private const string FileName = "moderators";

    private readonly IFileOperations _fileOperations;

    public ModeratorRepository(IFileOperations fileOperations)
    {
        _fileOperations = fileOperations;
    }

    // Read fresh every call rather than cached, so editing Data/moderators.json takes effect
    // immediately without a restart - same convention as gnuuy-responses.json.
    public async Task<bool> IsModeratorAsync(string characterName)
    {
        string[] moderators;
        try
        {
            moderators = await _fileOperations.ReadFromJsonFileAsync<string[]>(FileName);
        }
        catch (FileNotFoundException)
        {
            return false;
        }

        return moderators.Any(m => string.Equals(m, characterName, StringComparison.OrdinalIgnoreCase));
    }
}
