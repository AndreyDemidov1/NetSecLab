namespace NetSecLab.Core.Models;

public sealed class LogicalPacket
{
    public DateTime Timestamp { get; init; }
    public string SourceIp { get; init; } = string.Empty;
    public int? SourcePort { get; init; }
    public string DestinationIp { get; init; } = string.Empty;
    public int? DestinationPort { get; init; }
    public PacketProtocol Protocol { get; init; }
    public string Flags { get; init; } = string.Empty;
    public int Length { get; init; }
    public PacketKind Kind { get; init; }
    public AttackType? AttackType { get; init; }
}
