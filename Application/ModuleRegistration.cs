using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddModulesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var moduleTypes = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => typeof(ModuleBase).IsAssignableFrom(type));

        foreach (var moduleType in moduleTypes)
        {
            services.AddSingleton(typeof(ModuleBase), moduleType);
        }

        return services;
    }
}