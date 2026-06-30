using NetSecLab.Core.Models;

namespace NetSecLab.Core.Interfaces;

public interface IRealtimeVisualizationService
{
    bool IsAvailable { get; }
    void Record(PacketInspectionResult inspection);
    VisualizationSnapshot CreateSnapshot();
    void Reset();
}
