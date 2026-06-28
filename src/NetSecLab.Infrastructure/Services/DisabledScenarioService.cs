using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Infrastructure.Services;

internal sealed class DisabledScenarioService : IScenarioService
{
    public bool IsAvailable => false;
    public IReadOnlyList<TrainingScenario> Scenarios { get; } = Array.Empty<TrainingScenario>();
    public TrainingScenario? CurrentScenario => null;
    public ScenarioStatus Status => ScenarioStatus.NotStarted;

    public TrainingScenario Start(string scenarioId)
    {
        throw new InvalidOperationException("Модуль учебных сценариев не подключён.");
    }

    public void Reset()
    {
    }

    public ScenarioEvaluationResult Evaluate(ScenarioEvaluationInput input)
    {
        return new ScenarioEvaluationResult(
            ScenarioStatus.NotStarted,
            0,
            0,
            0,
            0,
            0,
            0,
            "Модуль сценариев не подключён.",
            "Подключите модуль сценариев для оценки действий пользователя.",
            "Реакция 0/15 • Выбор 0/35 • Эффективность 0/35 • Адаптивность 0/15",
            "Реакция не зафиксирована.");
    }
}
