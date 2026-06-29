using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Scenarios.Services;

internal sealed class ScenarioService : IScenarioService
{
    private const int ReactionMaxScore = 15;
    private const int ChoiceMaxScore = 35;
    private const int EfficiencyMaxScore = 35;
    private const int AdaptivityMaxScore = 15;

    private readonly List<TrainingScenario> _scenarios;
    private TrainingScenario? _currentScenario;

    public ScenarioService()
    {
        _scenarios = new List<TrainingScenario>
        {
            new()
            {
                Id = "syn-flood-combo",
                ShortTitle = "SYN-flood",
                Title = "SYN-flood: SYN cookies + rate limiting",
                GoalText = "Сдержать SYN-flood комбинацией SYN cookies и ограничения частоты однотипных пакетов.",
                VerificationText = "Условия: SYN-flood, общая защита, SYN cookies, rate limiting, статистика по пакету и целевая нейтрализация.",
                AttackType = AttackType.SynFlood,
                RequiredDefenses = new[] { ScenarioDefenseKind.SynCookies, ScenarioDefenseKind.RateLimit },
                RequiredDefenseName = "SYN cookies + Rate limiting",
                MinimumPackets = 80,
                TargetEfficiencyPercent = 75,
                ExcellentReactionSeconds = 8,
                AcceptableReactionSeconds = 25
            },
            new()
            {
                Id = "udp-flood-ratelimit-blacklist",
                ShortTitle = "UDP-flood",
                Title = "UDP-flood: rate limiting + blacklist",
                GoalText = "Ограничить UDP-flood через rate limiting и вручную добавить подозрительный источник в чёрный список.",
                VerificationText = "Условия: UDP-flood, общая защита, rate limiting, включённый blacklist и хотя бы один IP в чёрном списке.",
                AttackType = AttackType.UdpFlood,
                RequiredDefenses = new[] { ScenarioDefenseKind.RateLimit, ScenarioDefenseKind.Blacklist },
                RequiredDefenseName = "Rate limiting + Blacklist",
                RequiresBlacklistEntry = true,
                MinimumPackets = 80,
                TargetEfficiencyPercent = 65,
                ExcellentReactionSeconds = 10,
                AcceptableReactionSeconds = 30
            },
            new()
            {
                Id = "icmp-flood-whitelist",
                ShortTitle = "ICMP-flood",
                Title = "ICMP-flood: whitelist",
                GoalText = "Ограничить ICMP-flood режимом белого списка, пропуская только доверенные источники.",
                VerificationText = "Условия: ICMP-flood, общая защита, включённый whitelist и хотя бы один IP в белом списке.",
                AttackType = AttackType.IcmpFlood,
                RequiredDefenses = new[] { ScenarioDefenseKind.Whitelist },
                RequiredDefenseName = "Whitelist",
                RequiresWhitelistEntry = true,
                MinimumPackets = 60,
                TargetEfficiencyPercent = 70,
                ExcellentReactionSeconds = 10,
                AcceptableReactionSeconds = 30
            },
            new()
            {
                Id = "slowloris-behavior-whitelist",
                ShortTitle = "Slowloris",
                Title = "Slowloris: filter + whitelist",
                GoalText = "Выявить медленную HTTP-атаку поведенческим фильтром и усилить режимом доверенных IP.",
                VerificationText = "Условия: HTTP Slowloris, общая защита, поведенческий фильтр, включённый whitelist и хотя бы один доверенный IP.",
                AttackType = AttackType.HttpSlowloris,
                RequiredDefenses = new[] { ScenarioDefenseKind.BehaviorFilter, ScenarioDefenseKind.Whitelist },
                RequiredDefenseName = "Поведенческий фильтр + Whitelist",
                RequiresWhitelistEntry = true,
                MinimumPackets = 50,
                TargetEfficiencyPercent = 80,
                ExcellentReactionSeconds = 12,
                AcceptableReactionSeconds = 35
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
        _currentScenario = null;
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
                "Реакция: атака ещё не запущена.");
        }

        double efficiency = CalculateEfficiency(input);
        bool attackMatches = input.AttackType == _currentScenario.AttackType;
        bool requiredDefensesEnabled = AreRequiredDefensesEnabled(input, _currentScenario);
        bool accessListRequirementsMet = AreAccessListRequirementsMet(input, _currentScenario);
        bool defenseChoiceReady = requiredDefensesEnabled && accessListRequirementsMet;

        int choiceScore = CalculateChoiceScore(input, attackMatches, requiredDefensesEnabled, accessListRequirementsMet);
        int reactionScore = CalculateReactionScore(input, defenseChoiceReady);
        int efficiencyScore = CalculateEfficiencyScore(efficiency);
        int adaptivityScore = CalculateAdaptivityScore(input, _currentScenario, defenseChoiceReady);
        int totalScore = Math.Clamp(choiceScore + reactionScore + efficiencyScore + adaptivityScore, 0, 100);

        bool minimumTrafficReached = input.ReceivedPackets >= _currentScenario.MinimumPackets;
        bool targetEfficiencyReached = efficiency >= _currentScenario.TargetEfficiencyPercent;
        bool completed = attackMatches &&
                         input.ProtectionEnabled &&
                         defenseChoiceReady &&
                         input.AttackStartedAt is not null &&
                         input.FirstCorrectDefenseEnabledAt is not null &&
                         !input.CorrectDefenseWasEnabledBeforeAttack &&
                         minimumTrafficReached &&
                         targetEfficiencyReached;

        bool failed = !completed &&
                      input.AttackStartedAt is not null &&
                      !input.AttackIsRunning &&
                      input.ReceivedPackets > 0;

        if (completed)
        {
            Status = ScenarioStatus.Completed;
        }
        else if (failed)
        {
            Status = ScenarioStatus.Failed;
        }

        string statusText = CreateStatusText(
            input,
            attackMatches,
            requiredDefensesEnabled,
            accessListRequirementsMet,
            minimumTrafficReached,
            targetEfficiencyReached);

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
        ScenarioEvaluationInput input,
        bool attackMatches,
        bool requiredDefensesEnabled,
        bool accessListRequirementsMet,
        bool minimumTrafficReached,
        bool targetEfficiencyReached)
    {
        if (Status == ScenarioStatus.Completed)
        {
            return "Сценарий пройден: атака распознана, защита подобрана и поток нейтрализован.";
        }

        if (Status == ScenarioStatus.Failed)
        {
            return CreateFailedStatusText(
                input,
                attackMatches,
                requiredDefensesEnabled,
                accessListRequirementsMet,
                minimumTrafficReached,
                targetEfficiencyReached);
        }

        if (!attackMatches)
        {
            return "Ожидается атака: " + FormatAttackType(_currentScenario!.AttackType) + ".";
        }

        if (!input.ProtectionEnabled)
        {
            return "Атака выбрана верно. Включите общий переключатель защиты.";
        }

        if (!requiredDefensesEnabled)
        {
            return "Включите механизмы: " + CreateMissingDefenseText(input, _currentScenario!) + ".";
        }

        if (!accessListRequirementsMet)
        {
            return CreateAccessListRequirementText(input, _currentScenario!);
        }

        int extraDefenseCount = Math.Max(0, input.EnabledDefenseMechanismCount - _currentScenario!.RequiredDefenses.Count);
        if (extraDefenseCount > 0 && !minimumTrafficReached)
        {
            return "Подходящая защита выбрана, но включены лишние механизмы: балл за выбор будет снижен.";
        }

        if (input.CorrectDefenseWasEnabledBeforeAttack)
        {
            return "Защита выбрана, но реакция не засчитана: механизм был включён до начала атаки.";
        }

        if (!minimumTrafficReached)
        {
            return "Защита выбрана верно. Идёт накопление статистики по сценарию.";
        }

        if (!targetEfficiencyReached)
        {
            return "Условия выбраны верно, но поток ещё недостаточно нейтрализован.";
        }

        return "Сценарий выполняется.";
    }

