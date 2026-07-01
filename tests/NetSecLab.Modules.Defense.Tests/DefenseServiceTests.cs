using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;
using NetSecLab.Modules.Defense.Services;
using Xunit;

namespace NetSecLab.Modules.Defense.Tests;

public class DefenseServiceTests
{
    [Fact]
    public void Inspect_When_Protection_Disabled_Should_Allow_Packet()
    {
        DefenseService service = CreateService();

        PacketInspectionResult result = service.Inspect(CreatePacket(PacketProtocol.Tcp, "ACK"));

        Assert.Equal(PacketDecision.Allowed, result.Decision);
        Assert.Equal("—", result.RuleName);
        Assert.Contains("Защита отключена", result.Reason);
    }

    [Fact]
    public void Inspect_When_Source_Is_Blacklisted_Should_Block_Packet()
    {
        DefenseService service = CreateService();
        service.Settings.IsEnabled = true;
        service.Settings.BlacklistEnabled = true;
        service.Settings.BlacklistedIps.Add("192.168.1.10");

        PacketInspectionResult result = service.Inspect(CreatePacket(PacketProtocol.Udp, "Datagram", "192.168.1.10"));

        Assert.Equal(PacketDecision.Blocked, result.Decision);
        Assert.Equal("Blacklist", result.RuleName);
    }

    [Fact]
    public void Inspect_When_Whitelist_Enabled_And_Source_Is_Not_Trusted_Should_Block_Packet()
    {
        DefenseService service = CreateService();
        service.Settings.IsEnabled = true;
        service.Settings.WhitelistEnabled = true;
        service.Settings.WhitelistedIps.Add("192.168.1.20");

        PacketInspectionResult result = service.Inspect(CreatePacket(PacketProtocol.Tcp, "ACK", "192.168.1.10"));

        Assert.Equal(PacketDecision.Blocked, result.Decision);
        Assert.Equal("Whitelist", result.RuleName);
    }

    [Fact]
    public void Inspect_When_RateLimit_Is_Exceeded_Should_Block_Packet()
    {
        DefenseService service = CreateService();
        service.Settings.IsEnabled = true;
        service.Settings.RateLimitEnabled = true;
        service.Settings.RateLimitPerSecond = 2;

        PacketInspectionResult first = service.Inspect(CreatePacket(PacketProtocol.Udp, "Datagram"));
        PacketInspectionResult second = service.Inspect(CreatePacket(PacketProtocol.Udp, "Datagram"));
        PacketInspectionResult third = service.Inspect(CreatePacket(PacketProtocol.Udp, "Datagram"));

        Assert.Equal(PacketDecision.Allowed, first.Decision);
        Assert.Equal(PacketDecision.Allowed, second.Decision);
        Assert.Equal(PacketDecision.Blocked, third.Decision);
        Assert.Equal("Rate limiting", third.RuleName);
    }

    [Fact]
    public void Inspect_When_SynFlood_Is_Suspicious_Should_Mitigate_With_SynCookies()
    {
        DefenseService service = CreateService();
        service.Settings.IsEnabled = true;
        service.Settings.SynCookiesEnabled = true;
        PacketInspectionResult result = service.Inspect(CreatePacket(PacketProtocol.Tcp, "SYN"));

        for (int i = 0; i < 19; i++)
        {
            result = service.Inspect(CreatePacket(PacketProtocol.Tcp, "SYN"));
        }

        Assert.Equal(PacketDecision.Mitigated, result.Decision);
        Assert.Equal("SYN cookies", result.RuleName);
    }

    [Fact]
    public void Inspect_When_Slowloris_Like_Packet_Should_Block_With_Behavior_Filter()
    {
        DefenseService service = CreateService();
        service.Settings.IsEnabled = true;
        service.Settings.BehaviorFilterEnabled = true;

        PacketInspectionResult result = service.Inspect(CreatePacket(PacketProtocol.Http, "Partial headers"));

        Assert.Equal(PacketDecision.Blocked, result.Decision);
        Assert.Equal("Поведенческий фильтр", result.RuleName);
    }

    [Fact]
    public void Reset_Should_Clear_Rate_Limit_Counters()
    {
        DefenseService service = CreateService();
        service.Settings.IsEnabled = true;
        service.Settings.RateLimitEnabled = true;
        service.Settings.RateLimitPerSecond = 1;

        service.Inspect(CreatePacket(PacketProtocol.Udp, "Datagram"));
        PacketInspectionResult blocked = service.Inspect(CreatePacket(PacketProtocol.Udp, "Datagram"));

        service.Reset();
        PacketInspectionResult afterReset = service.Inspect(CreatePacket(PacketProtocol.Udp, "Datagram"));

        Assert.Equal(PacketDecision.Blocked, blocked.Decision);
        Assert.Equal(PacketDecision.Allowed, afterReset.Decision);
    }

    private static DefenseService CreateService()
    {
        return new DefenseService(new AlwaysApplyStochasticSimulationService());
    }

    private static LogicalPacket CreatePacket(
        PacketProtocol protocol,
        string flags,
        string sourceIp = "192.168.1.10")
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.UtcNow,
            SourceIp = sourceIp,
            SourcePort = protocol == PacketProtocol.Icmp ? null : 53000,
            DestinationIp = "127.0.0.1",
            DestinationPort = protocol == PacketProtocol.Icmp ? null : 80,
            Protocol = protocol,
            Flags = flags,
            Length = 100,
            Kind = PacketKind.Attack,
            AttackType = AttackType.SynFlood
        };
    }

    private sealed class AlwaysApplyStochasticSimulationService : IStochasticSimulationService
    {
        public bool IsAvailable => true;
        public double CurrentDefenseLoadFactor => 1.0;

        public StochasticTickResult NextTick(StochasticTickInput input)
        {
            return new StochasticTickResult(1, 0, 1, 1.0, null, Array.Empty<StochasticSimulationEvent>());
        }

        public bool ShouldApplyDefense(ScenarioDefenseKind defenseKind) => true;

        public void Reset()
        {
        }
    }
}
