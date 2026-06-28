using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;
using NetSecLab.App.Commands;
using NetSecLab.App.Models;
using NetSecLab.Core.Events;
using NetSecLab.Core.Interfaces;
using NetSecLab.Core.Models;
using NetSecLab.Core.Settings;

namespace NetSecLab.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IAttackService _attackService;
    private readonly IDefenseService _defenseService;
    private readonly IScenarioService _scenarioService;
    private readonly AppSettings _settings;
    private readonly List<IDisposable> _subscriptions = new();

    private AttackTypeOption _selectedAttackType;
    private ScenarioOption? _selectedScenario;
    private TrainingScenario? _activeScenario;
    private string _targetIp;
    private string _targetPortText;
    private string _intensityText;
    private string _rateLimitText;
    private string _blacklistIpText = "203.0.113.10";
    private string _whitelistIpText = "192.168.1.2";
    private bool _includeBackgroundTraffic = true;
    private string _statusText;
    private string _runStateText;
    private string _generatedPacketsText = "0";
    private string _allowedPacketsText = "0";
    private string _mitigatedPacketsText = "0";
    private string _blockedPacketsText = "0";
    private string _currentRateText = "0 пакетов/сек";
    private string _neutralizedPacketsText = "0";
    private string _defenseEfficiencyText = "0.0%";
    private string _blacklistSummaryText = "Пусто";
    private string _whitelistSummaryText = "Пусто";
    private string _defenseStatusText;
    private string _scenarioGoalText;
    private string _scenarioVerificationText;
    private string _scenarioStatusText;
    private string _scenarioScoreText = "Оценка: 0/100";
    private string _scenarioBreakdownText = "Реакция 0/15 • Выбор 0/35 • Эффективность 0/35 • Адаптивность 0/15";
    private string _scenarioReactionText = "Реакция: атака ещё не запущена.";
    private DateTime? _attackStartedAt;
    private DateTime? _firstCorrectDefenseEnabledAt;
    private bool _correctDefenseWasActiveAtAttackStart;
    private int _scenarioDefenseConfigurationChangesAfterAttack;
    private int _scenarioReceivedPacketsBaseline;
    private int _scenarioAllowedPacketsBaseline;
    private int _scenarioMitigatedPacketsBaseline;
    private int _scenarioBlockedPacketsBaseline;
    private int _receivedPackets;
    private int _allowedPackets;
    private int _mitigatedPackets;
    private int _blockedPackets;

    private const int MinPacketsPerSecond = 1;
    private const int MaxAttackPacketsPerSecond = 500;
    private const int MaxRateLimitPacketsPerSecond = 500;

    private static readonly IBrush EnabledAddButtonBrush = Brush.Parse("#16A34A");
    private static readonly IBrush EnabledRemoveButtonBrush = Brush.Parse("#DC2626");
    private static readonly IBrush EnabledClearButtonBrush = Brush.Parse("#D97706");
    private static readonly IBrush DisabledActionButtonBrush = Brush.Parse("#334155");

    public MainWindowViewModel(
        IAttackService attackService,
        IDefenseService defenseService,
        IScenarioService scenarioService,
        IEventBus eventBus,
        AppSettings settings)
    {
        _attackService = attackService;
        _defenseService = defenseService;
        _scenarioService = scenarioService;
        _settings = settings;
        _targetIp = settings.TargetIp;
        _targetPortText = settings.TargetPort.ToString();
        _intensityText = settings.DefaultIntensity.ToString();
        _rateLimitText = _defenseService.Settings.RateLimitPerSecond.ToString();
        _statusText = attackService.IsAvailable
            ? "Готово к запуску генератора атак."
            : "Генератор атак недоступен. Подключите функциональный модуль генерации трафика.";
        _runStateText = attackService.IsAvailable ? "Остановлено" : "Недоступно";
        UpdateIpListSummaryTexts();
        _defenseStatusText = CreateDefenseStatusText();
        _scenarioStatusText = scenarioService.IsAvailable
            ? "Сценарий не запущен. Выберите учебную задачу и нажмите \"Начать\"."
            : "Модуль сценариев недоступен.";
        _scenarioGoalText = scenarioService.IsAvailable
            ? "Выберите сценарий для проверки действий пользователя."
            : "Подключите модуль сценариев.";
        _scenarioVerificationText = "После запуска здесь будут показаны критерии проверки.";

        AttackTypes = new ObservableCollection<AttackTypeOption>
        {
            new(AttackType.SynFlood, "SYN-flood"),
            new(AttackType.UdpFlood, "UDP-flood"),
            new(AttackType.IcmpFlood, "ICMP-flood"),
            new(AttackType.HttpSlowloris, "HTTP Slowloris")
        };

        _selectedAttackType = AttackTypes[0];
        Scenarios = new ObservableCollection<ScenarioOption>(
            scenarioService.Scenarios.Select(scenario => new ScenarioOption(scenario)));
        _selectedScenario = Scenarios.Count > 0 ? Scenarios[0] : null;
        UpdateSelectedScenarioDetails();
        Packets = new ObservableCollection<PacketLogItem>();

        StartScenarioCommand = new RelayCommand(StartScenario, () => CanStartScenario);
        ResetScenarioCommand = new RelayCommand(ResetScenario, () => CanResetScenario);
        StartAttackCommand = new RelayCommand(StartAttack, () => AttackModuleAvailable && !_attackService.IsRunning);
        StopAttackCommand = new RelayCommand(StopAttack, () => AttackModuleAvailable && _attackService.IsRunning);
        ClearPacketsCommand = new RelayCommand(ClearPackets, () => AttackModuleAvailable && Packets.Count > 0);
        AddBlacklistIpCommand = new RelayCommand(AddBlacklistIp, () => CanAddBlacklistIp);
        RemoveBlacklistIpCommand = new RelayCommand(RemoveBlacklistIp, () => CanRemoveBlacklistIp);
        ClearBlacklistIpsCommand = new RelayCommand(ClearBlacklistIps, () => CanClearBlacklistIps);
        AddWhitelistIpCommand = new RelayCommand(AddWhitelistIp, () => CanAddWhitelistIp);
        RemoveWhitelistIpCommand = new RelayCommand(RemoveWhitelistIp, () => CanRemoveWhitelistIp);
        ClearWhitelistIpsCommand = new RelayCommand(ClearWhitelistIps, () => CanClearWhitelistIps);

        _subscriptions.Add(eventBus.Subscribe<PacketGeneratedEvent>(OnPacketGenerated));
        _subscriptions.Add(eventBus.Subscribe<AttackStartedEvent>(OnAttackStarted));
        _subscriptions.Add(eventBus.Subscribe<AttackStoppedEvent>(OnAttackStopped));
        _subscriptions.Add(eventBus.Subscribe<AttackStatisticsUpdatedEvent>(OnStatisticsUpdated));
    }

    public ObservableCollection<AttackTypeOption> AttackTypes { get; }
    public ObservableCollection<ScenarioOption> Scenarios { get; }
    public ObservableCollection<PacketLogItem> Packets { get; }
    public ICommand StartScenarioCommand { get; }
    public ICommand ResetScenarioCommand { get; }
    public ICommand StartAttackCommand { get; }
    public ICommand StopAttackCommand { get; }
    public ICommand ClearPacketsCommand { get; }
    public ICommand AddBlacklistIpCommand { get; }
    public ICommand RemoveBlacklistIpCommand { get; }
    public ICommand ClearBlacklistIpsCommand { get; }
    public ICommand AddWhitelistIpCommand { get; }
    public ICommand RemoveWhitelistIpCommand { get; }
    public ICommand ClearWhitelistIpsCommand { get; }

    public bool AttackModuleAvailable => _attackService.IsAvailable;
    public bool DefenseModuleAvailable => _defenseService.IsAvailable;
    public bool ScenarioModuleAvailable => _scenarioService.IsAvailable;
    public bool AttackConfigurationEnabled => AttackModuleAvailable && !_attackService.IsRunning;
    public bool ScenarioConfigurationEnabled => ScenarioModuleAvailable && !_attackService.IsRunning && _scenarioService.Status == ScenarioStatus.NotStarted;
    public bool CanStartScenario => ScenarioConfigurationEnabled && SelectedScenario is not null;
    public bool CanResetScenario => ScenarioModuleAvailable && _scenarioService.Status != ScenarioStatus.NotStarted;
    public bool DefenseConfigurationEnabled => DefenseModuleAvailable;
    public bool DefenseOptionsEnabled => DefenseConfigurationEnabled && ProtectionEnabled;
    public bool RateLimitInputEnabled => DefenseOptionsEnabled && RateLimitEnabled;
    public bool BlacklistControlsEnabled => DefenseOptionsEnabled && BlacklistEnabled;
    public bool WhitelistControlsEnabled => DefenseOptionsEnabled && WhitelistEnabled;
    public bool CanAddBlacklistIp => BlacklistControlsEnabled && CanAddIpToList(BlacklistIpText, _defenseService.Settings.BlacklistedIps);
    public bool CanRemoveBlacklistIp => BlacklistControlsEnabled && ContainsIpInList(BlacklistIpText, _defenseService.Settings.BlacklistedIps);
    public bool CanClearBlacklistIps => BlacklistControlsEnabled && _defenseService.Settings.BlacklistedIps.Count > 0;
    public bool CanAddWhitelistIp => WhitelistControlsEnabled && CanAddIpToList(WhitelistIpText, _defenseService.Settings.WhitelistedIps);
    public bool CanRemoveWhitelistIp => WhitelistControlsEnabled && ContainsIpInList(WhitelistIpText, _defenseService.Settings.WhitelistedIps);
    public bool CanClearWhitelistIps => WhitelistControlsEnabled && _defenseService.Settings.WhitelistedIps.Count > 0;
    public IBrush BlacklistAddButtonBackground => CanAddBlacklistIp ? EnabledAddButtonBrush : DisabledActionButtonBrush;
    public IBrush BlacklistRemoveButtonBackground => CanRemoveBlacklistIp ? EnabledRemoveButtonBrush : DisabledActionButtonBrush;
    public IBrush BlacklistClearButtonBackground => CanClearBlacklistIps ? EnabledClearButtonBrush : DisabledActionButtonBrush;
    public IBrush WhitelistAddButtonBackground => CanAddWhitelistIp ? EnabledAddButtonBrush : DisabledActionButtonBrush;
    public IBrush WhitelistRemoveButtonBackground => CanRemoveWhitelistIp ? EnabledRemoveButtonBrush : DisabledActionButtonBrush;
    public IBrush WhitelistClearButtonBackground => CanClearWhitelistIps ? EnabledClearButtonBrush : DisabledActionButtonBrush;

    public AttackTypeOption SelectedAttackType
    {
        get => _selectedAttackType;
        set
        {
            if (SetProperty(ref _selectedAttackType, value))
            {
                UpdateScenarioState();
            }
        }
    }

    public ScenarioOption? SelectedScenario
    {
        get => _selectedScenario;
        set
        {
            if (SetProperty(ref _selectedScenario, value))
            {
                UpdateSelectedScenarioDetails();
                UpdateCommandStates();
            }
        }
    }

    public string TargetIp
    {
        get => _targetIp;
        set => SetProperty(ref _targetIp, value);
    }

    public string TargetPortText
    {
        get => _targetPortText;
        set => SetProperty(ref _targetPortText, value);
    }

    public string IntensityText
    {
        get => _intensityText;
        set => SetProperty(ref _intensityText, value);
    }

    public bool IncludeBackgroundTraffic
    {
        get => _includeBackgroundTraffic;
        set => SetProperty(ref _includeBackgroundTraffic, value);
    }

    public bool ProtectionEnabled
    {
        get => _defenseService.Settings.IsEnabled;
        set
        {
            if (_defenseService.Settings.IsEnabled == value)
            {
                return;
            }

            _defenseService.Settings.IsEnabled = value;
            RecordScenarioDefenseChange();
            OnPropertyChanged();
            OnPropertyChanged(nameof(DefenseOptionsEnabled));
            OnPropertyChanged(nameof(RateLimitInputEnabled));
            OnPropertyChanged(nameof(BlacklistControlsEnabled));
            OnPropertyChanged(nameof(WhitelistControlsEnabled));
            UpdateDefenseStatusText();
            UpdateScenarioState();
            UpdateCommandStates();
        }
    }

    public bool SynCookiesEnabled
    {
        get => _defenseService.Settings.SynCookiesEnabled;
        set
        {
            if (_defenseService.Settings.SynCookiesEnabled == value)
            {
                return;
            }

            _defenseService.Settings.SynCookiesEnabled = value;
            RecordScenarioDefenseChange();
            OnPropertyChanged();
            UpdateDefenseStatusText();
            UpdateScenarioState();
        }
    }

    public bool RateLimitEnabled
    {
        get => _defenseService.Settings.RateLimitEnabled;
        set
        {
            if (_defenseService.Settings.RateLimitEnabled == value)
            {
                return;
            }

            _defenseService.Settings.RateLimitEnabled = value;
            RecordScenarioDefenseChange();
            OnPropertyChanged();
            OnPropertyChanged(nameof(RateLimitInputEnabled));
            UpdateDefenseStatusText();
            UpdateScenarioState();
        }
    }

    public bool BehaviorFilterEnabled
    {
        get => _defenseService.Settings.BehaviorFilterEnabled;
        set
        {
            if (_defenseService.Settings.BehaviorFilterEnabled == value)
            {
                return;
            }

            _defenseService.Settings.BehaviorFilterEnabled = value;
            RecordScenarioDefenseChange();
            OnPropertyChanged();
            UpdateDefenseStatusText();
            UpdateScenarioState();
        }
    }

    public bool BlacklistEnabled
    {
        get => _defenseService.Settings.BlacklistEnabled;
        set
        {
            if (_defenseService.Settings.BlacklistEnabled == value)
            {
                return;
            }

            _defenseService.Settings.BlacklistEnabled = value;
            RecordScenarioDefenseChange();
            OnPropertyChanged();
            OnPropertyChanged(nameof(BlacklistControlsEnabled));
            UpdateDefenseStatusText();
            UpdateScenarioState();
            UpdateCommandStates();
        }
    }

    public bool WhitelistEnabled
    {
        get => _defenseService.Settings.WhitelistEnabled;
        set
        {
            if (_defenseService.Settings.WhitelistEnabled == value)
            {
                return;
            }

            _defenseService.Settings.WhitelistEnabled = value;
            RecordScenarioDefenseChange();
            OnPropertyChanged();
            OnPropertyChanged(nameof(WhitelistControlsEnabled));
            UpdateDefenseStatusText();
            UpdateScenarioState();
            UpdateCommandStates();
        }
    }

    public string BlacklistIpText
    {
        get => _blacklistIpText;
        set
        {
            if (SetProperty(ref _blacklistIpText, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string WhitelistIpText
    {
        get => _whitelistIpText;
        set
        {
            if (SetProperty(ref _whitelistIpText, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string BlacklistSummaryText
    {
        get => _blacklistSummaryText;
        private set => SetProperty(ref _blacklistSummaryText, value);
    }

    public string WhitelistSummaryText
    {
        get => _whitelistSummaryText;
        private set => SetProperty(ref _whitelistSummaryText, value);
    }

    public string ScenarioGoalText
    {
        get => _scenarioGoalText;
        private set => SetProperty(ref _scenarioGoalText, value);
    }

    public string ScenarioVerificationText
    {
        get => _scenarioVerificationText;
        private set => SetProperty(ref _scenarioVerificationText, value);
    }

    public string ScenarioStatusText
    {
        get => _scenarioStatusText;
        private set => SetProperty(ref _scenarioStatusText, value);
    }

    public string ScenarioScoreText
    {
        get => _scenarioScoreText;
        private set => SetProperty(ref _scenarioScoreText, value);
    }

    public string ScenarioBreakdownText
    {
        get => _scenarioBreakdownText;
        private set => SetProperty(ref _scenarioBreakdownText, value);
    }

    public string ScenarioReactionText
    {
        get => _scenarioReactionText;
        private set => SetProperty(ref _scenarioReactionText, value);
    }

    public string RateLimitText
    {
        get => _rateLimitText;
        set
        {
            if (!SetProperty(ref _rateLimitText, value))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!int.TryParse(value, out int limit))
            {
                StatusText = "Лимит защиты должен быть числом.";
                return;
            }

            if (limit < MinPacketsPerSecond || limit > MaxRateLimitPacketsPerSecond)
            {
                StatusText = "Лимит защиты должен быть от 1 до " + MaxRateLimitPacketsPerSecond + " пакетов/сек.";
                return;
            }

            _defenseService.Settings.RateLimitPerSecond = limit;
            UpdateDefenseStatusText();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string DefenseStatusText
    {
        get => _defenseStatusText;
        private set => SetProperty(ref _defenseStatusText, value);
    }

    public string RunStateText
    {
        get => _runStateText;
        private set => SetProperty(ref _runStateText, value);
    }

    public string GeneratedPacketsText
    {
        get => _generatedPacketsText;
        private set => SetProperty(ref _generatedPacketsText, value);
    }

    public string AllowedPacketsText
    {
        get => _allowedPacketsText;
        private set => SetProperty(ref _allowedPacketsText, value);
    }

    public string MitigatedPacketsText
    {
        get => _mitigatedPacketsText;
        private set => SetProperty(ref _mitigatedPacketsText, value);
    }

    public string BlockedPacketsText
    {
        get => _blockedPacketsText;
        private set => SetProperty(ref _blockedPacketsText, value);
    }

    public string NeutralizedPacketsText
    {
        get => _neutralizedPacketsText;
        private set => SetProperty(ref _neutralizedPacketsText, value);
    }

    public string DefenseEfficiencyText
    {
        get => _defenseEfficiencyText;
        private set => SetProperty(ref _defenseEfficiencyText, value);
    }

    public string CurrentRateText
    {
        get => _currentRateText;
        private set => SetProperty(ref _currentRateText, value);
    }

    private void StartScenario()
    {
        if (!ScenarioModuleAvailable || SelectedScenario is null)
        {
            StatusText = "Модуль сценариев недоступен или сценарий не выбран.";
            return;
        }

        Packets.Clear();
        ResetPacketCounters();
        _defenseService.Reset();

        TrainingScenario scenario = _scenarioService.Start(SelectedScenario.Id);
        _activeScenario = scenario;
        ResetScenarioAttemptState();
        SetScenarioPacketBaselines();

        AttackTypeOption? attackType = AttackTypes.FirstOrDefault(option => option.Value == scenario.AttackType);
        if (attackType is not null)
        {
            SelectedAttackType = attackType;
        }

        ScenarioGoalText = scenario.GoalText;
        ScenarioVerificationText = scenario.VerificationText;
        ScenarioStatusText = "Сценарий запущен. Выполните условия и запустите генерацию атаки.";
        ScenarioScoreText = "Оценка: 0/100";
        ScenarioBreakdownText = "Реакция 0/15 • Выбор 0/35 • Эффективность 0/35 • Адаптивность 0/15";
        ScenarioReactionText = "Реакция: атака ещё не запущена.";
        StatusText = "Учебный сценарий запущен: " + scenario.Title;
        UpdateScenarioState();
        UpdateCommandStates();
    }

    private void ResetScenario()
    {
        _scenarioService.Reset();
        _activeScenario = null;
        ResetScenarioAttemptState();
        SetScenarioPacketBaselines();
        ScenarioStatusText = "Сценарий сброшен.";
        ScenarioScoreText = "Оценка: 0/100";
        ScenarioBreakdownText = "Реакция 0/15 • Выбор 0/35 • Эффективность 0/35 • Адаптивность 0/15";
        ScenarioReactionText = "Реакция: атака ещё не запущена.";
        UpdateSelectedScenarioDetails();
        UpdateCommandStates();
    }

    private void StartAttack()
    {
        if (!AttackModuleAvailable)
        {
            StatusText = "Генератор атак недоступен. Функциональный модуль генерации трафика не подключён.";
            return;
        }

        string targetIp = string.IsNullOrWhiteSpace(TargetIp)
            ? _settings.TargetIp
            : TargetIp.Trim();

        if (!IsAllowedSimulationTargetIp(targetIp))
        {
            StatusText = "Для локальной симуляции используйте IPv4 из диапазонов: 127.x.x.x, 10.x.x.x, 172.16-31.x.x или 192.168.x.x.";
            return;
        }

        if (!TryReadTargetPort(out int targetPort))
        {
            return;
        }

        if (!TryReadIntensity(out int intensity))
        {
            return;
        }

        if (ProtectionEnabled && RateLimitEnabled)
        {
            if (!TryReadRateLimit(out int rateLimit))
            {
                return;
            }

            _defenseService.Settings.RateLimitPerSecond = rateLimit;
        }

        Packets.Clear();
        ResetPacketCounters();
        _defenseService.Reset();
        UpdateScenarioState();

        AttackRunOptions options = new()
        {
            AttackType = SelectedAttackType.Value,
            TargetIp = targetIp,
            TargetPort = targetPort,
            IntensityPerSecond = intensity,
            IncludeBackgroundTraffic = IncludeBackgroundTraffic
        };

        _attackService.Start(options);
        UpdateCommandStates();
    }

    private void AddBlacklistIp()
    {
        if (!TryNormalizeAccessListIp(BlacklistIpText, out string ip))
        {
            StatusText = CreateAccessListIpErrorText();
            return;
        }

        _defenseService.Settings.BlacklistedIps.Add(ip);
        _defenseService.Settings.WhitelistedIps.Remove(ip);
        RecordScenarioDefenseChange();
        BlacklistIpText = ip;
        UpdateIpListSummaryTexts();
        UpdateDefenseStatusText();
        StatusText = "IP " + ip + " добавлен в чёрный список.";
        UpdateCommandStates();
    }

    private void RemoveBlacklistIp()
    {
        if (!TryNormalizeAccessListIp(BlacklistIpText, out string ip))
        {
            StatusText = CreateAccessListIpErrorText();
            return;
        }

        _defenseService.Settings.BlacklistedIps.Remove(ip);
        RecordScenarioDefenseChange();
        BlacklistIpText = ip;
        UpdateIpListSummaryTexts();
        UpdateDefenseStatusText();
        StatusText = "IP " + ip + " удалён из чёрного списка.";
        UpdateCommandStates();
    }

    private void ClearBlacklistIps()
    {
        _defenseService.Settings.BlacklistedIps.Clear();
        RecordScenarioDefenseChange();
        UpdateIpListSummaryTexts();
        UpdateDefenseStatusText();
        StatusText = "Чёрный список очищен.";
        UpdateCommandStates();
    }

    private void AddWhitelistIp()
    {
        if (!TryNormalizeAccessListIp(WhitelistIpText, out string ip))
        {
            StatusText = CreateAccessListIpErrorText();
            return;
        }

        _defenseService.Settings.WhitelistedIps.Add(ip);
        _defenseService.Settings.BlacklistedIps.Remove(ip);
        RecordScenarioDefenseChange();
        WhitelistIpText = ip;
        UpdateIpListSummaryTexts();
        UpdateDefenseStatusText();
        StatusText = "IP " + ip + " добавлен в белый список.";
        UpdateCommandStates();
    }

    private void RemoveWhitelistIp()
    {
        if (!TryNormalizeAccessListIp(WhitelistIpText, out string ip))
        {
            StatusText = CreateAccessListIpErrorText();
            return;
        }

        _defenseService.Settings.WhitelistedIps.Remove(ip);
        RecordScenarioDefenseChange();
        WhitelistIpText = ip;
        UpdateIpListSummaryTexts();
        UpdateDefenseStatusText();
        StatusText = "IP " + ip + " удалён из белого списка.";
        UpdateCommandStates();
    }

    private void ClearWhitelistIps()
    {
        _defenseService.Settings.WhitelistedIps.Clear();
        RecordScenarioDefenseChange();
        UpdateIpListSummaryTexts();
        UpdateDefenseStatusText();
        StatusText = "Белый список очищен.";
        UpdateCommandStates();
    }

    private bool TryReadTargetPort(out int targetPort)
    {
        if (!int.TryParse(TargetPortText, out targetPort))
        {
            StatusText = "Целевой порт должен быть числом.";
            return false;
        }

        if (targetPort < 1 || targetPort > 65535)
        {
            StatusText = "Целевой порт должен быть в диапазоне от 1 до 65535.";
            return false;
        }

        return true;
    }

    private bool TryReadIntensity(out int intensity)
    {
        if (!int.TryParse(IntensityText, out intensity))
        {
            StatusText = "Интенсивность должна быть числом.";
            return false;
        }

        if (intensity < MinPacketsPerSecond || intensity > MaxAttackPacketsPerSecond)
        {
            StatusText = "Интенсивность должна быть от 1 до " + MaxAttackPacketsPerSecond + " пакетов/сек.";
            return false;
        }

        return true;
    }

    private bool TryReadRateLimit(out int rateLimit)
    {
        if (!int.TryParse(RateLimitText, out rateLimit))
        {
            StatusText = "Лимит защиты должен быть числом.";
            return false;
        }

        if (rateLimit < MinPacketsPerSecond || rateLimit > MaxRateLimitPacketsPerSecond)
        {
            StatusText = "Лимит защиты должен быть от 1 до " + MaxRateLimitPacketsPerSecond + " пакетов/сек.";
            return false;
        }

        return true;
    }

    private static bool IsAllowedSimulationTargetIp(string value)
    {
        if (!IPAddress.TryParse(value, out IPAddress? address))
        {
            return false;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        byte[] bytes = address.GetAddressBytes();

        return bytes[0] == 10
            || bytes[0] == 127
            || bytes[0] == 192 && bytes[1] == 168
            || bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31;
    }

    private static bool TryNormalizeAccessListIp(string value, out string normalizedIp)
    {
        normalizedIp = string.Empty;

        if (!IPAddress.TryParse(value?.Trim(), out IPAddress? address) ||
            address.AddressFamily != AddressFamily.InterNetwork ||
            !IsAllowedAccessListIp(address))
        {
            return false;
        }

        normalizedIp = address.ToString();
        return true;
    }

    private static bool CanAddIpToList(string value, HashSet<string> ips)
    {
        return TryNormalizeAccessListIp(value, out string ip) && !ips.Contains(ip);
    }

    private static bool ContainsIpInList(string value, HashSet<string> ips)
    {
        return TryNormalizeAccessListIp(value, out string ip) && ips.Contains(ip);
    }

    private static bool IsAllowedAccessListIp(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        bool lastOctetCanBeGenerated = bytes[3] >= 2 && bytes[3] <= 239;

        return lastOctetCanBeGenerated &&
               (bytes[0] == 10
                || bytes[0] == 192 && bytes[1] == 168 && bytes[2] == 1
                || bytes[0] == 172 && bytes[1] == 16 && bytes[2] <= 31
                || bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
    }

    private static string CreateAccessListIpErrorText()
    {
        return "Для списков доступа используйте IPv4, которые могут быть источниками пакетов в симуляции: 192.168.1.2-239, 10.x.x.2-239, 172.16.0-31.2-239 или 203.0.113.2-239.";
    }

    private void StopAttack()
    {
        _attackService.Stop();
        UpdateCommandStates();
    }

    private void ClearPackets()
    {
        Packets.Clear();
        ResetPacketCounters();
        _defenseService.Reset();
        StatusText = "Журнал пакетов очищен.";
        UpdateScenarioState();
        UpdateCommandStates();
    }

    private void OnPacketGenerated(PacketGeneratedEvent eventData)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PacketInspectionResult inspection = _defenseService.Inspect(eventData.Packet);

            Packets.Insert(0, new PacketLogItem(inspection));
            _receivedPackets++;

            switch (inspection.Decision)
            {
                case PacketDecision.Blocked:
                    _blockedPackets++;
                    break;

                case PacketDecision.Mitigated:
                    _mitigatedPackets++;
                    break;

                default:
                    _allowedPackets++;
                    break;
            }

            UpdatePacketCounterTexts();
            UpdateScenarioState();

            while (Packets.Count > _settings.MaxPacketsInUi)
            {
                Packets.RemoveAt(Packets.Count - 1);
            }

            UpdateCommandStates();
        });
    }

    private void UpdatePacketCounterTexts()
    {
        int neutralizedPackets = _mitigatedPackets + _blockedPackets;

        GeneratedPacketsText = _receivedPackets.ToString();
        AllowedPacketsText = _allowedPackets.ToString();
        MitigatedPacketsText = _mitigatedPackets.ToString();
        BlockedPacketsText = _blockedPackets.ToString();
        NeutralizedPacketsText = neutralizedPackets.ToString();

        if (_receivedPackets == 0)
        {
            DefenseEfficiencyText = "0.0%";
            return;
        }

        double efficiency = neutralizedPackets * 100.0 / _receivedPackets;
        DefenseEfficiencyText = efficiency.ToString("0.0") + "%";
    }

    private void OnAttackStarted(AttackStartedEvent eventData)
    {
        Dispatcher.UIThread.Post(() =>
        {
            string targetText = eventData.Options.AttackType == AttackType.IcmpFlood
                ? eventData.Options.TargetIp
                : eventData.Options.TargetIp + ":" + eventData.Options.TargetPort;

            if (_scenarioService.Status == ScenarioStatus.InProgress && _attackStartedAt is null)
            {
                _attackStartedAt = DateTime.Now;
                _correctDefenseWasActiveAtAttackStart = IsScenarioDefenseReady();
            }

            RunStateText = "Атака выполняется";
            StatusText =
                "Запущен генератор: " + eventData.Options.AttackType +
                ". Цель: " + targetText +
                ". Для изменения параметров остановите генерацию и запустите её снова.";
            UpdateScenarioState();
            UpdateCommandStates();
        });
    }

    private void OnAttackStopped(AttackStoppedEvent eventData)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RunStateText = AttackModuleAvailable ? "Остановлено" : "Недоступно";
            CurrentRateText = "0 пакетов/сек";
            StatusText = AttackModuleAvailable
                ? "Генератор атак остановлен."
                : "Генератор атак недоступен.";
            UpdateCommandStates();
        });
    }

    private void OnStatisticsUpdated(AttackStatisticsUpdatedEvent eventData)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CurrentRateText = eventData.PacketsPerSecond + " пакетов/сек";
        });
    }

    private void ResetPacketCounters()
    {
        _receivedPackets = 0;
        _allowedPackets = 0;
        _mitigatedPackets = 0;
        _blockedPackets = 0;

        UpdatePacketCounterTexts();
    }

    private void UpdateSelectedScenarioDetails()
    {
        if (!ScenarioModuleAvailable)
        {
            ScenarioGoalText = "Модуль сценариев недоступен.";
            ScenarioVerificationText = "Подключите модуль сценариев для оценки действий пользователя.";
            ScenarioStatusText = "Модуль сценариев недоступен.";
            return;
        }

        if (_scenarioService.Status != ScenarioStatus.NotStarted)
        {
            return;
        }

        if (SelectedScenario is null)
        {
            ScenarioGoalText = "Сценарий не выбран.";
            ScenarioVerificationText = "Выберите сценарий из списка.";
            ScenarioStatusText = "Сценарий не выбран.";
            return;
        }

        ScenarioGoalText = SelectedScenario.GoalText;
        ScenarioVerificationText = SelectedScenario.VerificationText;
        ScenarioStatusText = "Сценарий не запущен. Нажмите \"Начать\", чтобы включить проверку.";
        ScenarioScoreText = "Оценка: 0/100";
        ScenarioBreakdownText = "Реакция 0/15 • Выбор 0/35 • Эффективность 0/35 • Адаптивность 0/15";
        ScenarioReactionText = "Реакция: атака ещё не запущена.";
    }

    private void UpdateScenarioState()
    {
        if (!ScenarioModuleAvailable || SelectedScenario is null)
        {
            return;
        }

        TryRecordCorrectDefenseTime();

        ScenarioEvaluationResult result = _scenarioService.Evaluate(CreateScenarioEvaluationInput());

        if (_scenarioService.Status == ScenarioStatus.NotStarted)
        {
            UpdateSelectedScenarioDetails();
            return;
        }

        ScenarioStatusText = result.StatusText;
        ScenarioScoreText = "Оценка: " + result.Score + "/100";
        ScenarioBreakdownText = result.ScoreBreakdownText;
        ScenarioReactionText = result.ReactionTimeText;
    }

    private ScenarioEvaluationInput CreateScenarioEvaluationInput()
    {
        return new ScenarioEvaluationInput
        {
            AttackType = SelectedAttackType.Value,
            ReceivedPackets = Math.Max(0, _receivedPackets - _scenarioReceivedPacketsBaseline),
            AllowedPackets = Math.Max(0, _allowedPackets - _scenarioAllowedPacketsBaseline),
            MitigatedPackets = Math.Max(0, _mitigatedPackets - _scenarioMitigatedPacketsBaseline),
            BlockedPackets = Math.Max(0, _blockedPackets - _scenarioBlockedPacketsBaseline),
            ProtectionEnabled = ProtectionEnabled,
            SynCookiesEnabled = SynCookiesEnabled,
            RateLimitEnabled = RateLimitEnabled,
            BehaviorFilterEnabled = BehaviorFilterEnabled,
            BlacklistEnabled = BlacklistEnabled,
            WhitelistEnabled = WhitelistEnabled,
            AttackStartedAt = _attackStartedAt,
            FirstCorrectDefenseEnabledAt = _firstCorrectDefenseEnabledAt,
            BlacklistedIpCount = _defenseService.Settings.BlacklistedIps.Count,
            WhitelistedIpCount = _defenseService.Settings.WhitelistedIps.Count,
            EnabledDefenseMechanismCount = CountEnabledDefenseMechanisms(),
            CorrectDefenseWasEnabledBeforeAttack = _correctDefenseWasActiveAtAttackStart,
            DefenseConfigurationChangesAfterAttack = _scenarioDefenseConfigurationChangesAfterAttack
        };
    }

    private void ResetScenarioAttemptState()
    {
        _attackStartedAt = null;
        _firstCorrectDefenseEnabledAt = null;
        _correctDefenseWasActiveAtAttackStart = false;
        _scenarioDefenseConfigurationChangesAfterAttack = 0;
    }

    private void SetScenarioPacketBaselines()
    {
        _scenarioReceivedPacketsBaseline = _receivedPackets;
        _scenarioAllowedPacketsBaseline = _allowedPackets;
        _scenarioMitigatedPacketsBaseline = _mitigatedPackets;
        _scenarioBlockedPacketsBaseline = _blockedPackets;
    }

    private void TryRecordCorrectDefenseTime()
    {
        if (_activeScenario is null ||
            _scenarioService.Status != ScenarioStatus.InProgress ||
            _attackStartedAt is null ||
            _firstCorrectDefenseEnabledAt is not null ||
            _correctDefenseWasActiveAtAttackStart ||
            !IsScenarioDefenseReady())
        {
            return;
        }

        _firstCorrectDefenseEnabledAt = DateTime.Now;
    }

    private void RecordScenarioDefenseChange()
    {
        if (_scenarioService.Status != ScenarioStatus.InProgress)
        {
            return;
        }

        if (_attackStartedAt is not null)
        {
            _scenarioDefenseConfigurationChangesAfterAttack++;
        }

        TryRecordCorrectDefenseTime();
    }

    private bool IsScenarioDefenseReady()
    {
        if (_activeScenario is null || !ProtectionEnabled)
        {
            return false;
        }

        if (!_activeScenario.RequiredDefenses.All(IsDefenseActive))
        {
            return false;
        }

        if (_activeScenario.RequiresBlacklistEntry && _defenseService.Settings.BlacklistedIps.Count == 0)
        {
            return false;
        }

        if (_activeScenario.RequiresWhitelistEntry && _defenseService.Settings.WhitelistedIps.Count == 0)
        {
            return false;
        }

        return true;
    }

    private bool IsDefenseActive(ScenarioDefenseKind requiredDefense)
    {
        return requiredDefense switch
        {
            ScenarioDefenseKind.SynCookies => SynCookiesEnabled,
            ScenarioDefenseKind.RateLimit => RateLimitEnabled,
            ScenarioDefenseKind.BehaviorFilter => BehaviorFilterEnabled,
            ScenarioDefenseKind.Blacklist => BlacklistEnabled,
            ScenarioDefenseKind.Whitelist => WhitelistEnabled,
            _ => false
        };
    }

    private int CountEnabledDefenseMechanisms()
    {
        int count = 0;
        if (SynCookiesEnabled) count++;
        if (RateLimitEnabled) count++;
        if (BehaviorFilterEnabled) count++;
        if (BlacklistEnabled) count++;
        if (WhitelistEnabled) count++;
        return count;
    }

    private void UpdateDefenseStatusText()
    {
        DefenseStatusText = CreateDefenseStatusText();
    }

    private string CreateDefenseStatusText()
    {
        if (!DefenseModuleAvailable)
        {
            return "Модуль защиты не подключён.";
        }

        if (!ProtectionEnabled)
        {
            return "Защитные механизмы отключены.";
        }

        return "Активны: " +
               "SYN cookies — " + OnOff(SynCookiesEnabled) + ", " +
               "rate limiting — " + OnOff(RateLimitEnabled) + ", " +
               "blacklist — " + OnOff(BlacklistEnabled) + " (" + _defenseService.Settings.BlacklistedIps.Count + "), " +
               "whitelist — " + OnOff(WhitelistEnabled) + " (" + _defenseService.Settings.WhitelistedIps.Count + "), " +
               "поведенческий фильтр — " + OnOff(BehaviorFilterEnabled) + ".";
    }

    private void UpdateIpListSummaryTexts()
    {
        BlacklistSummaryText = CreateIpListSummary(_defenseService.Settings.BlacklistedIps);
        WhitelistSummaryText = CreateIpListSummary(_defenseService.Settings.WhitelistedIps);
    }

    private static string CreateIpListSummary(IEnumerable<string> ips)
    {
        string[] values = ips.OrderBy(ip => ip).ToArray();

        return values.Length == 0
            ? "Пусто"
            : string.Join(", ", values);
    }

    private void UpdateCommandStates()
    {
        OnPropertyChanged(nameof(AttackConfigurationEnabled));
        OnPropertyChanged(nameof(ScenarioConfigurationEnabled));
        OnPropertyChanged(nameof(CanStartScenario));
        OnPropertyChanged(nameof(CanResetScenario));
        OnPropertyChanged(nameof(DefenseConfigurationEnabled));
        OnPropertyChanged(nameof(DefenseOptionsEnabled));
        OnPropertyChanged(nameof(RateLimitInputEnabled));
        OnPropertyChanged(nameof(BlacklistControlsEnabled));
        OnPropertyChanged(nameof(WhitelistControlsEnabled));
        OnPropertyChanged(nameof(CanAddBlacklistIp));
        OnPropertyChanged(nameof(CanRemoveBlacklistIp));
        OnPropertyChanged(nameof(CanClearBlacklistIps));
        OnPropertyChanged(nameof(CanAddWhitelistIp));
        OnPropertyChanged(nameof(CanRemoveWhitelistIp));
        OnPropertyChanged(nameof(CanClearWhitelistIps));
        OnPropertyChanged(nameof(BlacklistAddButtonBackground));
        OnPropertyChanged(nameof(BlacklistRemoveButtonBackground));
        OnPropertyChanged(nameof(BlacklistClearButtonBackground));
        OnPropertyChanged(nameof(WhitelistAddButtonBackground));
        OnPropertyChanged(nameof(WhitelistRemoveButtonBackground));
        OnPropertyChanged(nameof(WhitelistClearButtonBackground));

        if (StartScenarioCommand is RelayCommand startScenarioCommand)
        {
            startScenarioCommand.RaiseCanExecuteChanged();
        }

        if (ResetScenarioCommand is RelayCommand resetScenarioCommand)
        {
            resetScenarioCommand.RaiseCanExecuteChanged();
        }

        if (StartAttackCommand is RelayCommand startCommand)
        {
            startCommand.RaiseCanExecuteChanged();
        }

        if (StopAttackCommand is RelayCommand stopCommand)
        {
            stopCommand.RaiseCanExecuteChanged();
        }

        if (ClearPacketsCommand is RelayCommand clearCommand)
        {
            clearCommand.RaiseCanExecuteChanged();
        }

        if (AddBlacklistIpCommand is RelayCommand addBlacklistCommand)
        {
            addBlacklistCommand.RaiseCanExecuteChanged();
        }

        if (RemoveBlacklistIpCommand is RelayCommand removeBlacklistCommand)
        {
            removeBlacklistCommand.RaiseCanExecuteChanged();
        }

        if (ClearBlacklistIpsCommand is RelayCommand clearBlacklistCommand)
        {
            clearBlacklistCommand.RaiseCanExecuteChanged();
        }

        if (AddWhitelistIpCommand is RelayCommand addWhitelistCommand)
        {
            addWhitelistCommand.RaiseCanExecuteChanged();
        }

        if (RemoveWhitelistIpCommand is RelayCommand removeWhitelistCommand)
        {
            removeWhitelistCommand.RaiseCanExecuteChanged();
        }

        if (ClearWhitelistIpsCommand is RelayCommand clearWhitelistCommand)
        {
            clearWhitelistCommand.RaiseCanExecuteChanged();
        }
    }

    private static string OnOff(bool value)
    {
        return value ? "ВКЛ" : "ВЫКЛ";
    }

    public void Dispose()
    {
        _attackService.Stop();

        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }
}
