namespace NetSecLab.Core.Models;

public sealed class DistributionItem
{
    public DistributionItem(
        string name,
        int count,
        string shareText,
        double barWidth)
    {
        Name = name;
        Count = count;
        ShareText = shareText;
        BarWidth = barWidth;
    }

    public string Name { get; }
    public int Count { get; }
    public string ShareText { get; }
    public double BarWidth { get; }
}
