using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Infrastructure.Services;

internal sealed class DisabledStochasticSimulationService : IStochasticSimulationService
{
    public bool IsAvailable => false;
    public double CurrentDefenseLoadFactor => 1.0;

    public StochasticTickResult NextTick(StochasticTickInput input)
    {
        double tickSeconds = Math.Max(0.05, input.TickDuration.TotalSeconds);
        int attackPackets = Math.Max(1, (int)Math.Round(input.BaseIntensityPerSecond * tickSeconds));
        int backgroundPackets = input.IncludeBackgroundTraffic ? 1 : 0;
        int packetsPerSecond = Math.Max(0, (int)Math.Round((attackPackets + backgroundPackets) / tickSeconds));

        return new StochasticTickResult(
            attackPackets,
            backgroundPackets,
            packetsPerSecond,
            1.0,
            null,
            Array.Empty<StochasticSimulationEvent>());
    }

    public bool ShouldApplyDefense(ScenarioDefenseKind defenseKind)
    {
        return true;
    }

    public void Reset()
    {
    }
}
