using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Simulation.Services;

internal sealed class StochasticSimulationService : IStochasticSimulationService
{
    private readonly object _syncRoot = new();
    private readonly Random _random = new();

    private int _attackSpikeTicksRemaining;
    private int _legitimateBurstTicksRemaining;
    private int _ipRotationTicksRemaining;
    private int _connectionInstabilityTicksRemaining;
    private double _attackSpikeMultiplier = 1.0;
    private double _legitimateBurstMultiplier = 1.0;
    private string? _rotatedAttackSourceIp;
    private double _currentDefenseLoadFactor = 1.0;

    public bool IsAvailable => true;

    public double CurrentDefenseLoadFactor
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentDefenseLoadFactor;
            }
        }
    }

    public StochasticTickResult NextTick(StochasticTickInput input)
    {
        lock (_syncRoot)
        {
            List<StochasticSimulationEvent> events = new();
            AdvanceActiveEvents();
            TryStartRandomEvent(input.Difficulty, events);

            double tickSeconds = Math.Max(0.05, input.TickDuration.TotalSeconds);
            double difficultyAttackMultiplier = GetDifficultyAttackMultiplier(input.Difficulty);
            double gaussianNoise = NextGaussian(0, GetAttackNoiseStdDev(input.Difficulty));
            double attackMultiplier = Math.Max(0.05, difficultyAttackMultiplier * _attackSpikeMultiplier + gaussianNoise);

            if (_connectionInstabilityTicksRemaining > 0)
            {
                attackMultiplier *= 0.2;
            }

            double attackRate = Math.Max(0, input.BaseIntensityPerSecond * attackMultiplier);
            int attackPackets = Math.Max(0, SamplePoisson(attackRate * tickSeconds));

            if (_connectionInstabilityTicksRemaining == 0 && input.BaseIntensityPerSecond > 0 && attackPackets == 0)
            {
                attackPackets = 1;
            }

            double backgroundRate = input.IncludeBackgroundTraffic
                ? GetBackgroundRate(input.Difficulty) * _legitimateBurstMultiplier
                : 0;
            int backgroundPackets = input.IncludeBackgroundTraffic
                ? SamplePoisson(backgroundRate * tickSeconds)
                : 0;

            int packetsPerSecond = Math.Max(0, (int)Math.Round((attackPackets + backgroundPackets) / tickSeconds));
            _currentDefenseLoadFactor = CalculateDefenseLoadFactor(input.BaseIntensityPerSecond, packetsPerSecond, input.Difficulty);

            return new StochasticTickResult(
                attackPackets,
                backgroundPackets,
                packetsPerSecond,
                _currentDefenseLoadFactor,
                _ipRotationTicksRemaining > 0 ? _rotatedAttackSourceIp : null,
                events);
        }
    }

    public bool ShouldApplyDefense(ScenarioDefenseKind defenseKind)
    {
        lock (_syncRoot)
        {
            double baseProbability = defenseKind switch
            {
                ScenarioDefenseKind.SynCookies => 0.95,
                ScenarioDefenseKind.RateLimit => 0.92,
                ScenarioDefenseKind.BehaviorFilter => 0.90,
                ScenarioDefenseKind.Blacklist => 1.0,
                ScenarioDefenseKind.Whitelist => 1.0,
                _ => 1.0
            };

            if (baseProbability >= 1.0)
            {
                return true;
            }

            double pressurePenalty = Math.Max(0, _currentDefenseLoadFactor - 1.0) * 0.18;
            double probability = Math.Clamp(baseProbability - pressurePenalty, 0.65, baseProbability);

            return _random.NextDouble() <= probability;
        }
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            _attackSpikeTicksRemaining = 0;
            _legitimateBurstTicksRemaining = 0;
            _ipRotationTicksRemaining = 0;
            _connectionInstabilityTicksRemaining = 0;
            _attackSpikeMultiplier = 1.0;
            _legitimateBurstMultiplier = 1.0;
            _rotatedAttackSourceIp = null;
            _currentDefenseLoadFactor = 1.0;
        }
    }

    private void AdvanceActiveEvents()
    {
        if (_attackSpikeTicksRemaining > 0)
        {
            _attackSpikeTicksRemaining--;
            if (_attackSpikeTicksRemaining == 0)
            {
                _attackSpikeMultiplier = 1.0;
            }
        }

        if (_legitimateBurstTicksRemaining > 0)
        {
            _legitimateBurstTicksRemaining--;
            if (_legitimateBurstTicksRemaining == 0)
            {
                _legitimateBurstMultiplier = 1.0;
            }
        }

        if (_ipRotationTicksRemaining > 0)
        {
            _ipRotationTicksRemaining--;
            if (_ipRotationTicksRemaining == 0)
            {
                _rotatedAttackSourceIp = null;
            }
        }

        if (_connectionInstabilityTicksRemaining > 0)
        {
            _connectionInstabilityTicksRemaining--;
        }
    }

    private void TryStartRandomEvent(
        SimulationDifficulty difficulty,
        List<StochasticSimulationEvent> events)
    {
        if (HasActiveRandomEvent())
        {
            return;
        }

        double chance = difficulty switch
        {
            SimulationDifficulty.Easy => 0.025,
            SimulationDifficulty.Medium => 0.055,
            SimulationDifficulty.Hard => 0.09,
            _ => 0.055
        };

        if (_random.NextDouble() > chance)
        {
            return;
        }

        int eventType = _random.Next(100);

        if (eventType < 35)
        {
            StartAttackSpike(difficulty, events);
            return;
        }

        if (eventType < 60)
        {
            StartLegitimateBurst(difficulty, events);
            return;
        }

        if (eventType < 82)
        {
            StartIpRotation(events);
            return;
        }

        StartConnectionInstability(events);
    }

    private bool HasActiveRandomEvent()
    {
        return _attackSpikeTicksRemaining > 0 ||
               _legitimateBurstTicksRemaining > 0 ||
               _ipRotationTicksRemaining > 0 ||
               _connectionInstabilityTicksRemaining > 0;
    }

    private void StartAttackSpike(
        SimulationDifficulty difficulty,
        List<StochasticSimulationEvent> events)
    {
        _attackSpikeTicksRemaining = _random.Next(6, 13);
        _attackSpikeMultiplier = difficulty switch
        {
            SimulationDifficulty.Easy => 1.35 + _random.NextDouble() * 0.25,
            SimulationDifficulty.Medium => 1.55 + _random.NextDouble() * 0.45,
            SimulationDifficulty.Hard => 1.9 + _random.NextDouble() * 0.7,
            _ => 1.55
        };

        events.Add(CreateEvent(
            StochasticEventKind.AttackIntensitySpike,
            "Всплеск атаки",
            "Стохастическое ядро временно усилило атакующий поток."));
    }

    private void StartLegitimateBurst(
        SimulationDifficulty difficulty,
        List<StochasticSimulationEvent> events)
    {
        _legitimateBurstTicksRemaining = _random.Next(6, 14);
        _legitimateBurstMultiplier = difficulty switch
        {
            SimulationDifficulty.Easy => 2.0,
            SimulationDifficulty.Medium => 3.0,
            SimulationDifficulty.Hard => 4.5,
            _ => 3.0
        };

        events.Add(CreateEvent(
            StochasticEventKind.LegitimateTrafficBurst,
            "Всплеск фонового трафика",
            "Появился кратковременный поток легитимных фоновых пакетов."));
    }

    private void StartIpRotation(List<StochasticSimulationEvent> events)
    {
        _ipRotationTicksRemaining = _random.Next(8, 16);
        _rotatedAttackSourceIp = CreateRotatedAttackSourceIp();

        events.Add(CreateEvent(
            StochasticEventKind.AttackerIpRotation,
            "Смена IP атакующего",
            "Атакующий временно использует новый адрес источника: " + _rotatedAttackSourceIp + "."));
    }

    private void StartConnectionInstability(List<StochasticSimulationEvent> events)
    {
        _connectionInstabilityTicksRemaining = _random.Next(3, 7);

        events.Add(CreateEvent(
            StochasticEventKind.ConnectionInstability,
            "Кратковременный провал связи",
            "Стохастическая модель временно снизила поток пакетов."));
    }

    private StochasticSimulationEvent CreateEvent(
        StochasticEventKind kind,
        string title,
        string description)
    {
        return new StochasticSimulationEvent(kind, title, description, DateTime.Now);
    }

    private int SamplePoisson(double lambda)
    {
        if (lambda <= 0)
        {
            return 0;
        }

        if (lambda > 30)
        {
            return Math.Max(0, (int)Math.Round(NextGaussian(lambda, Math.Sqrt(lambda))));
        }

        double limit = Math.Exp(-lambda);
        double product = 1.0;
        int count = 0;

        do
        {
            count++;
            product *= _random.NextDouble();
        }
        while (product > limit);

        return count - 1;
    }

    private double NextGaussian(double mean, double standardDeviation)
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return mean + standardDeviation * normal;
    }

    private static double GetDifficultyAttackMultiplier(SimulationDifficulty difficulty)
    {
        return difficulty switch
        {
            SimulationDifficulty.Easy => 0.9,
            SimulationDifficulty.Medium => 1.0,
            SimulationDifficulty.Hard => 1.15,
            _ => 1.0
        };
    }

    private static double GetAttackNoiseStdDev(SimulationDifficulty difficulty)
    {
        return difficulty switch
        {
            SimulationDifficulty.Easy => 0.08,
            SimulationDifficulty.Medium => 0.18,
            SimulationDifficulty.Hard => 0.32,
            _ => 0.18
        };
    }

    private static double GetBackgroundRate(SimulationDifficulty difficulty)
    {
        return difficulty switch
        {
            SimulationDifficulty.Easy => 3.0,
            SimulationDifficulty.Medium => 5.0,
            SimulationDifficulty.Hard => 8.0,
            _ => 5.0
        };
    }

    private static double CalculateDefenseLoadFactor(
        int baseIntensityPerSecond,
        int packetsPerSecond,
        SimulationDifficulty difficulty)
    {
        double expectedRate = Math.Max(1, baseIntensityPerSecond);
        double rawLoadFactor = packetsPerSecond / expectedRate;
        double difficultyPressure = difficulty switch
        {
            SimulationDifficulty.Easy => 0.9,
            SimulationDifficulty.Medium => 1.0,
            SimulationDifficulty.Hard => 1.12,
            _ => 1.0
        };

        return Math.Clamp(rawLoadFactor * difficultyPressure, 0.6, 2.2);
    }

    private string CreateRotatedAttackSourceIp()
    {
        int networkType = _random.Next(100);

        if (networkType < 35)
        {
            return "192.168.1." + _random.Next(2, 240);
        }

        if (networkType < 60)
        {
            return "10." + _random.Next(0, 255) + "." + _random.Next(0, 255) + "." + _random.Next(2, 240);
        }

        if (networkType < 80)
        {
            return "172.16." + _random.Next(0, 32) + "." + _random.Next(2, 240);
        }

        return "203.0.113." + _random.Next(2, 240);
    }
}
