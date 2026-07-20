using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Equipment.Driver;

namespace Talon.Driver.Wpf;

public partial class MainWindow : Window
{
    private CEquipmentDriver? _driver;
    private CSimulatorEquipmentTransport? _simulator;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        ComponentCombo.ItemsSource = CEquipmentCatalog.AllProfiles;
        ComponentCombo.SelectedIndex = 0;
        Loaded += async (_, _) => await ConnectSelectedMode();
        Closed += async (_, _) => await DisposeDriver();
    }

    private ST_EQUIPMENT_PROFILE SelectedProfile => (ST_EQUIPMENT_PROFILE)ComponentCombo.SelectedItem;
    private bool IsSimulation => (ModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() != "Hardware";

    private async void ComponentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComponentCombo.SelectedItem is not ST_EQUIPMENT_PROFILE profile) return;
        CommandGrid.ItemsSource = CEquipmentCatalog.GetCommands(profile.Type);
        CommandGrid.SelectedIndex = 0;
        EndpointTextBox.Text = profile.DefaultEndpoint;
        BaudTextBox.Text = profile.DefaultBaudRate == 0 ? "-" : profile.DefaultBaudRate.ToString(CultureInfo.InvariantCulture);
        TcpPortTextBox.Text = profile.DefaultTcpPort == 0 ? "-" : profile.DefaultTcpPort.ToString(CultureInfo.InvariantCulture);
        RoleText.Text = profile.Role;
        ProtocolText.Text = $"{profile.Transport} · TX terminator: {ShowTerminator(profile.TxTerminator)}";
        GuideTitleText.Text = profile.DisplayName;
        ManualText.Text = $"분석 기준 매뉴얼: {profile.Manual}";
        TheoryText.Text = profile.Theory;
        OperationText.Text = profile.Operation;
        NotesText.Text = profile.ImportantNotes;
        LimitText.Text = profile.VerificationLimit;
        if (IsLoaded) await RunUiAction(ConnectSelectedMode);
    }

    private void CommandGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CommandGrid.SelectedItem is not ST_EQUIPMENT_COMMAND_SPEC spec) return;
        SelectedCommandText.Text = spec.Template;
        SelectedDescriptionText.Text = spec.Description;
        ParameterText.IsEnabled = spec.RequiresParameter;
        ParameterText.Text = (spec.DefaultParameter ?? 0).ToString("G", CultureInfo.InvariantCulture);
        ParameterUnitText.Text = spec.Unit;
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || ComponentCombo.SelectedItem is null) return;
        EndpointTextBox.IsEnabled = !IsSimulation;
        BaudTextBox.IsEnabled = !IsSimulation;
        TcpPortTextBox.IsEnabled = !IsSimulation;
        FooterText.Text = IsSimulation ? "Simulation 모드: 실제 장비를 움직이지 않습니다." : "Hardware 모드: Read-only 조회부터 확인하고 동작 명령은 안전 잠금을 모두 확인하십시오.";
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e) => await RunUiAction(ConnectSelectedMode);
    private async void DisconnectButton_Click(object sender, RoutedEventArgs e) => await RunUiAction(async () => { if (_driver is not null) await _driver.Disconnect(); UpdateConnection(); });

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAction(async () =>
        {
            if (CommandGrid.SelectedItem is not ST_EQUIPMENT_COMMAND_SPEC spec) throw new InvalidOperationException("실행 기능을 선택하세요.");
            double? parameter = null;
            if (spec.RequiresParameter)
            {
                if (!double.TryParse(ParameterText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) throw new InvalidOperationException("Parameter는 '.' 소수점을 사용하는 숫자여야 합니다.");
                parameter = value;
            }
            var response = await RequireDriver().Execute(spec, parameter, ReadSafety());
            RefreshStatus();
            FooterText.Text = $"{SelectedProfile.DisplayName} / {spec.DisplayName}: {response}";
        });
    }

    private async void ValidationButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAction(async () =>
        {
            var result = await RequireDriver().RunReadOnlyValidation();
            ValidationGrid.ItemsSource = result;
            RefreshStatus(); MainTabs.SelectedIndex = 1;
            FooterText.Text = $"{SelectedProfile.DisplayName} read-only 검증: {result.Count(x => x.Passed)}/{result.Count} PASS";
        });
    }

    private async void ValidateAllButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAction(async () =>
        {
            var all = new List<ST_EQUIPMENT_VALIDATION>();
            foreach (var profile in CEquipmentCatalog.AllProfiles)
            {
                await using var driver = new CEquipmentDriver(profile, new CSimulatorEquipmentTransport(profile));
                await driver.Connect();
                all.AddRange(await driver.RunReadOnlyValidation());
            }
            ValidationGrid.ItemsSource = all; MainTabs.SelectedIndex = 1;
            FooterText.Text = $"전체 6개 컴포넌트 통합 검증: {all.Count(x => x.Passed)}/{all.Count} PASS";
        });
    }

    private void InjectTimeoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_simulator is null) { FooterText.Text = "오류 주입은 Simulation에서만 가능합니다."; return; }
        _simulator.InjectTimeoutOnce = true; FooterText.Text = "다음 명령 한 번에 timeout을 주입합니다.";
    }

    private void InjectInvalidButton_Click(object sender, RoutedEventArgs e)
    {
        if (_simulator is null) { FooterText.Text = "오류 주입은 Simulation에서만 가능합니다."; return; }
        _simulator.InjectInvalidResponseOnce = true; FooterText.Text = "다음 명령 한 번에 invalid response를 주입합니다.";
    }

    private async Task ConnectSelectedMode()
    {
        await DisposeDriver();
        var profile = SelectedProfile;
        IEquipmentTransport transport;
        if (IsSimulation)
        {
            _simulator = new CSimulatorEquipmentTransport(profile); transport = _simulator;
        }
        else if (profile.Transport == EN_EQUIPMENT_TRANSPORT.Serial)
        {
            transport = new CSerialEquipmentTransport(profile, EndpointTextBox.Text.Trim(), int.Parse(BaudTextBox.Text, CultureInfo.InvariantCulture), 1500);
        }
        else if (profile.Transport == EN_EQUIPMENT_TRANSPORT.Tcp)
        {
            transport = new CTcpEquipmentTransport(EndpointTextBox.Text.Trim(), int.Parse(TcpPortTextBox.Text, CultureInfo.InvariantCulture), 2000);
        }
        else transport = new CPicomotorCmdLibTransport(EndpointTextBox.Text.Trim());

        _driver = new CEquipmentDriver(profile, transport);
        TraceGrid.ItemsSource = _driver.Trace;
        await _driver.Connect();
        RefreshStatus(); UpdateConnection();
        FooterText.Text = $"{profile.DisplayName} 연결 완료: {transport.Endpoint}";
    }

    private CEquipmentDriver RequireDriver() => _driver ?? throw new InvalidOperationException("먼저 연결하세요.");
    private ST_EQUIPMENT_SAFETY ReadSafety() => new(UnlockCheck.IsChecked == true, MotionClearCheck.IsChecked == true, LaserSafeCheck.IsChecked == true, OperatorCheck.IsChecked == true);
    private void RefreshStatus() => StatusGrid.ItemsSource = _driver?.Status;

    private void UpdateConnection()
    {
        var state = _driver?.Transport.ConnectionState ?? EN_EQUIPMENT_CONNECTION.Offline;
        ConnectionText.Text = state.ToString().ToUpperInvariant();
        ConnectionLamp.Fill = FindBrush(state is EN_EQUIPMENT_CONNECTION.Online or EN_EQUIPMENT_CONNECTION.Simulation ? "SuccessBrush" : state == EN_EQUIPMENT_CONNECTION.Fault ? "DangerBrush" : "MutedBrush");
    }

    private async Task DisposeDriver()
    {
        if (_driver is not null) await _driver.DisposeAsync();
        _driver = null; _simulator = null; UpdateConnection();
    }

    private async Task RunUiAction(Func<Task> action)
    {
        if (_busy) return;
        _busy = true; SetButtons(false);
        try { await action(); }
        catch (Exception ex) { FooterText.Text = $"오류: {ex.Message}"; MessageBox.Show(ex.Message, "Driver Validation", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { _busy = false; SetButtons(true); UpdateConnection(); }
    }

    private void SetButtons(bool enabled) { ConnectButton.IsEnabled = enabled; DisconnectButton.IsEnabled = enabled; SendButton.IsEnabled = enabled; }
    private Brush FindBrush(string key) => (Brush)FindResource(key);
    private static string ShowTerminator(string value) => string.IsNullOrEmpty(value) ? "none" : value.Replace("\r", "<CR>", StringComparison.Ordinal).Replace("\n", "<LF>", StringComparison.Ordinal);
}
