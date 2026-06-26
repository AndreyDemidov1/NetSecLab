using NetSecLab.Core.Models;

namespace NetSecLab.App.Models;

public sealed class AttackTypeOption
{
    public AttackTypeOption(AttackType value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public AttackType Value { get; }
    public string DisplayName { get; }
}
