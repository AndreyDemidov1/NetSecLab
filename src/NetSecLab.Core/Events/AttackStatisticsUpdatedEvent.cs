namespace NetSecLab.Core.Events;

public sealed class AttackStatisticsUpdatedEvent
{
    public AttackStatisticsUpdatedEvent(int generatedPackets, int packetsPerSecond)
    {
        GeneratedPackets = generatedPackets;
        PacketsPerSecond = packetsPerSecond;
    }

    public int GeneratedPackets { get; }
    public int PacketsPerSecond { get; }
}
