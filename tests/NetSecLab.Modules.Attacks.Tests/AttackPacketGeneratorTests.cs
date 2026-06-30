using NetSecLab.Core.Models;
using NetSecLab.Modules.Attacks.Generators;
using Xunit;

namespace NetSecLab.Modules.Attacks.Tests;

public class AttackPacketGeneratorTests
{
    private static readonly AttackRunOptions Options = new()
    {
        TargetIp = "127.0.0.1",
        TargetPort = 8080,
        IntensityPerSecond = 100
    };

    [Fact]
    public void SynFloodPacketGenerator_Should_Create_Tcp_Syn_Attack_Packet()
    {
        SynFloodPacketGenerator generator = new();

        LogicalPacket packet = generator.CreatePacket(Options, new Random(1));

        Assert.Equal(PacketProtocol.Tcp, packet.Protocol);
        Assert.Equal("SYN", packet.Flags);
        Assert.Equal(PacketKind.Attack, packet.Kind);
        Assert.Equal(AttackType.SynFlood, packet.AttackType);
        Assert.Equal(Options.TargetIp, packet.DestinationIp);
        Assert.Equal(Options.TargetPort, packet.DestinationPort);
        Assert.InRange(packet.Length, 60, 75);
        Assert.NotNull(packet.SourcePort);
    }

    [Fact]
    public void UdpFloodPacketGenerator_Should_Create_Udp_Attack_Packet()
    {
        UdpFloodPacketGenerator generator = new();

        LogicalPacket packet = generator.CreatePacket(Options, new Random(2));

        Assert.Equal(PacketProtocol.Udp, packet.Protocol);
        Assert.Equal("Datagram", packet.Flags);
        Assert.Equal(PacketKind.Attack, packet.Kind);
        Assert.Equal(AttackType.UdpFlood, packet.AttackType);
        Assert.Equal(Options.TargetPort, packet.DestinationPort);
        Assert.NotNull(packet.SourcePort);
    }

    [Fact]
    public void IcmpFloodPacketGenerator_Should_Create_Icmp_Attack_Packet_Without_Ports()
    {
        IcmpFloodPacketGenerator generator = new();

        LogicalPacket packet = generator.CreatePacket(Options, new Random(3));

        Assert.Equal(PacketProtocol.Icmp, packet.Protocol);
        Assert.Equal("Echo Request", packet.Flags);
        Assert.Equal(PacketKind.Attack, packet.Kind);
        Assert.Equal(AttackType.IcmpFlood, packet.AttackType);
        Assert.Null(packet.SourcePort);
        Assert.Null(packet.DestinationPort);
    }

    [Fact]
    public void HttpSlowlorisPacketGenerator_Should_Create_Http_Partial_Headers_Attack_Packet()
    {
        HttpSlowlorisPacketGenerator generator = new();

        LogicalPacket packet = generator.CreatePacket(Options, new Random(4));

        Assert.Equal(PacketProtocol.Http, packet.Protocol);
        Assert.Equal("Partial headers", packet.Flags);
        Assert.Equal(PacketKind.Attack, packet.Kind);
        Assert.Equal(AttackType.HttpSlowloris, packet.AttackType);
        Assert.Equal(Options.TargetPort, packet.DestinationPort);
    }

    [Fact]
    public void BackgroundPacketGenerator_Should_Create_Background_Packet_For_Target()
    {
        BackgroundPacketGenerator generator = new();

        LogicalPacket packet = generator.CreatePacket(Options, new Random(5));

        Assert.Equal(PacketKind.Background, packet.Kind);
        Assert.Null(packet.AttackType);
        Assert.Equal(Options.TargetIp, packet.DestinationIp);
        Assert.True(packet.Length > 0);
    }
}
