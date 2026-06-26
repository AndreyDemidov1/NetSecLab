using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Infrastructure.Services;

public sealed class DisabledDefenseService : IDefenseService
{
    public bool IsAvailable => false;

    public DefenseSettings Settings { get; } = new()
    {
        IsEnabled = false,
        SynCookiesEnabled = false,
        RateLimitEnabled = false,
        BehaviorFilterEnabled = false
    };

    public PacketInspectionResult Inspect(LogicalPacket packet)
    {
        return new PacketInspectionResult(
            packet,
            PacketDecision.Allowed,
            "Без проверки",
            "Модуль защиты не подключён.");
    }

    public void Reset()
    {
    }
}