    private string CreateFailedStatusText(
        ScenarioEvaluationInput input,
        bool attackMatches,
        bool requiredDefensesEnabled,
        bool accessListRequirementsMet,
        bool minimumTrafficReached,
        bool targetEfficiencyReached)
    {
        if (!attackMatches)
        {
            return "Сценарий не пройден: была запущена не та атака. Нажмите «Сбросить» и повторите попытку.";
        }

        if (!input.ProtectionEnabled)
        {
            return "Сценарий не пройден: защита не была включена. Нажмите «Сбросить» и повторите попытку.";
        }

        if (!requiredDefensesEnabled || !accessListRequirementsMet)
        {
            return "Сценарий не пройден: выбран неполный набор защитных механизмов. Нажмите «Сбросить» и повторите попытку.";
        }

        if (input.CorrectDefenseWasEnabledBeforeAttack)
        {
            return "Сценарий не пройден: защита была включена заранее, поэтому реакция пользователя не засчитана.";
        }

        if (!minimumTrafficReached)
        {
            return "Сценарий не пройден: атака остановлена до накопления достаточной статистики.";
        }

        if (!targetEfficiencyReached)
        {
            return "Сценарий не пройден: эффективность нейтрализации ниже целевого значения.";
        }

        return "Сценарий не пройден. Нажмите «Сбросить» и повторите попытку.";
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
        bool requiredDefensesEnabled,
        bool accessListRequirementsMet)
    {
        int score = 0;

        if (attackMatches)
        {
            score += 8;
        }

        if (input.ProtectionEnabled)
        {
            score += 7;
        }

        if (_currentScenario is not null && _currentScenario.RequiredDefenses.Count > 0)
        {
            int enabledRequiredCount = CountEnabledRequiredDefenses(input, _currentScenario.RequiredDefenses);
            score += (int)Math.Round(enabledRequiredCount * 15.0 / _currentScenario.RequiredDefenses.Count);
        }

        if (requiredDefensesEnabled && accessListRequirementsMet)
        {
            score += 5;
        }

        if (_currentScenario is not null)
        {
            int extraDefenseCount = Math.Max(0, input.EnabledDefenseMechanismCount - _currentScenario.RequiredDefenses.Count);
            score -= extraDefenseCount * 6;
        }

        return Math.Clamp(score, 0, ChoiceMaxScore);
    }

