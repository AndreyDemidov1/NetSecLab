using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Scenarios.Services;

internal sealed class ScenarioService : IScenarioService
{
    private readonly List<TrainingScenario> _scenarios;
    private TrainingScenario? _currentScenario;

    public ScenarioService()
    {
        _scenarios = new List<TrainingScenario>
        {
            new()
            {
                Id = "syn-flood-syncookies",
                Title = "Обнаружить SYN-flood и включить SYN cookies",
                GoalText = "Запустить SYN-flood, распознать поток SYN-пакетов и нейтрализовать его механизмом SYN cookies.",
                VerificationText = "Проверяется выбор атаки, включение SYN cookies, скорость реакции и процент нейтрализованных пакетов.",
                AttackType = AttackType.SynFlood,
                RequiredDefense = ScenarioDefenseKind.SynCookies,
                RequiredDefenseName = "SYN cookies",
                MinimumPackets = 50,
                TargetEfficiencyPercent = 70,
                ExcellentReactionSeconds = 5,
                AcceptableReactionSeconds = 20
            },
            new()
            {
                Id = "udp-flood-ratelimit",
                Title = "Остановить UDP-флуд с помощью rate limiting",
                GoalText = "Запустить UDP-flood и ограничить однотипный поток пакетов с помощью rate limiting.",
                VerificationText = "Проверяется выбор UDP-flood, активный rate limiting, скорость реакции и снижение проходящего потока.",
                AttackType = AttackType.UdpFlood,
                RequiredDefense = ScenarioDefenseKind.RateLimit,
                RequiredDefenseName = "Rate limiting",
                MinimumPackets = 50,
                TargetEfficiencyPercent = 60,
                ExcellentReactionSeconds = 7,
                AcceptableReactionSeconds = 25
            },
            new()
            {
                Id = "slowloris-behavior-filter",
                Title = "Выявить Slowloris поведенческим анализом",
                GoalText = "Запустить HTTP Slowloris и применить поведенческий фильтр к медленным частичным HTTP-запросам.",
                VerificationText = "Проверяется выбор Slowloris, включение поведенческого фильтра, реакция и эффективность блокировки.",
                AttackType = AttackType.HttpSlowloris,
                RequiredDefense = ScenarioDefenseKind.BehaviorFilter,
                RequiredDefenseName = "Поведенческий фильтр",
                MinimumPackets = 30,
                TargetEfficiencyPercent = 80,
                ExcellentReactionSeconds = 8,
                AcceptableReactionSeconds = 30
            }
        };
    }

    public bool IsAvailable => true;
    public IReadOnlyList<TrainingScenario> Scenarios => _scenarios;
    public TrainingScenario? CurrentScenario => _currentScenario;
    public ScenarioStatus Status { get; private set; } = ScenarioStatus.NotStarted;

    public TrainingScenario Start(string scenarioId)
    {
        TrainingScenario scenario = _scenarios.FirstOrDefault(item => item.Id == scenarioId)
            ?? throw new InvalidOperationException("Выбранный учебный сценарий не найден.");

        _currentScenario = scenario;
        Status = ScenarioStatus.InProgress;

        return scenario;
    }

    public void Reset()
    {
        Status = ScenarioStatus.NotStarted;
    }

