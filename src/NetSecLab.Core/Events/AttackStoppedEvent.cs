namespace NetSecLab.Core.Events;

public sealed class AttackStoppedEvent
{
    public AttackStoppedEvent(DateTime stoppedAt)
    {
        StoppedAt = stoppedAt;
    }

    public DateTime StoppedAt { get; }
}