    private int CalculateReactionScore(
        ScenarioEvaluationInput input,
        bool defenseChoiceReady)
    {
        if (!defenseChoiceReady ||
            input.CorrectDefenseWasEnabledBeforeAttack ||
            input.AttackStartedAt is null ||
            input.FirstCorrectDefenseEnabledAt is null)
        {
            return 0;
        }

        double seconds = (input.FirstCorrectDefenseEnabledAt.Value - input.AttackStartedAt.Value).TotalSeconds;

        if (seconds <= _currentScenario!.ExcellentReactionSeconds)
        {
            return ReactionMaxScore;
        }

        if (seconds >= _currentScenario.AcceptableReactionSeconds)
        {
            return 4;
        }

        double range = _currentScenario.AcceptableReactionSeconds - _currentScenario.ExcellentReactionSeconds;
        double penaltyPart = (seconds - _currentScenario.ExcellentReactionSeconds) / Math.Max(1, range);

        return Math.Clamp(ReactionMaxScore - (int)Math.Round(penaltyPart * 11), 4, ReactionMaxScore);
    }

    private int CalculateEfficiencyScore(double efficiency)
    {
        int target = Math.Max(1, _currentScenario!.TargetEfficiencyPercent);
        return Math.Clamp((int)Math.Round(efficiency * EfficiencyMaxScore / target), 0, EfficiencyMaxScore);
    }

    private int CalculateAdaptivityScore(
        ScenarioEvaluationInput input,
        TrainingScenario scenario,
        bool defenseChoiceReady)
    {
        if (!defenseChoiceReady)
        {
            return 0;
        }

        int score = 0;

        if (scenario.RequiredDefenses.Count > 1)
        {
            score += 6;
        }

        if (scenario.RequiresBlacklistEntry && input.BlacklistedIpCount > 0)
        {
            score += 4;
        }

        if (scenario.RequiresWhitelistEntry && input.WhitelistedIpCount > 0)
        {
            score += 4;
        }

        if (input.DefenseConfigurationChangesAfterAttack > 0)
        {
            score += Math.Min(3, input.DefenseConfigurationChangesAfterAttack);
        }

        if (input.RandomEventsAfterAttack > 0)
        {
            score += 2;
        }

        if (input.DefenseConfigurationChangesAfterRandomEvents > 0)
        {
            score += Math.Min(5, input.DefenseConfigurationChangesAfterRandomEvents * 3);
        }

        return Math.Clamp(score, 0, AdaptivityMaxScore);
    }

