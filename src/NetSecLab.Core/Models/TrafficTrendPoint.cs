namespace NetSecLab.Core.Models;

public sealed class TrafficTrendPoint
{
    public TrafficTrendPoint(
        string label,
        int totalCount,
        int attackCount,
        int backgroundCount,
        double attackHeight,
        double backgroundHeight)
    {
        Label = label;
        TotalCount = totalCount;
        AttackCount = attackCount;
        BackgroundCount = backgroundCount;
        AttackHeight = attackHeight;
        BackgroundHeight = backgroundHeight;
    }

    public string Label { get; }
    public int TotalCount { get; }
    public int AttackCount { get; }
    public int BackgroundCount { get; }
    public double AttackHeight { get; }
    public double BackgroundHeight { get; }
}
