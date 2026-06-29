namespace NetSecLab.Core.Models;

public sealed class ScenarioEvaluationInput
{
    public AttackType AttackType { get; init; }
    public int ReceivedPackets { get; init; }
    public int AllowedPackets { get; init; }
    public int MitigatedPackets { get; init; }
    public int BlockedPackets { get; init; }
    public bool AttackIsRunning { get; init; }
    public bool ProtectionEnabled { get; init; }
    public bool SynCookiesEnabled { get; init; }
    public bool RateLimitEnabled { get; init; }
    public bool BehaviorFilterEnabled { get; init; }
    public bool BlacklistEnabled { get; init; }
    public bool WhitelistEnabled { get; init; }
    public int BlacklistedIpCount { get; init; }
    public int WhitelistedIpCount { get; init; }
    public int EnabledDefenseMechanismCount { get; init; }
    public DateTime? AttackStartedAt { get; init; }
    public DateTime? FirstCorrectDefenseEnabledAt { get; init; }
    public bool CorrectDefenseWasEnabledBeforeAttack { get; init; }
    public int DefenseConfigurationChangesAfterAttack { get; init; }
    public int RandomEventsAfterAttack { get; init; }
    public int DefenseConfigurationChangesAfterRandomEvents { get; init; }
}
