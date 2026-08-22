using Domain;

namespace Application.Interfaces;

public interface IGloryholeProfileRepository
{
    Task<GloryholeProfile?> GetProfileAsync(string characterName);
    Task SaveProfileAsync(string characterName, GloryholeProfile profile);
}
