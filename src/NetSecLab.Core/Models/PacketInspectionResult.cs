namespace NetSecLab.Core.Models;

public sealed class PacketInspectionResult
{
    public PacketInspectionResult(
        LogicalPacket packet,
        PacketDecision decision,
        string ruleName,
        string reason)
    {
        Packet = packet;
        Decision = decision;
        RuleName = ruleName;
        Reason = reason;
    }

    public LogicalPacket Packet { get; }
    public PacketDecision Decision { get; }
    public string RuleName { get; }
    public string Reason { get; }
}
