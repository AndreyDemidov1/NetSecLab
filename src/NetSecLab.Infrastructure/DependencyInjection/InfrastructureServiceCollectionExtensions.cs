using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Settings;
using NetSecLab.Infrastructure.Events;
using NetSecLab.Infrastructure.Services;

namespace NetSecLab.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddNetSecLabInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<AppSettings>();

        services.TryAddSingleton<IAttackService, DisabledAttackService>();
        services.TryAddSingleton<IDefenseService, DisabledDefenseService>();
        services.TryAddSingleton<IScenarioService, DisabledScenarioService>();
        services.TryAddSingleton<IStochasticSimulationService, DisabledStochasticSimulationService>();

        return services;
    }
}
