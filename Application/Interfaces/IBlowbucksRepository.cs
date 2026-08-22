namespace Application.Interfaces;

public interface IBlowbucksRepository
{
    Task<int> GetBalanceAsync(string characterName);

    // Adds (or subtracts, with a negative amount) and returns the resulting balance.
    Task<int> AddAsync(string characterName, int amount);
}
