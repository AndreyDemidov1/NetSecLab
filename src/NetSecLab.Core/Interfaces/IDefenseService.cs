using NetSecLab.Core.Models;

namespace NetSecLab.Core.Interfaces;

public interface IDefenseService
{
    bool IsAvailable { get; }
    DefenseSettings Settings { get; }
    PacketInspectionResult Inspect(LogicalPacket packet);
    void Reset();
}
