using NetSecLab.Core.Models;

namespace NetSecLab.Core.Interfaces;

public interface IStochasticSimulationService
{
    bool IsAvailable { get; }
    double CurrentDefenseLoadFactor { get; }
    StochasticTickResult NextTick(StochasticTickInput input);
    bool ShouldApplyDefense(ScenarioDefenseKind defenseKind);
    void Reset();
}
