using Application.Interfaces;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application;

public sealed class ModuleDispatcher : IModuleDispatcher
{
    private readonly IReadOnlyCollection<ModuleBase> _modules;
    private readonly ILogger<ModuleDispatcher> _logger;

    public ModuleDispatcher(
        IEnumerable<ModuleBase> modules,
        ILogger<ModuleDispatcher> logger)
    {
        _modules = modules.ToArray();
        _logger = logger;
    }

    public async Task DispatchAsync(InboundMessage message)
    {
        foreach (var module in _modules)
        {
            if (!module.CanHandle(message))
                continue;

            try
            {
                await module.HandleAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Module {ModuleName} failed while handling command/message {Command}",
                    module.GetType().Name,
                    message.Command);
            }
        }
    }
}