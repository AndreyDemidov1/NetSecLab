using NetSecLab.Core.Models;

namespace NetSecLab.Core.Events;

public sealed class PacketGeneratedEvent
{
    public PacketGeneratedEvent(LogicalPacket packet)
    {
        Packet = packet;
    }

    public LogicalPacket Packet { get; }
}
