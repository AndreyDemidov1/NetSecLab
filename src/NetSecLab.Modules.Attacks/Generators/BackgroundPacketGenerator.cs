using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Attacks.Generators;

internal sealed class BackgroundPacketGenerator
{
    public LogicalPacket CreatePacket(AttackRunOptions options, Random random)
    {
        int trafficType = random.Next(100);

        if (trafficType < 45)
        {
            return CreateTcpBackgroundPacket(options, random, "ACK");
        }

        if (trafficType < 70)
        {
            return CreateTcpBackgroundPacket(options, random, "PSH/ACK");
        }

        if (trafficType < 85)
        {
            return CreateUdpBackgroundPacket(options, random);
        }

        if (trafficType < 95)
        {
            return CreateIcmpBackgroundPacket(options, random, "Echo Reply");
        }

        return CreateTcpBackgroundPacket(options, random, "SYN");
    }

    private static LogicalPacket CreateTcpBackgroundPacket(
        AttackRunOptions options,
        Random random,
        string flags)
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.Now,
            SourceIp = IpAddressGenerator.CreateBackgroundSourceIp(random),
            SourcePort = random.Next(1024, 65536),
            DestinationIp = options.TargetIp,
            DestinationPort = options.TargetPort,
            Protocol = PacketProtocol.Tcp,
            Flags = flags,
            Length = flags switch
            {
                "SYN" => random.Next(60, 76),
                "ACK" => random.Next(64, 128),
                "PSH/ACK" => random.Next(128, 512),
                _ => random.Next(64, 256)
            },
            Kind = PacketKind.Background,
            AttackType = null
        };
    }

    private static LogicalPacket CreateUdpBackgroundPacket(
        AttackRunOptions options,
        Random random)
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.Now,
            SourceIp = IpAddressGenerator.CreateBackgroundSourceIp(random),
            SourcePort = random.Next(1024, 65536),
            DestinationIp = options.TargetIp,
            DestinationPort = options.TargetPort,
            Protocol = PacketProtocol.Udp,
            Flags = "Datagram",
            Length = random.Next(80, 600),
            Kind = PacketKind.Background,
            AttackType = null
        };
    }

    private static LogicalPacket CreateIcmpBackgroundPacket(
        AttackRunOptions options,
        Random random,
        string messageType)
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.Now,
            SourceIp = IpAddressGenerator.CreateBackgroundSourceIp(random),
            SourcePort = null,
            DestinationIp = options.TargetIp,
            DestinationPort = null,
            Protocol = PacketProtocol.Icmp,
            Flags = messageType,
            Length = random.Next(64, 128),
            Kind = PacketKind.Background,
            AttackType = null
        };
    }
}