    public ScenarioEvaluationResult Evaluate(ScenarioEvaluationInput input)
    {
        if (_currentScenario is null || Status == ScenarioStatus.NotStarted)
        {
            return CreateResult(
                ScenarioStatus.NotStarted,
                0,
                0,
                0,
                0,
                0,
                CalculateEfficiency(input),
                "Сценарий не запущен.",
                "Выберите сценарий и нажмите \"Начать\".",
                "Реакция не зафиксирована.");
        }

        double efficiency = CalculateEfficiency(input);
        bool attackMatches = input.AttackType == _currentScenario.AttackType;
        bool requiredDefenseEnabled = IsRequiredDefenseEnabled(input, _currentScenario.RequiredDefense);

        int choiceScore = CalculateChoiceScore(input, attackMatches, requiredDefenseEnabled);
        int reactionScore = CalculateReactionScore(input);
        int efficiencyScore = CalculateEfficiencyScore(efficiency);
        int adaptivityScore = CalculateAdaptivityScore(input, requiredDefenseEnabled);
        int totalScore = Math.Clamp(choiceScore + reactionScore + efficiencyScore + adaptivityScore, 0, 100);

        bool minimumTrafficReached = input.ReceivedPackets >= _currentScenario.MinimumPackets;
        bool targetEfficiencyReached = efficiency >= _currentScenario.TargetEfficiencyPercent;
        bool completed = attackMatches && input.ProtectionEnabled && requiredDefenseEnabled && minimumTrafficReached && targetEfficiencyReached;

        if (completed)
        {
            Status = ScenarioStatus.Completed;
        }

        string statusText = CreateStatusText(
            attackMatches,
            input.ProtectionEnabled,
            requiredDefenseEnabled,
            minimumTrafficReached,
            targetEfficiencyReached,
            efficiency);

        return CreateResult(
            Status,
            totalScore,
            reactionScore,
            efficiencyScore,
            choiceScore,
            adaptivityScore,
            efficiency,
            statusText,
            CreateCriteriaText(),
            CreateReactionTimeText(input));
    }

    private string CreateStatusText(
        bool attackMatches,
        bool protectionEnabled,
        bool requiredDefenseEnabled,
        bool minimumTrafficReached,
        bool targetEfficiencyReached,
        double efficiency)
    {
        if (Status == ScenarioStatus.Completed)
        {
            return "Сценарий пройден: атака распознана, защита выбрана корректно, поток нейтрализован.";
        }

        if (!attackMatches)
        {
            return "Ожидается атака: " + FormatAttackType(_currentScenario!.AttackType) + ".";
        }

        if (!protectionEnabled)
        {
            return "Атака выбрана верно. Теперь включите общий переключатель защиты.";
        }

        if (!requiredDefenseEnabled)
        {
            return "Защита включена, но для сценария нужен механизм: " + _currentScenario!.RequiredDefenseName + ".";
        }

        if (!minimumTrafficReached)
        {
            return "Защита выбрана верно. Идёт сбор статистики по пакетам сценария.";
        }

        if (!targetEfficiencyReached)
        {
            return "Защита выбрана верно, но эффективности пока недостаточно: " + efficiency.ToString("0.0") + "%";
        }

        return "Сценарий выполняется.";
    }

    private ScenarioEvaluationResult CreateResult(
        ScenarioStatus status,
        int score,
        int reactionScore,
        int efficiencyScore,
        int choiceScore,
        int adaptivityScore,
        double efficiency,
        string statusText,
        string criteriaText,
        string reactionTimeText)
    {
        return new ScenarioEvaluationResult(
            status,
            score,
            reactionScore,
            efficiencyScore,
            choiceScore,
            adaptivityScore,
            efficiency,
            statusText,
            criteriaText,
            CreateScoreBreakdownText(reactionScore, choiceScore, efficiencyScore, adaptivityScore),
            reactionTimeText);
    }

    private int CalculateChoiceScore(
        ScenarioEvaluationInput input,
        bool attackMatches,
        bool requiredDefenseEnabled)
    {
        int score = 0;

        if (attackMatches)
        {
            score += 10;
        }

        if (input.ProtectionEnabled)
        {
            score += 5;
        }

        if (requiredDefenseEnabled)
        {
            score += 15;
        }

        return Math.Clamp(score, 0, 30);
    }

