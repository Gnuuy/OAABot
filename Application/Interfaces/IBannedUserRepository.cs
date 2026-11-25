namespace Application.Interfaces;

public interface IBannedUserRepository
{
    Task<bool> IsBannedAsync(string characterName);

    // Idempotent - banning an already-banned character is a no-op.
    Task BanAsync(string characterName);
}
