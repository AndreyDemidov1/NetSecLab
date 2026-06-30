using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Visualization.Services;

internal sealed class RealtimeVisualizationService : IRealtimeVisualizationService
{
    private const int TrendWindowSeconds = 12;
    private const double MaxTrendBarHeight = 88.0;
    private const double MaxDistributionBarWidth = 150.0;

    private readonly object _syncRoot = new();
    private readonly Dictionary<DateTime, TrafficBucket> _trafficBuckets = new();
    private readonly Dictionary<PacketProtocol, int> _protocolCounters = new();
    private readonly Dictionary<PacketDecision, int> _decisionCounters = new();

    public bool IsAvailable => true;

    public void Record(PacketInspectionResult inspection)
    {
        LogicalPacket packet = inspection.Packet;
        DateTime bucketKey = new(
            packet.Timestamp.Year,
            packet.Timestamp.Month,
            packet.Timestamp.Day,
            packet.Timestamp.Hour,
            packet.Timestamp.Minute,
            packet.Timestamp.Second);

        lock (_syncRoot)
        {
            if (!_trafficBuckets.TryGetValue(bucketKey, out TrafficBucket? bucket))
            {
                bucket = new TrafficBucket();
                _trafficBuckets[bucketKey] = bucket;
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
            RemoveOldBuckets(bucketKey);
        }
    }

    public VisualizationSnapshot CreateSnapshot()
    {
        lock (_syncRoot)
        {
            DateTime now = DateTime.Now;
            DateTime currentSecond = new(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                now.Second);

            List<DateTime> seconds = Enumerable
                .Range(0, TrendWindowSeconds)
                .Select(offset => currentSecond.AddSeconds(-(TrendWindowSeconds - 1 - offset)))
                .ToList();

            int maxBucketTotal = seconds
                .Select(second => _trafficBuckets.TryGetValue(second, out TrafficBucket? bucket) ? bucket.TotalCount : 0)
                .DefaultIfEmpty(0)
                .Max();

            if (maxBucketTotal <= 0)
            {
                maxBucketTotal = 1;
            }

            List<TrafficTrendPoint> trend = seconds
                .Select(second => CreateTrendPoint(second, maxBucketTotal))
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
        }
    }

    private TrafficTrendPoint CreateTrendPoint(DateTime second, int maxBucketTotal)
    {
        _trafficBuckets.TryGetValue(second, out TrafficBucket? bucket);

        int attackCount = bucket?.AttackCount ?? 0;
        int backgroundCount = bucket?.BackgroundCount ?? 0;
        int totalCount = attackCount + backgroundCount;
        double scale = MaxTrendBarHeight / maxBucketTotal;

        return new TrafficTrendPoint(
            second.ToString("HH:mm:ss"),
            totalCount,
            attackCount,
            backgroundCount,
            Math.Max(0, attackCount * scale),
            Math.Max(0, backgroundCount * scale));
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
        int maxCount = counters.Values.DefaultIfEmpty(0).Max();

        return orderedKeys
            .Select(key =>
            {
                counters.TryGetValue(key, out int count);
                double share = total == 0 ? 0 : count * 100.0 / total;
                double width = maxCount == 0 ? 0 : count * MaxDistributionBarWidth / maxCount;

                return new DistributionItem(
                    nameSelector(key),
                    count,
                    share.ToString("0.0") + "%",
                    width);
            })
            .ToList();
    }

    private void RemoveOldBuckets(DateTime currentSecond)
    {
        DateTime minAllowed = currentSecond.AddSeconds(-(TrendWindowSeconds - 1));
        List<DateTime> oldKeys = _trafficBuckets.Keys
            .Where(key => key < minAllowed)
            .ToList();

        foreach (DateTime oldKey in oldKeys)
        {
            _trafficBuckets.Remove(oldKey);
        }
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
