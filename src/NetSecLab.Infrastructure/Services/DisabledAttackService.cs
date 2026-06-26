using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Infrastructure.Services;

public sealed class DisabledAttackService : IAttackService
{
    public bool IsAvailable => false;
    public bool IsRunning => false;

    public void Start(AttackRunOptions options)
    {
        throw new InvalidOperationException("Генератор атак недоступен, потому что соответствующий функциональный модуль не подключён.");
    }

    public void Stop()
    {
    }
}
