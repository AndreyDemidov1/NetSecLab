using Microsoft.Extensions.DependencyInjection;
using NetSecLab.Core.Interfaces;
using NetSecLab.Modules.Attacks.Generators;
using NetSecLab.Modules.Attacks.Services;

namespace NetSecLab.Modules.Attacks.DependencyInjection;

public static class AttackModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAttackModule(this IServiceCollection services)
    {
        services.AddSingleton<IAttackPacketGenerator, SynFloodPacketGenerator>();
        services.AddSingleton<IAttackPacketGenerator, UdpFloodPacketGenerator>();
        services.AddSingleton<IAttackPacketGenerator, IcmpFloodPacketGenerator>();
        services.AddSingleton<IAttackPacketGenerator, HttpSlowlorisPacketGenerator>();
        services.AddSingleton<BackgroundPacketGenerator>();
        services.AddSingleton<IAttackService, AttackService>();

        return services;
    }
}
