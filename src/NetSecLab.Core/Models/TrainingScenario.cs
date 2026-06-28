namespace NetSecLab.Core.Models;

public sealed class TrainingScenario
{
    public string Id { get; init; } = string.Empty;
    public string ShortTitle { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string GoalText { get; init; } = string.Empty;
    public string VerificationText { get; init; } = string.Empty;
    public AttackType AttackType { get; init; }
    public IReadOnlyList<ScenarioDefenseKind> RequiredDefenses { get; init; } = Array.Empty<ScenarioDefenseKind>();
    public string RequiredDefenseName { get; init; } = string.Empty;
    public bool RequiresBlacklistEntry { get; init; }
    public bool RequiresWhitelistEntry { get; init; }
    public int MinimumPackets { get; init; }
    public int TargetEfficiencyPercent { get; init; }
    public int ExcellentReactionSeconds { get; init; }
    public int AcceptableReactionSeconds { get; init; }
}
