namespace NetSecLab.Core.Models;

public sealed class ScenarioEvaluationResult
{
    public ScenarioEvaluationResult(
        ScenarioStatus status,
        int score,
        int reactionScore,
        int efficiencyScore,
        int choiceScore,
        int adaptivityScore,
        double efficiencyPercent,
        string statusText,
        string criteriaText,
        string scoreBreakdownText,
        string reactionTimeText)
    {
        Status = status;
        Score = score;
        ReactionScore = reactionScore;
        EfficiencyScore = efficiencyScore;
        ChoiceScore = choiceScore;
        AdaptivityScore = adaptivityScore;
        EfficiencyPercent = efficiencyPercent;
        StatusText = statusText;
        CriteriaText = criteriaText;
        ScoreBreakdownText = scoreBreakdownText;
        ReactionTimeText = reactionTimeText;
    }

    public ScenarioStatus Status { get; }
    public int Score { get; }
    public int ReactionScore { get; }
    public int EfficiencyScore { get; }
    public int ChoiceScore { get; }
    public int AdaptivityScore { get; }
    public double EfficiencyPercent { get; }
    public string StatusText { get; }
    public string CriteriaText { get; }
    public string ScoreBreakdownText { get; }
    public string ReactionTimeText { get; }
    public bool IsCompleted => Status == ScenarioStatus.Completed;
}
