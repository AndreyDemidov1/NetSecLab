using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Attacks.Generators;

internal sealed class UdpFloodPacketGenerator : IAttackPacketGenerator
{
    public AttackType AttackType => AttackType.UdpFlood;

    public LogicalPacket CreatePacket(AttackRunOptions options, Random random)
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.Now,
            SourceIp = IpAddressGenerator.CreateAttackSourceIp(random),
            SourcePort = random.Next(1024, 65536),
            DestinationIp = options.TargetIp,
            DestinationPort = options.TargetPort,
            Protocol = PacketProtocol.Udp,
            Flags = "Datagram",
            Length = random.Next(64, 512),
            Kind = PacketKind.Attack,
            AttackType = AttackType.UdpFlood
        };
    }
}
