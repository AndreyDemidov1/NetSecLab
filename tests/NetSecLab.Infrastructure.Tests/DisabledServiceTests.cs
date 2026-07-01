using NetSecLab.Core.Models;
using NetSecLab.Infrastructure.Services;
using Xunit;

namespace NetSecLab.Infrastructure.Tests;

public class DisabledServiceTests
{
    [Fact]
    public void DisabledAttackService_Start_Should_Throw_Clear_Error()
    {
        DisabledAttackService service = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            service.Start(new AttackRunOptions { AttackType = AttackType.SynFlood }));

        Assert.Contains("Генератор атак недоступен", exception.Message);
        Assert.False(service.IsAvailable);
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void DisabledDefenseService_Inspect_Should_Allow_Packet_With_Module_Not_Connected_Reason()
    {
        DisabledDefenseService service = new();
        LogicalPacket packet = CreatePacket();

        PacketInspectionResult result = service.Inspect(packet);

        Assert.False(service.IsAvailable);
        Assert.Equal(PacketDecision.Allowed, result.Decision);
        Assert.Equal("Без проверки", result.RuleName);
        Assert.Contains("Модуль защиты не подключён", result.Reason);
        Assert.Same(packet, result.Packet);
    }

    private static LogicalPacket CreatePacket()
    {
        return new LogicalPacket
        {
            Timestamp = DateTime.UtcNow,
            SourceIp = "192.168.1.10",
            DestinationIp = "127.0.0.1",
            Protocol = PacketProtocol.Tcp,
            Flags = "ACK",
            Length = 80,
            Kind = PacketKind.Background
        };
    }
}
