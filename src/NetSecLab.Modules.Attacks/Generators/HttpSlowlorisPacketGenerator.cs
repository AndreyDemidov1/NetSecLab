using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Attacks.Generators;

internal sealed class HttpSlowlorisPacketGenerator : IAttackPacketGenerator
{
    public AttackType AttackType => AttackType.HttpSlowloris;

    public LogicalPacket CreatePacket(AttackRunOptions options, Random random)
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.Now,
            SourceIp = IpAddressGenerator.CreateAttackSourceIp(random),
            SourcePort = random.Next(1024, 65536),
            DestinationIp = options.TargetIp,
            DestinationPort = options.TargetPort,
            Protocol = PacketProtocol.Http,
            Flags = "Partial headers",
            Length = random.Next(80, 180),
            Kind = PacketKind.Attack,
            AttackType = AttackType.HttpSlowloris
        };
    }
}
