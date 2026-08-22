using Domain;

namespace Application.Interfaces;

public interface IModuleDispatcher
{
    Task DispatchAsync(InboundMessage message);
}