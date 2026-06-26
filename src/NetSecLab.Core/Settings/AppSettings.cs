namespace NetSecLab.Core.Settings;

public sealed class AppSettings
{
    public string TargetIp { get; set; } = "127.0.0.1";
    public int TargetPort { get; set; } = 80;
    public int DefaultIntensity { get; set; } = 120;
    public int MaxPacketsInUi { get; set; } = 500;
}
