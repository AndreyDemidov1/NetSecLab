using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Infrastructure.Services;

internal sealed class DisabledRealtimeVisualizationService : IRealtimeVisualizationService
{
    public bool IsAvailable => false;

    public void Record(PacketInspectionResult inspection)
    {
    }

    public VisualizationSnapshot CreateSnapshot()
    {
        return new VisualizationSnapshot(
            Array.Empty<TrafficTrendPoint>(),
            Array.Empty<DistributionItem>(),
            Array.Empty<DistributionItem>());
    }

    public void Reset()
    {
    }
}
