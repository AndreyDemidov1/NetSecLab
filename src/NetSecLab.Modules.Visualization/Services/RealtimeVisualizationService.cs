using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Visualization.Services;

internal sealed class RealtimeVisualizationService : IRealtimeVisualizationService
{
    private const int TrendWindowSeconds = 12;
    private const double MaxTrendBarHeight = 88.0;
    private const int InitialTrendScaleMaximum = 60;

    private readonly object _syncRoot = new();
    private readonly SortedDictionary<int, TrafficBucket> _trafficBuckets = new();
    private readonly Dictionary<PacketProtocol, int> _protocolCounters = new();
    private readonly Dictionary<PacketDecision, int> _decisionCounters = new();

    private int _trendScaleMaximum = InitialTrendScaleMaximum;
    private int _currentBucketIndex;
    private DateTime? _currentBucketSecond;

    public bool IsAvailable => true;

    public void Record(PacketInspectionResult inspection)
    {
        LogicalPacket packet = inspection.Packet;
        DateTime packetSecond = new(
            packet.Timestamp.Year,
            packet.Timestamp.Month,
            packet.Timestamp.Day,
            packet.Timestamp.Hour,
            packet.Timestamp.Minute,
            packet.Timestamp.Second);

        lock (_syncRoot)
        {
            int bucketIndex = GetBucketIndex(packetSecond);

            if (!_trafficBuckets.TryGetValue(bucketIndex, out TrafficBucket? bucket))
            {
                bucket = new TrafficBucket();
                _trafficBuckets[bucketIndex] = bucket;
            }

            if (packet.Kind == PacketKind.Attack)
            {
                bucket.AttackCount++;
            }
            else
            {
                bucket.BackgroundCount++;
            }

            Increment(_protocolCounters, packet.Protocol);
            Increment(_decisionCounters, inspection.Decision);
            RemoveOldBuckets(_currentBucketIndex);
        }
    }

    public VisualizationSnapshot CreateSnapshot()
    {
        lock (_syncRoot)
        {
            int endIndex = _currentBucketSecond is null
                ? TrendWindowSeconds - 1
                : _currentBucketIndex;

            List<int> bucketIndexes = Enumerable
                .Range(0, TrendWindowSeconds)
                .Select(offset => endIndex - (TrendWindowSeconds - 1 - offset))
                .ToList();

            int currentWindowMaximum = bucketIndexes
                .Select(index => _trafficBuckets.TryGetValue(index, out TrafficBucket? bucket) ? bucket.TotalCount : 0)
                .DefaultIfEmpty(0)
                .Max();

            _trendScaleMaximum = Math.Max(
                _trendScaleMaximum,
                SelectStableTrendScaleMaximum(currentWindowMaximum));

            List<TrafficTrendPoint> trend = bucketIndexes
                .Select(index => CreateTrendPoint(index, _trendScaleMaximum))
                .ToList();

            return new VisualizationSnapshot(
                trend,
                CreateProtocolDistribution(),
                CreateDecisionDistribution());
        }
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            _trafficBuckets.Clear();
            _protocolCounters.Clear();
            _decisionCounters.Clear();
            _trendScaleMaximum = InitialTrendScaleMaximum;
            _currentBucketIndex = 0;
            _currentBucketSecond = null;
        }
    }

    private int GetBucketIndex(DateTime packetSecond)
    {
        if (_currentBucketSecond is null)
        {
            _currentBucketSecond = packetSecond;
            return _currentBucketIndex;
        }

        if (packetSecond > _currentBucketSecond.Value)
        {
            _currentBucketIndex++;
            _currentBucketSecond = packetSecond;
        }

        return _currentBucketIndex;
    }

    private TrafficTrendPoint CreateTrendPoint(int bucketIndex, int scaleMaximum)
    {
        _trafficBuckets.TryGetValue(bucketIndex, out TrafficBucket? bucket);

        int attackCount = bucket?.AttackCount ?? 0;
        int backgroundCount = bucket?.BackgroundCount ?? 0;
        int totalCount = attackCount + backgroundCount;
        double scale = MaxTrendBarHeight / scaleMaximum;

        double attackHeight = CalculateTrendHeight(attackCount, scale);
        double backgroundHeight = CalculateTrendHeight(backgroundCount, scale);

        if (attackHeight + backgroundHeight > MaxTrendBarHeight)
        {
            double overflowScale = MaxTrendBarHeight / (attackHeight + backgroundHeight);
            attackHeight *= overflowScale;
            backgroundHeight *= overflowScale;
        }

        return new TrafficTrendPoint(
            bucketIndex.ToString(),
            totalCount,
            attackCount,
            backgroundCount,
            attackHeight,
            backgroundHeight);
    }

    private IReadOnlyList<DistributionItem> CreateProtocolDistribution()
    {
        PacketProtocol[] order =
        {
            PacketProtocol.Tcp,
            PacketProtocol.Udp,
            PacketProtocol.Icmp,
            PacketProtocol.Http
        };

        return CreateDistribution(
            order,
            _protocolCounters,
            protocol => protocol.ToString().ToUpperInvariant());
    }

    private IReadOnlyList<DistributionItem> CreateDecisionDistribution()
    {
        PacketDecision[] order =
        {
            PacketDecision.Allowed,
            PacketDecision.Mitigated,
            PacketDecision.Blocked
        };

        return CreateDistribution(
            order,
            _decisionCounters,
            decision => decision switch
            {
                PacketDecision.Allowed => "Пропущен",
                PacketDecision.Mitigated => "Защищён",
                PacketDecision.Blocked => "Заблокирован",
                _ => "—"
            });
    }

    private static IReadOnlyList<DistributionItem> CreateDistribution<T>(
        IReadOnlyList<T> orderedKeys,
        IReadOnlyDictionary<T, int> counters,
        Func<T, string> nameSelector)
        where T : notnull
    {
        int total = counters.Values.Sum();

        return orderedKeys
            .Select(key =>
            {
                counters.TryGetValue(key, out int count);
                double percent = total == 0 ? 0 : count * 100.0 / total;

                return new DistributionItem(
                    nameSelector(key),
                    count,
                    percent.ToString("0.0") + "%",
                    Math.Clamp(percent, 0, 100));
            })
            .ToList();
    }

    private void RemoveOldBuckets(int currentBucketIndex)
    {
        int minAllowed = currentBucketIndex - (TrendWindowSeconds - 1);
        List<int> oldKeys = _trafficBuckets.Keys
            .Where(key => key < minAllowed)
            .ToList();

        foreach (int oldKey in oldKeys)
        {
            _trafficBuckets.Remove(oldKey);
        }
    }

    private static double CalculateTrendHeight(int count, double scale)
    {
        if (count <= 0)
        {
            return 0;
        }

        return Math.Max(3, count * scale);
    }

    private static int SelectStableTrendScaleMaximum(int currentWindowMaximum)
    {
        return currentWindowMaximum switch
        {
            <= 60 => 60,
            <= 100 => 100,
            <= 200 => 200,
            <= 500 => 500,
            <= 1000 => 1000,
            _ => 1500
        };
    }

    private static void Increment<TKey>(IDictionary<TKey, int> counters, TKey key)
        where TKey : notnull
    {
        counters.TryGetValue(key, out int value);
        counters[key] = value + 1;
    }

    private sealed class TrafficBucket
    {
        public int AttackCount { get; set; }
        public int BackgroundCount { get; set; }
        public int TotalCount => AttackCount + BackgroundCount;
    }
}
