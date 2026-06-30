namespace NetSecLab.Core.Models;

public sealed class DistributionItem
{
    public DistributionItem(
        string name,
        int count,
        string shareText,
        double percentValue)
    {
        Name = name;
        Count = count;
        ShareText = shareText;
        PercentValue = percentValue;
    }

    public string Name { get; }
    public int Count { get; }
    public string ShareText { get; }
    public double PercentValue { get; }
}