    private int CalculateReactionScore(ScenarioEvaluationInput input)
    {
        if (input.AttackStartedAt is null || input.FirstCorrectDefenseEnabledAt is null)
        {
            return 0;
        }

        double seconds = (input.FirstCorrectDefenseEnabledAt.Value - input.AttackStartedAt.Value).TotalSeconds;

        if (seconds <= _currentScenario!.ExcellentReactionSeconds)
        {
            return 20;
        }

        if (seconds >= _currentScenario.AcceptableReactionSeconds)
        {
            return 5;
        }

        double range = _currentScenario.AcceptableReactionSeconds - _currentScenario.ExcellentReactionSeconds;
        double penaltyPart = (seconds - _currentScenario.ExcellentReactionSeconds) / Math.Max(1, range);

        return Math.Clamp(20 - (int)Math.Round(penaltyPart * 15), 5, 20);
    }

    private int CalculateEfficiencyScore(double efficiency)
    {
        int target = Math.Max(1, _currentScenario!.TargetEfficiencyPercent);
        return Math.Clamp((int)Math.Round(efficiency * 35 / target), 0, 35);
    }

    private static int CalculateAdaptivityScore(
        ScenarioEvaluationInput input,
        bool requiredDefenseEnabled)
    {
        if (!requiredDefenseEnabled)
        {
            return 0;
        }

        int score = 0;

        if (input.DefenseConfigurationChanges > 0)
        {
            score += Math.Min(10, input.DefenseConfigurationChanges * 4);
        }

        if (input.AdditionalDefenseUsed)
        {
            score += 5;
        }

        return Math.Clamp(score, 0, 15);
    }

    private string CreateCriteriaText()
    {
        if (_currentScenario is null)
        {
            return "Критерии сценария не выбраны.";
        }

        return "Критерии: " +
               FormatAttackType(_currentScenario.AttackType) + ", " +
               _currentScenario.RequiredDefenseName + ", минимум " +
               _currentScenario.MinimumPackets + " пакетов, эффективность от " +
               _currentScenario.TargetEfficiencyPercent + "%.";
    }

    private static string CreateScoreBreakdownText(
        int reactionScore,
        int choiceScore,
        int efficiencyScore,
        int adaptivityScore)
    {
        return "Реакция " + reactionScore + "/20 • " +
               "Выбор " + choiceScore + "/30 • " +
               "Эффективность " + efficiencyScore + "/35 • " +
               "Адаптивность " + adaptivityScore + "/15";
    }

    private static string CreateReactionTimeText(ScenarioEvaluationInput input)
    {
        if (input.AttackStartedAt is null)
        {
            return "Реакция: атака ещё не запущена.";
        }

        if (input.FirstCorrectDefenseEnabledAt is null)
        {
            return "Реакция: подходящая защита ещё не включена.";
        }

        double seconds = (input.FirstCorrectDefenseEnabledAt.Value - input.AttackStartedAt.Value).TotalSeconds;
        return "Реакция: " + Math.Max(0, seconds).ToString("0.0") + " сек.";
    }

    private bool IsRequiredDefenseEnabled(
        ScenarioEvaluationInput input,
        ScenarioDefenseKind defenseKind)
    {
        return defenseKind switch
        {
            ScenarioDefenseKind.SynCookies => input.SynCookiesEnabled,
            ScenarioDefenseKind.RateLimit => input.RateLimitEnabled,
            ScenarioDefenseKind.BehaviorFilter => input.BehaviorFilterEnabled,
            ScenarioDefenseKind.Blacklist => input.BlacklistEnabled,
            ScenarioDefenseKind.Whitelist => input.WhitelistEnabled,
            _ => false
        };
    }

    private static double CalculateEfficiency(ScenarioEvaluationInput input)
    {
        if (input.ReceivedPackets == 0)
        {
            return 0;
        }

        int neutralizedPackets = input.MitigatedPackets + input.BlockedPackets;
        return neutralizedPackets * 100.0 / input.ReceivedPackets;
    }

    private static string FormatAttackType(AttackType attackType)
    {
        return attackType switch
        {
            AttackType.SynFlood => "SYN-flood",
            AttackType.UdpFlood => "UDP-flood",
            AttackType.IcmpFlood => "ICMP-flood",
            AttackType.HttpSlowloris => "HTTP Slowloris",
            _ => attackType.ToString()
        };
    }
}
