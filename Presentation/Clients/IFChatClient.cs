using Application.Interfaces;

namespace Presentation.Clients;

public interface IFChatClient
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task SendCommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : IFChatCommand;
    Task ReceiveLoopAsync(CancellationToken cancellationToken);
    Task AutoJoinChannelOnStartUpAsync(CancellationToken cancellationToken);
}