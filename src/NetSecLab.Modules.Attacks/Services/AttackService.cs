using NetSecLab.Core.Events;
using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;
using NetSecLab.Modules.Attacks.Generators;

namespace NetSecLab.Modules.Attacks.Services;

internal sealed class AttackService : IAttackService, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IReadOnlyDictionary<AttackType, IAttackPacketGenerator> _generators;
    private readonly BackgroundPacketGenerator _backgroundPacketGenerator;
    private readonly Random _random = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private int _generatedPackets;

    public AttackService(
        IEventBus eventBus,
        IEnumerable<IAttackPacketGenerator> generators,
        BackgroundPacketGenerator backgroundPacketGenerator)
    {
        _eventBus = eventBus;
        _generators = generators.ToDictionary(generator => generator.AttackType);
        _backgroundPacketGenerator = backgroundPacketGenerator;
    }

    public bool IsAvailable => true;

    public bool IsRunning { get; private set; }

    public void Start(AttackRunOptions options)
    {
        Stop();

        if (!_generators.TryGetValue(options.AttackType, out IAttackPacketGenerator? generator))
        {
            throw new InvalidOperationException("Для выбранного типа атаки не найден генератор пакетов.");
        }

        _generatedPackets = 0;
        IsRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _eventBus.Publish(new AttackStartedEvent(options));

        _ = RunAsync(options, generator, _cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsRunning = false;
        _eventBus.Publish(new AttackStoppedEvent(DateTime.Now));
    }

    private async Task RunAsync(
        AttackRunOptions options,
        IAttackPacketGenerator generator,
        CancellationToken cancellationToken)
    {
        const int ticksPerSecond = 4;
        TimeSpan delay = TimeSpan.FromMilliseconds(250);

        while (!cancellationToken.IsCancellationRequested)
        {
            int packetsInTick = CalculatePacketsForTick(options.IntensityPerSecond, ticksPerSecond);

            for (int i = 0; i < packetsInTick; i++)
            {
                LogicalPacket packet = generator.CreatePacket(options, _random);
                _generatedPackets++;
                _eventBus.Publish(new PacketGeneratedEvent(packet));
            }

            if (options.IncludeBackgroundTraffic)
            {
                int backgroundCount = _random.Next(0, 3);

                for (int i = 0; i < backgroundCount; i++)
                {
                    LogicalPacket packet = _backgroundPacketGenerator.CreatePacket(options, _random);
                    _eventBus.Publish(new PacketGeneratedEvent(packet));
                }
            }

            _eventBus.Publish(new AttackStatisticsUpdatedEvent(_generatedPackets, packetsInTick * ticksPerSecond));

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private int CalculatePacketsForTick(int intensityPerSecond, int ticksPerSecond)
    {
        int baseCount = Math.Max(1, intensityPerSecond / ticksPerSecond);
        int jitter = _random.Next(-baseCount / 4, baseCount / 4 + 1);
        return Math.Max(1, baseCount + jitter);
    }

    public void Dispose()
    {
        Stop();
    }
}
