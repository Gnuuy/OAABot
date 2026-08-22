using Application.Interfaces;
using Domain;

namespace Infrastructure;

public sealed class GloryholeProfileRepository : IGloryholeProfileRepository
{
    private const string FileName = "gloryhole/profiles";

    private readonly IFileOperations _fileOperations;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public GloryholeProfileRepository(IFileOperations fileOperations)
    {
        _fileOperations = fileOperations;
    }

    public async Task<GloryholeProfile?> GetProfileAsync(string characterName)
    {
        ValidateCharacterName(characterName);

        var profiles = await LoadAllAsync();
        return profiles.TryGetValue(characterName, out var profile) ? profile : null;
    }

    public async Task SaveProfileAsync(string characterName, GloryholeProfile profile)
    {
        ValidateCharacterName(characterName);

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        await _gate.WaitAsync();
        try
        {
            var profiles = await LoadAllAsync();
            profiles[characterName] = profile;
            await _fileOperations.WriteToJsonFileAsync(FileName, profiles);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, GloryholeProfile>> LoadAllAsync()
    {
        Dictionary<string, GloryholeProfile> profiles;

        try
        {
            profiles = await _fileOperations.ReadFromJsonFileAsync<Dictionary<string, GloryholeProfile>>(FileName);
        }
        catch (FileNotFoundException)
        {
            profiles = new Dictionary<string, GloryholeProfile>();
        }

        return new Dictionary<string, GloryholeProfile>(profiles, StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateCharacterName(string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            throw new ArgumentException(
                "The character name cannot be empty or contain only whitespace.", nameof(characterName));
        }
    }
}
