namespace Application.Interfaces;

public interface IFChatService
{
    public Task HandleMessageAsync(string message);
}