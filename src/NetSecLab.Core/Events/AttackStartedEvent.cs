using NetSecLab.Core.Models;

namespace NetSecLab.Core.Events;

public sealed class AttackStartedEvent
{
    public AttackStartedEvent(AttackRunOptions options)
    {
        Options = options;
    }

    public AttackRunOptions Options { get; }
}
