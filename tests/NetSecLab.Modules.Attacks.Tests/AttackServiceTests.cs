using NetSecLab.Core.Events;
using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;
using NetSecLab.Infrastructure.Events;
using NetSecLab.Modules.Attacks.Generators;
using NetSecLab.Modules.Attacks.Services;
using Xunit;

namespace NetSecLab.Modules.Attacks.Tests;

public class AttackServiceTests
{
    [Fact]
    public void Start_Should_Set_IsRunning_And_Publish_AttackStartedEvent()
    {
        InMemoryEventBus eventBus = new();
        AttackRunOptions? startedOptions = null;
        eventBus.Subscribe<AttackStartedEvent>(ev => startedOptions = ev.Options);
        AttackService service = CreateService(eventBus);
        AttackRunOptions options = new()
        {
            AttackType = AttackType.SynFlood,
            TargetIp = "127.0.0.1",
            TargetPort = 80,
            IntensityPerSecond = 1,
            IncludeBackgroundTraffic = false
        };

        try
        {
            service.Start(options);

            Assert.True(service.IsRunning);
            Assert.Same(options, startedOptions);
        }
        finally
        {
            service.Stop();
        }
    }

    [Fact]
    public void Stop_Should_Clear_IsRunning_And_Publish_AttackStoppedEvent()
    {
        InMemoryEventBus eventBus = new();
        int stoppedEvents = 0;
        eventBus.Subscribe<AttackStoppedEvent>(_ => stoppedEvents++);
        AttackService service = CreateService(eventBus);

        service.Start(new AttackRunOptions
        {
            AttackType = AttackType.UdpFlood,
            TargetIp = "127.0.0.1",
            TargetPort = 53,
            IntensityPerSecond = 1,
            IncludeBackgroundTraffic = false
        });

        service.Stop();

        Assert.False(service.IsRunning);
        Assert.Equal(1, stoppedEvents);
    }

    private static AttackService CreateService(IEventBus eventBus)
    {
        IAttackPacketGenerator[] generators =
        {
            new SynFloodPacketGenerator(),
            new UdpFloodPacketGenerator(),
            new IcmpFloodPacketGenerator(),
            new HttpSlowlorisPacketGenerator()
        };

        return new AttackService(
            eventBus,
            generators,
            new BackgroundPacketGenerator(),
            new FixedStochasticSimulationService());
    }

    private sealed class FixedStochasticSimulationService : IStochasticSimulationService
    {
        public bool IsAvailable => true;
        public double CurrentDefenseLoadFactor => 1.0;

        public StochasticTickResult NextTick(StochasticTickInput input)
        {
            return new StochasticTickResult(1, 0, 1, 1.0, null, Array.Empty<StochasticSimulationEvent>());
        }

        public bool ShouldApplyDefense(ScenarioDefenseKind defenseKind) => true;

        public void Reset()
        {
        }
    }
}
