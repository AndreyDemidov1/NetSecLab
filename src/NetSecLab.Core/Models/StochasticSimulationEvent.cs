namespace NetSecLab.Core.Models;

public sealed class StochasticSimulationEvent
{
    public StochasticSimulationEvent(
        StochasticEventKind kind,
        string title,
        string description,
        DateTime occurredAt)
    {
        Kind = kind;
        Title = title;
        Description = description;
        OccurredAt = occurredAt;
    }

    public StochasticEventKind Kind { get; }
    public string Title { get; }
    public string Description { get; }
    public DateTime OccurredAt { get; }
}
