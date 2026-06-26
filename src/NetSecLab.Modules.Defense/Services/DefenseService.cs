using System;
using System.Collections.Generic;
using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Defense.Services;

internal sealed class DefenseService : IDefenseService
{
    private readonly Dictionary<string, RateCounter> _rateCounters = new();
    private readonly object _syncRoot = new();

    private DateTime _synWindowStartedAt = DateTime.Now;
    private int _synPacketsInWindow;

    private const int SynCookiesActivationThreshold = 20;

    public bool IsAvailable => true;

    public DefenseSettings Settings { get; } = new();

    public PacketInspectionResult Inspect(LogicalPacket packet)
    {
        if (!Settings.IsEnabled)
        {
            return Allow(packet, "—", "Защита отключена.");
        }

        if (Settings.BehaviorFilterEnabled && IsSlowlorisLikePacket(packet))
        {
            return Block(
                packet,
                "Поведенческий фильтр",
                "Slowloris");
        }

        if (Settings.RateLimitEnabled && IsRateLimitExceeded(packet))
        {
            return Block(
                packet,
                "Rate limiting",
                "Превышен лимит.");
        }

        if (Settings.SynCookiesEnabled && IsSynFloodSuspicious(packet))
        {
            return Mitigate(
                packet,
                "SYN cookies",
                "Повышенная частота SYN.");
        }

        return Allow(packet, "—", "Нарушений не обнаружено.");
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            _rateCounters.Clear();
            _synWindowStartedAt = DateTime.Now;
            _synPacketsInWindow = 0;
        }
    }

    private bool IsRateLimitExceeded(LogicalPacket packet)
    {
        string key = CreateRateLimitKey(packet);
        DateTime now = DateTime.Now;

        lock (_syncRoot)
        {
            if (!_rateCounters.TryGetValue(key, out RateCounter? counter) ||
                (now - counter.WindowStartedAt).TotalSeconds >= 1)
            {
                counter = new RateCounter(now);
                _rateCounters[key] = counter;
            }

            counter.Count++;

            return counter.Count > Settings.RateLimitPerSecond;
        }
    }

    private bool IsSynFloodSuspicious(LogicalPacket packet)
    {
        if (!IsTcpSynPacket(packet))
        {
            return false;
        }

        DateTime now = DateTime.Now;

        lock (_syncRoot)
        {
            if ((now - _synWindowStartedAt).TotalSeconds >= 1)
            {
                _synWindowStartedAt = now;
                _synPacketsInWindow = 0;
            }

            _synPacketsInWindow++;

            return _synPacketsInWindow >= SynCookiesActivationThreshold;
        }
    }

    private static string CreateRateLimitKey(LogicalPacket packet)
    {
        string destinationPort = packet.DestinationPort.HasValue
            ? packet.DestinationPort.Value.ToString()
            : "-";

        return packet.DestinationIp + "|" +
               destinationPort + "|" +
               packet.Protocol + "|" +
               NormalizeFeature(packet.Flags);
    }

    private static bool IsTcpSynPacket(LogicalPacket packet)
    {
        return packet.Protocol == PacketProtocol.Tcp &&
               string.Equals(packet.Flags, "SYN", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSlowlorisLikePacket(LogicalPacket packet)
    {
        return packet.Protocol == PacketProtocol.Http &&
               packet.Flags.Contains("Partial", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFeature(string feature)
    {
        return string.IsNullOrWhiteSpace(feature)
            ? "-"
            : feature.Trim().ToUpperInvariant();
    }

    private static PacketInspectionResult Allow(
        LogicalPacket packet,
        string ruleName,
        string reason)
    {
        return new PacketInspectionResult(packet, PacketDecision.Allowed, ruleName, reason);
    }

    private static PacketInspectionResult Mitigate(
        LogicalPacket packet,
        string ruleName,
        string reason)
    {
        return new PacketInspectionResult(packet, PacketDecision.Mitigated, ruleName, reason);
    }

    private static PacketInspectionResult Block(
        LogicalPacket packet,
        string ruleName,
        string reason)
    {
        return new PacketInspectionResult(packet, PacketDecision.Blocked, ruleName, reason);
    }

    private sealed class RateCounter
    {
        public RateCounter(DateTime windowStartedAt)
        {
            WindowStartedAt = windowStartedAt;
        }

        public DateTime WindowStartedAt { get; }
        public int Count { get; set; }
    }
}
