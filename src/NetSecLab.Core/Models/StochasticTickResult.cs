namespace NetSecLab.Core.Models;

public sealed class StochasticTickResult
{
    public StochasticTickResult(
        int attackPacketCount,
        int backgroundPacketCount,
        int packetsPerSecond,
        double defenseLoadFactor,
        string? attackSourceOverrideIp,
        IReadOnlyList<StochasticSimulationEvent> events)
    {
        AttackPacketCount = attackPacketCount;
        BackgroundPacketCount = backgroundPacketCount;
        PacketsPerSecond = packetsPerSecond;
        DefenseLoadFactor = defenseLoadFactor;
        AttackSourceOverrideIp = attackSourceOverrideIp;
        Events = events;
    }

    public int AttackPacketCount { get; }
    public int BackgroundPacketCount { get; }
    public int PacketsPerSecond { get; }
    public double DefenseLoadFactor { get; }
    public string? AttackSourceOverrideIp { get; }
    public IReadOnlyList<StochasticSimulationEvent> Events { get; }
}
