using Domain;

namespace Application.Interfaces;

public interface IBoothItemCatalog
{
    Task<IReadOnlyList<BoothItem>> GetAllAsync();
    Task<BoothItem?> FindByIdAsync(string itemId);
}
