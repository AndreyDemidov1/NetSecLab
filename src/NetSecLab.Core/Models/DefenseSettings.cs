namespace NetSecLab.Core.Models;

public sealed class DefenseSettings
{
    public bool IsEnabled { get; set; } = false;
    public bool SynCookiesEnabled { get; set; } = false;
    public bool RateLimitEnabled { get; set; } = false;
    public bool BehaviorFilterEnabled { get; set; } = false;
    public int RateLimitPerSecond { get; set; } = 60;
}