    private string CreateCriteriaText()
    {
        if (_currentScenario is null)
        {
            return "Условия сценария не выбраны.";
        }

        return "Условия: " +
               FormatAttackType(_currentScenario.AttackType) + " • " +
               _currentScenario.RequiredDefenseName + " • от " +
               _currentScenario.MinimumPackets + " пакетов • цель " +
               _currentScenario.TargetEfficiencyPercent + "%";
    }

    private static string CreateScoreBreakdownText(
        int reactionScore,
        int choiceScore,
        int efficiencyScore,
        int adaptivityScore)
    {
        return "Реакция " + reactionScore + "/" + ReactionMaxScore + " • " +
               "Выбор " + choiceScore + "/" + ChoiceMaxScore + " • " +
               "Эффективность " + efficiencyScore + "/" + EfficiencyMaxScore + " • " +
               "Адаптивность " + adaptivityScore + "/" + AdaptivityMaxScore;
    }

    private static string CreateReactionTimeText(ScenarioEvaluationInput input)
    {
        if (input.AttackStartedAt is null)
        {
            return "Реакция: атака ещё не запущена.";
        }

        if (input.CorrectDefenseWasEnabledBeforeAttack)
        {
            return "Реакция: не засчитана, защита была включена заранее.";
        }

        if (input.FirstCorrectDefenseEnabledAt is null)
        {
            return "Реакция: подходящая защита ещё не включена.";
        }

        double seconds = (input.FirstCorrectDefenseEnabledAt.Value - input.AttackStartedAt.Value).TotalSeconds;
        return "Реакция: " + Math.Max(0, seconds).ToString("0.0") + " сек.";
    }

    private static bool AreRequiredDefensesEnabled(
        ScenarioEvaluationInput input,
        TrainingScenario scenario)
    {
        return scenario.RequiredDefenses.All(defense => IsDefenseEnabled(input, defense));
    }

    private static int CountEnabledRequiredDefenses(
        ScenarioEvaluationInput input,
        IReadOnlyList<ScenarioDefenseKind> requiredDefenses)
    {
        return requiredDefenses.Count(defense => IsDefenseEnabled(input, defense));
    }

    private static bool AreAccessListRequirementsMet(
        ScenarioEvaluationInput input,
        TrainingScenario scenario)
    {
        if (scenario.RequiresBlacklistEntry && input.BlacklistedIpCount == 0)
        {
            return false;
        }

        if (scenario.RequiresWhitelistEntry && input.WhitelistedIpCount == 0)
        {
            return false;
        }

        return true;
    }

    private static bool IsDefenseEnabled(
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

    private static string CreateMissingDefenseText(
        ScenarioEvaluationInput input,
        TrainingScenario scenario)
    {
        string[] missing = scenario.RequiredDefenses
            .Where(defense => !IsDefenseEnabled(input, defense))
            .Select(FormatDefenseKind)
            .ToArray();

        return missing.Length == 0
            ? scenario.RequiredDefenseName
            : string.Join(", ", missing);
    }

    private static string CreateAccessListRequirementText(
        ScenarioEvaluationInput input,
        TrainingScenario scenario)
    {
        if (scenario.RequiresBlacklistEntry && input.BlacklistedIpCount == 0)
        {
            return "Blacklist включён. Добавьте хотя бы один IP-адрес из журнала в чёрный список.";
        }

        if (scenario.RequiresWhitelistEntry && input.WhitelistedIpCount == 0)
        {
            return "Whitelist включён. Добавьте хотя бы один доверенный IP-адрес в белый список.";
        }

        return "Заполните список доступа для выбранного сценария.";
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

    private static string FormatDefenseKind(ScenarioDefenseKind defenseKind)
    {
        return defenseKind switch
        {
            ScenarioDefenseKind.SynCookies => "SYN cookies",
            ScenarioDefenseKind.RateLimit => "Rate limiting",
            ScenarioDefenseKind.BehaviorFilter => "Поведенческий фильтр",
            ScenarioDefenseKind.Blacklist => "Blacklist",
            ScenarioDefenseKind.Whitelist => "Whitelist",
            _ => defenseKind.ToString()
        };
    }
}
