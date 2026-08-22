namespace Application.Interfaces;

public interface IBotStatusRepository
{
    Task<string?> GetStatusMessageAsync();
    Task SaveStatusMessageAsync(string statusMessage);
}
