using Microsoft.Extensions.DependencyInjection;
using NetSecLab.Core.Interfaces;
using NetSecLab.Modules.Scenarios.Services;

namespace NetSecLab.Modules.Scenarios.DependencyInjection;

public static class ScenarioModuleServiceCollectionExtensions
{
    public static IServiceCollection AddScenarioModule(this IServiceCollection services)
    {
        services.AddSingleton<IScenarioService, ScenarioService>();
        return services;
    }
}
