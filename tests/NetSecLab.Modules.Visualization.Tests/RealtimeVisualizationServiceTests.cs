using NetSecLab.Core.Models;
using NetSecLab.Modules.Visualization.Services;
using Xunit;

namespace NetSecLab.Modules.Visualization.Tests;

public class RealtimeVisualizationServiceTests
{
    [Fact]
    public void CreateSnapshot_Before_Records_Should_Return_Empty_Distributions_And_Fixed_Trend_Window()
    {
        RealtimeVisualizationService service = new();

        VisualizationSnapshot snapshot = service.CreateSnapshot();

        Assert.Equal(12, snapshot.TrafficTrend.Count);
        Assert.All(snapshot.TrafficTrend, point => Assert.Equal(0, point.TotalCount));
        Assert.Equal(4, snapshot.ProtocolDistribution.Count);
        Assert.Equal(3, snapshot.DecisionDistribution.Count);
        Assert.All(snapshot.ProtocolDistribution, item => Assert.Equal(0, item.Count));
        Assert.All(snapshot.DecisionDistribution, item => Assert.Equal(0, item.Count));
    }

    [Fact]
    public void Record_Should_Update_Protocol_And_Decision_Distributions()
    {
        RealtimeVisualizationService service = new();
        DateTime timestamp = new(2026, 6, 30, 12, 0, 0);

        service.Record(CreateInspection(PacketProtocol.Tcp, PacketKind.Attack, PacketDecision.Allowed, timestamp));
        service.Record(CreateInspection(PacketProtocol.Udp, PacketKind.Background, PacketDecision.Blocked, timestamp));

        VisualizationSnapshot snapshot = service.CreateSnapshot();

        DistributionItem tcp = snapshot.ProtocolDistribution.Single(item => item.Name == "TCP");
        DistributionItem udp = snapshot.ProtocolDistribution.Single(item => item.Name == "UDP");
        DistributionItem allowed = snapshot.DecisionDistribution.Single(item => item.Name == "Пропущен");
        DistributionItem blocked = snapshot.DecisionDistribution.Single(item => item.Name == "Заблокирован");

        Assert.Equal(1, tcp.Count);
        Assert.Equal("50.0%", tcp.ShareText);
        Assert.Equal(50.0, tcp.PercentValue);
        Assert.Equal(1, udp.Count);
        Assert.Equal(1, allowed.Count);
        Assert.Equal(1, blocked.Count);
    }

    [Fact]
    public void Record_Should_Keep_Only_Last_Twelve_Active_Seconds_In_Trend()
    {
        RealtimeVisualizationService service = new();
        DateTime start = new(2026, 6, 30, 12, 0, 0);

        for (int i = 0; i < 13; i++)
        {
            service.Record(CreateInspection(PacketProtocol.Tcp, PacketKind.Attack, PacketDecision.Allowed, start.AddSeconds(i)));
        }

        VisualizationSnapshot snapshot = service.CreateSnapshot();
        int totalPacketsInTrend = snapshot.TrafficTrend.Sum(point => point.TotalCount);

        Assert.Equal(12, snapshot.TrafficTrend.Count);
        Assert.Equal(12, totalPacketsInTrend);
    }

    [Fact]
    public void Reset_Should_Clear_Recorded_Data()
    {
        RealtimeVisualizationService service = new();
        service.Record(CreateInspection(PacketProtocol.Http, PacketKind.Attack, PacketDecision.Blocked, DateTime.UtcNow));

        service.Reset();
        VisualizationSnapshot snapshot = service.CreateSnapshot();

        Assert.All(snapshot.ProtocolDistribution, item => Assert.Equal(0, item.Count));
        Assert.All(snapshot.DecisionDistribution, item => Assert.Equal(0, item.Count));
        Assert.All(snapshot.TrafficTrend, point => Assert.Equal(0, point.TotalCount));
    }

    private static PacketInspectionResult CreateInspection(
        PacketProtocol protocol,
        PacketKind kind,
        PacketDecision decision,
        DateTime timestamp)
    {
        LogicalPacket packet = new()
        {
            Timestamp = timestamp,
            SourceIp = "192.168.1.10",
            SourcePort = protocol == PacketProtocol.Icmp ? null : 53000,
            DestinationIp = "127.0.0.1",
            DestinationPort = protocol == PacketProtocol.Icmp ? null : 80,
            Protocol = protocol,
            Flags = protocol == PacketProtocol.Http ? "Partial headers" : "SYN",
            Length = 100,
            Kind = kind,
            AttackType = kind == PacketKind.Attack ? AttackType.SynFlood : null
        };

        return new PacketInspectionResult(packet, decision, "rule", "reason");
    }
}
