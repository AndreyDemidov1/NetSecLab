using NetSecLab.Core.Models;

namespace NetSecLab.Core.Interfaces;

public interface IScenarioService
{
    bool IsAvailable { get; }
    IReadOnlyList<TrainingScenario> Scenarios { get; }
    TrainingScenario? CurrentScenario { get; }
    ScenarioStatus Status { get; }
    TrainingScenario Start(string scenarioId);
    void Reset();
    ScenarioEvaluationResult Evaluate(ScenarioEvaluationInput input);
}
