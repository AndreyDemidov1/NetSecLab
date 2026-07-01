using NetSecLab.Core.Models;
using NetSecLab.Modules.Scenarios.Services;
using Xunit;

namespace NetSecLab.Modules.Scenarios.Tests;

public class ScenarioServiceTests
{
    [Fact]
    public void Constructor_Should_Create_Scenarios_In_Attack_List_Order()
    {
        ScenarioService service = new();

        string[] shortTitles = service.Scenarios.Select(scenario => scenario.ShortTitle).ToArray();

        Assert.Equal(new[] { "SYN-flood", "UDP-flood", "ICMP-flood", "Slowloris" }, shortTitles);
    }

    [Fact]
    public void Start_Should_Set_CurrentScenario_And_InProgress_Status()
    {
        ScenarioService service = new();

        TrainingScenario scenario = service.Start("syn-flood-combo");

        Assert.Equal("syn-flood-combo", scenario.Id);
        Assert.Same(scenario, service.CurrentScenario);
        Assert.Equal(ScenarioStatus.InProgress, service.Status);
    }

    [Fact]
    public void Start_Should_Throw_When_Scenario_Does_Not_Exist()
    {
        ScenarioService service = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            service.Start("missing-scenario"));

        Assert.Contains("не найден", exception.Message);
    }

    [Fact]
    public void Evaluate_When_Scenario_Not_Started_Should_Return_NotStarted_Result()
    {
        ScenarioService service = new();

        ScenarioEvaluationResult result = service.Evaluate(new ScenarioEvaluationInput());

        Assert.Equal(ScenarioStatus.NotStarted, result.Status);
        Assert.Equal(0, result.Score);
        Assert.Contains("Сценарий не запущен", result.StatusText);
    }

    [Fact]
    public void Evaluate_When_All_SynFlood_Conditions_Are_Met_Should_Complete_Scenario()
    {
        ScenarioService service = new();
        DateTime attackStartedAt = DateTime.UtcNow;
        service.Start("syn-flood-combo");

        ScenarioEvaluationResult result = service.Evaluate(new ScenarioEvaluationInput
        {
            AttackType = AttackType.SynFlood,
            ReceivedPackets = 100,
            MitigatedPackets = 60,
            BlockedPackets = 20,
            AllowedPackets = 20,
            AttackIsRunning = true,
            ProtectionEnabled = true,
            SynCookiesEnabled = true,
            RateLimitEnabled = true,
            EnabledDefenseMechanismCount = 2,
            AttackStartedAt = attackStartedAt,
            FirstCorrectDefenseEnabledAt = attackStartedAt.AddSeconds(5),
            DefenseConfigurationChangesAfterAttack = 2
        });

        Assert.Equal(ScenarioStatus.Completed, result.Status);
        Assert.True(result.IsCompleted);
        Assert.Equal(80.0, result.EfficiencyPercent);
        Assert.True(result.Score > 0);
        Assert.Contains("Сценарий пройден", result.StatusText);
    }

    [Fact]
    public void Evaluate_When_Attack_Stopped_Before_Minimum_Traffic_Should_Fail_Scenario()
    {
        ScenarioService service = new();
        DateTime attackStartedAt = DateTime.UtcNow;
        service.Start("syn-flood-combo");

        ScenarioEvaluationResult result = service.Evaluate(new ScenarioEvaluationInput
        {
            AttackType = AttackType.SynFlood,
            ReceivedPackets = 20,
            MitigatedPackets = 20,
            BlockedPackets = 0,
            AttackIsRunning = false,
            ProtectionEnabled = true,
            SynCookiesEnabled = true,
            RateLimitEnabled = true,
            EnabledDefenseMechanismCount = 2,
            AttackStartedAt = attackStartedAt,
            FirstCorrectDefenseEnabledAt = attackStartedAt.AddSeconds(5),
            DefenseConfigurationChangesAfterAttack = 2
        });

        Assert.Equal(ScenarioStatus.Failed, result.Status);
        Assert.Contains("атака остановлена", result.StatusText);
    }

    [Fact]
    public void Evaluate_Should_Reduce_Choice_Score_For_Extra_Defense_Mechanisms()
    {
        DateTime attackStartedAt = DateTime.UtcNow;
        ScenarioEvaluationResult normalChoice = EvaluateSynScenarioWithEnabledMechanismCount(2, attackStartedAt);
        ScenarioEvaluationResult extraChoice = EvaluateSynScenarioWithEnabledMechanismCount(5, attackStartedAt);

        Assert.True(extraChoice.ChoiceScore < normalChoice.ChoiceScore);
    }

    private static ScenarioEvaluationResult EvaluateSynScenarioWithEnabledMechanismCount(
        int enabledDefenseMechanismCount,
        DateTime attackStartedAt)
    {
        ScenarioService service = new();
        service.Start("syn-flood-combo");

        return service.Evaluate(new ScenarioEvaluationInput
        {
            AttackType = AttackType.SynFlood,
            ReceivedPackets = 30,
            MitigatedPackets = 20,
            BlockedPackets = 0,
            AttackIsRunning = true,
            ProtectionEnabled = true,
            SynCookiesEnabled = true,
            RateLimitEnabled = true,
            BehaviorFilterEnabled = enabledDefenseMechanismCount > 2,
            BlacklistEnabled = enabledDefenseMechanismCount > 3,
            WhitelistEnabled = enabledDefenseMechanismCount > 4,
            EnabledDefenseMechanismCount = enabledDefenseMechanismCount,
            AttackStartedAt = attackStartedAt,
            FirstCorrectDefenseEnabledAt = attackStartedAt.AddSeconds(5),
            DefenseConfigurationChangesAfterAttack = 2
        });
    }
}
