using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Talon.Driver;

namespace Talon.Driver.Wpf;

public partial class MainWindow : Window
{
    private readonly CTalonObservableLog _trace = [];
    private CTalonDriver? _driver;
    private CTalonSimulatorTransport? _simulator;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        CommandGrid.ItemsSource = CTalonCommandCatalog.All;
        TraceGrid.ItemsSource = _trace;
        CommandGrid.SelectedItem = CTalonCommandCatalog.All.First();
        Loaded += async (_, _) => await ConnectSelectedMode();
        Closed += async (_, _) => await DisposeDriver();
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e) => await RunUiAction(ConnectSelectedMode);

    private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAction(async () =>
        {
            if (_driver is not null)
            {
                await _driver.Disconnect();
            }
            UpdateConnection();
        });
    }

    private async void PollButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAction(async () =>
        {
            var driver = RequireDriver();
            await driver.PollSafeStatus();
            UpdateStatus(driver.Status);
            FooterText.Text = "안전 Query Poll을 완료했습니다. 출력 상태는 변경하지 않았습니다.";
        });
    }

    private async void ValidationButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAction(async () =>
        {
            var results = await RequireDriver().RunReadOnlyValidation();
            ValidationGrid.ItemsSource = results;
            MainTabs.SelectedIndex = 1;
            var passed = results.Count(item => item.Passed);
            FooterText.Text = $"Read-only 검증 완료: {passed}/{results.Count} 항목 통과";
        });
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAction(async () =>
        {
            if (CommandGrid.SelectedItem is not ST_TALON_COMMAND_SPEC spec)
            {
                throw new InvalidOperationException("실행할 명령을 선택하세요.");
            }

            double? parameter = null;
            if (spec.RequiresParameter)
            {
                if (!double.TryParse(ParameterText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    throw new InvalidOperationException("Parameter를 숫자로 입력하세요. 소수점은 '.'을 사용합니다.");
                }
                parameter = parsed;
            }

            var response = await RequireDriver().Execute(spec.Command, parameter, ReadSafetyContext());
            UpdateStatus(RequireDriver().Status);
            FooterText.Text = $"{spec.DisplayName} 완료: {response}";
        });
    }

    private void InjectTimeoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_simulator is null)
        {
            FooterText.Text = "오류 주입은 Simulation 모드에서만 사용할 수 있습니다.";
            return;
        }

        _simulator.InjectTimeoutOnce = true;
        FooterText.Text = "다음 명령 한 번에 Timeout을 주입합니다.";
    }

    private void InjectInvalidButton_Click(object sender, RoutedEventArgs e)
    {
        if (_simulator is null)
        {
            FooterText.Text = "오류 주입은 Simulation 모드에서만 사용할 수 있습니다.";
            return;
        }

        _simulator.InjectInvalidResponseOnce = true;
        FooterText.Text = "다음 Query 한 번에 잘못된 응답을 주입합니다.";
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        var simulation = IsSimulationSelected();
        PortText.IsEnabled = !simulation;
        BaudCombo.IsEnabled = !simulation;
        TimeoutText.IsEnabled = !simulation;
        FooterText.Text = simulation
            ? "Simulation 모드 선택: 실제 장비 출력 없이 검증합니다. 연결 버튼을 누르세요."
            : "Hardware 모드 선택: Read-only Query부터 검증하고 출력 명령은 안전 잠금을 확인하세요.";
    }

    private void CommandGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CommandGrid.SelectedItem is not ST_TALON_COMMAND_SPEC spec)
        {
            return;
        }

        SelectedCommandText.Text = spec.RequiresParameter ? spec.SetTemplate : spec.QueryText + spec.SetTemplate;
        SelectedDescriptionText.Text = spec.Description;
        ParameterText.IsEnabled = spec.RequiresParameter;
        ParameterText.Text = spec.Minimum?.ToString(CultureInfo.InvariantCulture) ?? "0";
        ParameterUnitText.Text = spec.Unit;
        SendButton.Background = spec.RiskLevel switch
        {
            EN_TALON_RISK_LEVEL.ReadOnly => new SolidColorBrush(Color.FromRgb(21, 94, 117)),
            EN_TALON_RISK_LEVEL.Configuration => new SolidColorBrush(Color.FromRgb(120, 83, 20)),
            EN_TALON_RISK_LEVEL.LaserOutput => new SolidColorBrush(Color.FromRgb(159, 48, 71)),
            EN_TALON_RISK_LEVEL.Persistent => new SolidColorBrush(Color.FromRgb(126, 44, 144)),
            _ => new SolidColorBrush(Color.FromRgb(34, 49, 73))
        };
    }

    private async Task ConnectSelectedMode()
    {
        await DisposeDriver();

        if (IsSimulationSelected())
        {
            _simulator = new CTalonSimulatorTransport();
            _driver = new CTalonDriver(_simulator);
        }
        else
        {
            var baud = int.TryParse((BaudCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(), out var baudValue)
                ? baudValue
                : 115200;
            var timeout = int.TryParse(TimeoutText.Text, out var timeoutValue)
                ? Math.Clamp(timeoutValue, 100, 30000)
                : 1500;
            _driver = new CTalonDriver(new CSerialTalonTransport(PortText.Text.Trim(), baud, timeout));
        }

        _driver.TransactionCompleted += Driver_TransactionCompleted;
        await _driver.Connect();
        EndpointText.Text = _driver.Endpoint;
        UpdateConnection();
        FooterText.Text = _driver.IsSimulation
            ? "Simulation 연결 완료. 안전 Query Poll 또는 자동 검증을 실행할 수 있습니다."
            : "Hardware 연결 완료. 먼저 Read-only 자동 검증으로 통신을 확인하세요.";
    }

    private async Task DisposeDriver()
    {
        if (_driver is null)
        {
            return;
        }

        _driver.TransactionCompleted -= Driver_TransactionCompleted;
        await _driver.DisposeAsync();
        _driver = null;
        _simulator = null;
        UpdateConnection();
    }

    private void Driver_TransactionCompleted(object? sender, ST_TALON_TRANSACTION transaction)
    {
        Dispatcher.Invoke(() =>
        {
            _trace.AddBounded(transaction);
            UpdateConnection();
        });
    }

    private async Task RunUiAction(Func<Task> action)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        SetActionButtons(false);
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            FooterText.Text = $"오류: {ex.Message}";
            FooterText.Foreground = FindBrush("DangerBrush");
            UpdateConnection();
        }
        finally
        {
            _busy = false;
            SetActionButtons(true);
        }
    }

    private void UpdateStatus(ST_TALON_STATUS status)
    {
        SystemStatusText.Text = status.SystemStatus;
        SystemStatusText.Foreground = status.SystemStatus.Contains("READY", StringComparison.OrdinalIgnoreCase)
            ? FindBrush("SuccessBrush")
            : FindBrush("WarningBrush");
        PowerText.Text = $"{status.OutputPowerW:F3} W";
        CurrentText.Text = $"{status.DiodeCurrentA:F2} / {status.DiodeCurrentLimitA:F2} A";
        TemperatureText.Text = $"{status.DiodeTemperatureC:F1} / {status.TowerTemperatureC:F1} °C";
        IdentityText.Text = $"Identity: {status.Identity}";
        HistoryText.Text = $"History: {string.Join(" ; ", status.StatusHistory.Select(code => code.ToString("D3", CultureInfo.InvariantCulture)))}";

        var bits = status.StatusBits;
        EmissionBit.IsChecked = bits.Emission;
        ShutterBit.IsChecked = bits.ShutterOpen;
        GateBit.IsChecked = bits.GateOpen;
        ExternalGateBit.IsChecked = bits.ExternalGate;
        FaultBit.IsChecked = bits.SystemFault;
        MotorBit.IsChecked = bits.MotorMoving;
        ShgAutoBit.IsChecked = bits.ShgAutotune;
        ThgAutoBit.IsChecked = bits.ThgAutotune;
        StatusByteText.Text = $"RAW: {bits.RawValue}";
    }

    private void UpdateConnection()
    {
        var state = _driver?.ConnectionState ?? EN_TALON_CONNECTION_STATE.Offline;
        ConnectionText.Text = state.ToString().ToUpperInvariant();
        ConnectionLamp.Fill = state switch
        {
            EN_TALON_CONNECTION_STATE.Online or EN_TALON_CONNECTION_STATE.Simulation => FindBrush("SuccessBrush"),
            EN_TALON_CONNECTION_STATE.Connecting => FindBrush("WarningBrush"),
            EN_TALON_CONNECTION_STATE.Fault => FindBrush("DangerBrush"),
            _ => FindBrush("MutedBrush")
        };
    }

    private ST_TALON_SAFETY_CONTEXT ReadSafetyContext() => new(
        UnlockCheck.IsChecked == true,
        InterlockCheck.IsChecked == true,
        BeamPathCheck.IsChecked == true,
        OperatorCheck.IsChecked == true);

    private CTalonDriver RequireDriver() =>
        _driver ?? throw new InvalidOperationException("먼저 드라이버를 연결하세요.");

    private bool IsSimulationSelected() =>
        (ModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() != "Hardware";

    private Brush FindBrush(string key) => (Brush)FindResource(key);

    private void SetActionButtons(bool enabled)
    {
        ConnectButton.IsEnabled = enabled;
        DisconnectButton.IsEnabled = enabled;
        SendButton.IsEnabled = enabled;
    }
}
