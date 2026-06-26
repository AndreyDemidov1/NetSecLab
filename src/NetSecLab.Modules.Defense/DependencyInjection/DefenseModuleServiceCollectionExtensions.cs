using Microsoft.Extensions.DependencyInjection;
using NetSecLab.Core.Interfaces;
using NetSecLab.Modules.Defense.Services;

namespace NetSecLab.Modules.Defense.DependencyInjection;

public static class DefenseModuleServiceCollectionExtensions
{
    public static IServiceCollection AddDefenseModule(this IServiceCollection services)
    {
        services.AddSingleton<IDefenseService, DefenseService>();

        return services;
    }
}
