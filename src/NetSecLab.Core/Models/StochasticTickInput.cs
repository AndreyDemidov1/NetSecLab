namespace NetSecLab.Core.Models;

public sealed class StochasticTickInput
{
    public AttackType AttackType { get; init; }
    public int BaseIntensityPerSecond { get; init; }
    public bool IncludeBackgroundTraffic { get; init; }
    public SimulationDifficulty Difficulty { get; init; } = SimulationDifficulty.Medium;
    public TimeSpan TickDuration { get; init; } = TimeSpan.FromMilliseconds(250);
    public int TickIndex { get; init; }
}
