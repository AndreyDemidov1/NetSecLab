using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Attacks.Generators;

internal sealed class SynFloodPacketGenerator : IAttackPacketGenerator
{
    public AttackType AttackType => AttackType.SynFlood;

    public LogicalPacket CreatePacket(AttackRunOptions options, Random random)
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.Now,
            SourceIp = IpAddressGenerator.CreateAttackSourceIp(random),
            SourcePort = random.Next(1024, 65536),
            DestinationIp = options.TargetIp,
            DestinationPort = options.TargetPort,
            Protocol = PacketProtocol.Tcp,
            Flags = "SYN",
            Length = random.Next(60, 76),
            Kind = PacketKind.Attack,
            AttackType = AttackType.SynFlood
        };
    }
}
