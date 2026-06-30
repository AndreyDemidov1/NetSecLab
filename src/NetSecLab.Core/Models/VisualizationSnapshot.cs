namespace NetSecLab.Core.Models;

public sealed class VisualizationSnapshot
{
    public VisualizationSnapshot(
        IReadOnlyList<TrafficTrendPoint> trafficTrend,
        IReadOnlyList<DistributionItem> protocolDistribution,
        IReadOnlyList<DistributionItem> decisionDistribution)
    {
        TrafficTrend = trafficTrend;
        ProtocolDistribution = protocolDistribution;
        DecisionDistribution = decisionDistribution;
    }

    public IReadOnlyList<TrafficTrendPoint> TrafficTrend { get; }
    public IReadOnlyList<DistributionItem> ProtocolDistribution { get; }
    public IReadOnlyList<DistributionItem> DecisionDistribution { get; }
}
