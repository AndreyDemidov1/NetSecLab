namespace NetSecLab.Core.Models;

public sealed class TrainingScenario
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string GoalText { get; init; } = string.Empty;
    public string VerificationText { get; init; } = string.Empty;
    public AttackType AttackType { get; init; }
    public ScenarioDefenseKind RequiredDefense { get; init; }
    public string RequiredDefenseName { get; init; } = string.Empty;
    public int MinimumPackets { get; init; }
    public int TargetEfficiencyPercent { get; init; }
    public int ExcellentReactionSeconds { get; init; }
    public int AcceptableReactionSeconds { get; init; }
}
