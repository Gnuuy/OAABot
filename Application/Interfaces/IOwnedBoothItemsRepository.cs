namespace Application.Interfaces;

public interface IOwnedBoothItemsRepository
{
    Task<IReadOnlySet<string>> GetOwnedItemIdsAsync(string characterName);
    Task AddOwnedItemAsync(string characterName, string itemId);
}
