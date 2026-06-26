using NetSecLab.Core.Models;

namespace NetSecLab.Core.Interfaces;

public interface IAttackService
{
    bool IsAvailable { get; }
    bool IsRunning { get; }
    void Start(AttackRunOptions options);
    void Stop();
}
