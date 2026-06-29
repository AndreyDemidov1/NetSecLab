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
    private readonly IStochasticSimulationService _stochasticSimulationService;
    private readonly Random _random = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private int _generatedPackets;

    public AttackService(
        IEventBus eventBus,
        IEnumerable<IAttackPacketGenerator> generators,
        BackgroundPacketGenerator backgroundPacketGenerator,
        IStochasticSimulationService stochasticSimulationService)
    {
        _eventBus = eventBus;
        _generators = generators.ToDictionary(generator => generator.AttackType);
        _backgroundPacketGenerator = backgroundPacketGenerator;
        _stochasticSimulationService = stochasticSimulationService;
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
        _stochasticSimulationService.Reset();
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
        TimeSpan delay = TimeSpan.FromMilliseconds(250);
        int tickIndex = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            StochasticTickResult tick = _stochasticSimulationService.NextTick(new StochasticTickInput
            {
                AttackType = options.AttackType,
                BaseIntensityPerSecond = options.IntensityPerSecond,
                IncludeBackgroundTraffic = options.IncludeBackgroundTraffic,
                Difficulty = options.Difficulty,
                TickDuration = delay,
                TickIndex = tickIndex
            });

            PublishStochasticEvents(tick.Events);

            for (int i = 0; i < tick.AttackPacketCount; i++)
            {
                LogicalPacket packet = generator.CreatePacket(options, _random);

                if (!string.IsNullOrWhiteSpace(tick.AttackSourceOverrideIp))
                {
                    packet = ReplaceSourceIp(packet, tick.AttackSourceOverrideIp);
                }

                _generatedPackets++;
                _eventBus.Publish(new PacketGeneratedEvent(packet));
            }

            for (int i = 0; i < tick.BackgroundPacketCount; i++)
            {
                LogicalPacket packet = _backgroundPacketGenerator.CreatePacket(options, _random);
                _eventBus.Publish(new PacketGeneratedEvent(packet));
            }

            _eventBus.Publish(new AttackStatisticsUpdatedEvent(_generatedPackets, tick.PacketsPerSecond));
            tickIndex++;

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

    private void PublishStochasticEvents(IReadOnlyList<StochasticSimulationEvent> events)
    {
        foreach (StochasticSimulationEvent simulationEvent in events)
        {
            _eventBus.Publish(new StochasticSimulationEventRaisedEvent(simulationEvent));
        }
    }

    private static LogicalPacket ReplaceSourceIp(LogicalPacket packet, string sourceIp)
    {
        return new LogicalPacket
        {
            Timestamp = packet.Timestamp,
            SourceIp = sourceIp,
            SourcePort = packet.SourcePort,
            DestinationIp = packet.DestinationIp,
            DestinationPort = packet.DestinationPort,
            Protocol = packet.Protocol,
            Flags = packet.Flags,
            Length = packet.Length,
            Kind = packet.Kind,
            AttackType = packet.AttackType
        };
    }

    public void Dispose()
    {
        Stop();
    }
}
