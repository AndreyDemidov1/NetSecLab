using Microsoft.Extensions.DependencyInjection;
using NetSecLab.Core.Interfaces;
using NetSecLab.Modules.Simulation.Services;

namespace NetSecLab.Modules.Simulation.DependencyInjection;

public static class SimulationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationModule(this IServiceCollection services)
    {
        services.AddSingleton<IStochasticSimulationService, StochasticSimulationService>();

        return services;
    }
}
