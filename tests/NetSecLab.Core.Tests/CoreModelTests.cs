using NetSecLab.Core.Models;
using Xunit;

namespace NetSecLab.Core.Tests;

public class CoreModelTests
{
    [Fact]
    public void PacketInspectionResult_Should_Store_Packet_Decision_Rule_And_Reason()
    {
        LogicalPacket packet = CreatePacket();

        PacketInspectionResult result = new(
            packet,
            PacketDecision.Blocked,
            "Blacklist",
            "IP источника находится в чёрном списке.");

        Assert.Same(packet, result.Packet);
        Assert.Equal(PacketDecision.Blocked, result.Decision);
        Assert.Equal("Blacklist", result.RuleName);
        Assert.Equal("IP источника находится в чёрном списке.", result.Reason);
    }

    [Fact]
    public void ScenarioEvaluationResult_IsCompleted_Should_Be_True_Only_For_Completed_Status()
    {
        ScenarioEvaluationResult completed = CreateScenarioResult(ScenarioStatus.Completed);
        ScenarioEvaluationResult failed = CreateScenarioResult(ScenarioStatus.Failed);
        ScenarioEvaluationResult inProgress = CreateScenarioResult(ScenarioStatus.InProgress);

        Assert.True(completed.IsCompleted);
        Assert.False(failed.IsCompleted);
        Assert.False(inProgress.IsCompleted);
    }

    [Fact]
    public void VisualizationSnapshot_Should_Keep_Provided_Collections()
    {
        TrafficTrendPoint trendPoint = new("0", 3, 2, 1, 20, 10);
        DistributionItem protocolItem = new("TCP", 2, "66.7%", 66.7);
        DistributionItem decisionItem = new("Пропущен", 1, "33.3%", 33.3);

        VisualizationSnapshot snapshot = new(
            new[] { trendPoint },
            new[] { protocolItem },
            new[] { decisionItem });

        Assert.Same(trendPoint, snapshot.TrafficTrend.Single());
        Assert.Same(protocolItem, snapshot.ProtocolDistribution.Single());
        Assert.Same(decisionItem, snapshot.DecisionDistribution.Single());
    }

    private static LogicalPacket CreatePacket()
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.UtcNow,
            SourceIp = "192.168.1.10",
            SourcePort = 53000,
            DestinationIp = "127.0.0.1",
            DestinationPort = 80,
            Protocol = PacketProtocol.Tcp,
            Flags = "SYN",
            Length = 64,
            Kind = PacketKind.Attack,
            AttackType = AttackType.SynFlood
        };
    }

    private static ScenarioEvaluationResult CreateScenarioResult(ScenarioStatus status)
    {
        return new ScenarioEvaluationResult(
            status,
            0,
            0,
            0,
            0,
            0,
            0,
            "status",
            "criteria",
            "breakdown",
            "reaction");
    }
}
