namespace Application.Interfaces;

public interface IModeratorRepository
{
    Task<bool> IsModeratorAsync(string characterName);
}
