using NetSecLab.Core.Models;

namespace NetSecLab.App.Models;

public sealed class ScenarioOption
{
    public ScenarioOption(TrainingScenario scenario)
    {
        Id = scenario.Id;
        DisplayName = string.IsNullOrWhiteSpace(scenario.ShortTitle)
            ? scenario.Title
            : scenario.ShortTitle;
        GoalText = scenario.GoalText;
        VerificationText = scenario.VerificationText;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string GoalText { get; }
    public string VerificationText { get; }
}
