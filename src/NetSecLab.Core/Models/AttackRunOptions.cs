namespace NetSecLab.Core.Models;

public sealed class AttackRunOptions
{
    public AttackType AttackType { get; init; }
    public string TargetIp { get; init; } = "127.0.0.1";
    public int TargetPort { get; init; } = 80;
    public int IntensityPerSecond { get; init; } = 100;
    public bool IncludeBackgroundTraffic { get; init; } = true;
    public SimulationDifficulty Difficulty { get; init; } = SimulationDifficulty.Medium;
}
