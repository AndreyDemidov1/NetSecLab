using NetSecLab.Core.Models;

namespace NetSecLab.App.Models;

public sealed class PacketLogItem
{
    public PacketLogItem(PacketInspectionResult inspection)
    {
        LogicalPacket packet = inspection.Packet;

        Time = packet.Timestamp.ToString("HH:mm:ss.fff");
        Source = FormatEndpoint(packet.SourceIp, packet.SourcePort);
        Destination = FormatEndpoint(packet.DestinationIp, packet.DestinationPort);
        Protocol = packet.Protocol.ToString().ToUpperInvariant();
        Flags = string.IsNullOrWhiteSpace(packet.Flags) ? "—" : packet.Flags;
        Length = packet.Length.ToString();
        Kind = packet.Kind == PacketKind.Attack ? "Атакующий" : "Фоновый";
        Decision = FormatDecision(inspection.Decision);
        DefenseRule = inspection.RuleName;
        Reason = ShortenReason(inspection.Reason);
    }

    private static string FormatEndpoint(string ip, int? port)
    {
        return port.HasValue && port.Value > 0
            ? ip + ":" + port.Value
            : ip;
    }

    private static string FormatDecision(PacketDecision decision)
    {
        return decision switch
        {
            PacketDecision.Allowed => "Пропущен",
            PacketDecision.Mitigated => "Защищён",
            PacketDecision.Blocked => "Заблокирован",
            _ => "—"
        };
    }

    private static string ShortenReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "-";
        }

        if (reason.Contains("чёрном списке", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("blacklist", StringComparison.OrdinalIgnoreCase))
        {
            return "Blacklist";
        }

        if (reason.Contains("белом списке", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("whitelist", StringComparison.OrdinalIgnoreCase))
        {
            return "Whitelist";
        }

        if (reason.Contains("Rate", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("лимит", StringComparison.OrdinalIgnoreCase))
        {
            return "Лимит";
        }

        if (reason.Contains("SYN", StringComparison.OrdinalIgnoreCase))
        {
            return "SYN cookies";
        }

        if (reason.Contains("Slowloris", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("HTTP", StringComparison.OrdinalIgnoreCase))
        {
            return "HTTP-фильтр";
        }

        if (reason.Contains("защита отключена", StringComparison.OrdinalIgnoreCase))
        {
            return "Без защиты";
        }

        return reason.Length > 18
            ? reason[..18] + "..."
            : reason;
    }

    public string Time { get; }
    public string Source { get; }
    public string Destination { get; }
    public string Protocol { get; }
    public string Flags { get; }
    public string Length { get; }
    public string Kind { get; }
    public string Decision { get; }
    public string DefenseRule { get; }
    public string Reason { get; }
}
