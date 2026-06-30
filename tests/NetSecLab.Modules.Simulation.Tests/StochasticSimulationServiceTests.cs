using NetSecLab.Core.Models;
using NetSecLab.Modules.Simulation.Services;

namespace NetSecLab.Modules.Simulation.Tests;

public class StochasticSimulationServiceTests
{
    [Fact]
    public void NextTick_When_BackgroundTraffic_Disabled_Should_Not_Create_Background_Packets()
    {
        StochasticSimulationService service = new();

        StochasticTickResult result = service.NextTick(new StochasticTickInput
        {
            AttackType = AttackType.SynFlood,
            BaseIntensityPerSecond = 100,
            IncludeBackgroundTraffic = false,
            Difficulty = SimulationDifficulty.Medium,
            TickDuration = TimeSpan.FromMilliseconds(250)
        });

        Assert.True(result.AttackPacketCount >= 1);
        Assert.Equal(0, result.BackgroundPacketCount);
        Assert.NotNull(result.Events);
    }

    [Fact]
    public void NextTick_Should_Update_CurrentDefenseLoadFactor_From_Result()
    {
        StochasticSimulationService service = new();

        StochasticTickResult result = service.NextTick(new StochasticTickInput
        {
            AttackType = AttackType.UdpFlood,
            BaseIntensityPerSecond = 120,
            IncludeBackgroundTraffic = true,
            Difficulty = SimulationDifficulty.Hard,
            TickDuration = TimeSpan.FromMilliseconds(250)
        });

        Assert.Equal(result.DefenseLoadFactor, service.CurrentDefenseLoadFactor);
        Assert.InRange(result.DefenseLoadFactor, 0.6, 2.2);
        Assert.True(result.PacketsPerSecond >= 0);
    }

    [Fact]
    public void Reset_Should_Restore_Default_Defense_Load_Factor()
    {
        StochasticSimulationService service = new();
        service.NextTick(new StochasticTickInput
        {
            AttackType = AttackType.IcmpFlood,
            BaseIntensityPerSecond = 500,
            IncludeBackgroundTraffic = true,
            Difficulty = SimulationDifficulty.Hard,
            TickDuration = TimeSpan.FromMilliseconds(250)
        });

        service.Reset();

        Assert.Equal(1.0, service.CurrentDefenseLoadFactor);
    }

    [Fact]
    public void ShouldApplyDefense_For_AccessLists_Should_Always_Return_True()
    {
        StochasticSimulationService service = new();

        Assert.True(service.ShouldApplyDefense(ScenarioDefenseKind.Blacklist));
        Assert.True(service.ShouldApplyDefense(ScenarioDefenseKind.Whitelist));
    }
}
