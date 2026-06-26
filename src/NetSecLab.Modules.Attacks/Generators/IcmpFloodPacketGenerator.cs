using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Attacks.Generators;

internal sealed class IcmpFloodPacketGenerator : IAttackPacketGenerator
{
    public AttackType AttackType => AttackType.IcmpFlood;

    public LogicalPacket CreatePacket(AttackRunOptions options, Random random)
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.Now,
            SourceIp = IpAddressGenerator.CreateAttackSourceIp(random),
            SourcePort = null,
            DestinationIp = options.TargetIp,
            DestinationPort = null,
            Protocol = PacketProtocol.Icmp,
            Flags = "Echo Request",
            Length = random.Next(72, 128),
            Kind = PacketKind.Attack,
            AttackType = AttackType.IcmpFlood
        };
    }
}
