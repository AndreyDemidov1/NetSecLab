using NetSecLab.Core.Models;

namespace NetSecLab.App.Models;

public sealed class SimulationDifficultyOption
{
    public SimulationDifficultyOption(SimulationDifficulty value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public SimulationDifficulty Value { get; }
    public string DisplayName { get; }
}
