using Microsoft.Extensions.DependencyInjection;
using NetSecLab.Core.Interfaces;
using NetSecLab.Modules.Visualization.Services;

namespace NetSecLab.Modules.Visualization.DependencyInjection;

public static class VisualizationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddVisualizationModule(this IServiceCollection services)
    {
        services.AddSingleton<IRealtimeVisualizationService, RealtimeVisualizationService>();
        return services;
    }
}
