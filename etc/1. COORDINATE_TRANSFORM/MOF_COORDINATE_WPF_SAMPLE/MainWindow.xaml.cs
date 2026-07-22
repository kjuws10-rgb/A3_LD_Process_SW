using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using MofCoordinateDemo.Automation1;
using MofCoordinateDemo.Models;
using MofCoordinateDemo.Services;

namespace MofCoordinateDemo;

public partial class MainWindow : Window
{
    private readonly CoordinateTransformService _service = new();
    private readonly AeroScriptGenerator _aeroScriptGenerator = new();
    private CoordinateInput _input = new();
    private CoordinateResult? _lastResult;
    private AeroScriptPackage? _currentScriptPackage;
    private Automation1DirectClient? _automation1Client;
    private Automation1ConnectionOptions? _connectedAutomation1Options;
    private CancellationTokenSource? _scriptWorkflowCancellation;
    private double _matrixCellSize = 86;
    private double _boardZoom = 1.0;
    private string? _lastConfigPath;
    private bool _suppressDoeChange;
    private bool _isMatrixDragSelecting;
    private bool _matrixDragAddMode = true;
    private bool _matrixSelectionChangedDuringDrag;
    private CellCommand? _selectionAnchor;
    private readonly HashSet<string> _dragVisitedPointKeys = new();
    private readonly HashSet<string> _selectedPointKeys = new();
    private const double MinimumLayoutCanvasWidth = 640.0;
    private const double MinimumLayoutCanvasHeight = 560.0;
    private static readonly string ViewStatePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MofCoordinateDemo",
        "view-state.json");

    public MainWindow()
    {
        InitializeComponent();
        LoadViewState();
        Loaded += (_, _) =>
        {
            LocalScriptPathBox.Text = ResolveLocalScriptPath();
            LoadInputToScreen(_input);
            ConfigureParameterTooltips();
            GenerateAndRender();
        };
        SizeChanged += (_, _) => DrawLayout();
        PreviewMouseLeftButtonUp += (_, _) => CompleteMatrixDragSelection();
        Closing += (_, _) =>
        {
            _scriptWorkflowCancellation?.Cancel();
            if (_automation1Client is not null)
            {
                _automation1Client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            SaveViewState();
        };
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        _input = ReadInputFromScreen();
        GenerateAndRender();
    }

    private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Excel CSV Config",
            Filter = "Excel CSV (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _lastConfigPath = dialog.FileName;
            ApplyConfigCsv(dialog.FileName);
            LoadInputToScreen(_input);
            ResetSelectionFromInput();
            GenerateAndRender();
        }
    }

    private void OpenConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var template = System.IO.Path.Combine(AppContext.BaseDirectory, "CELL_LAYOUT_CONFIG_TEMPLATE.csv");
        if (!File.Exists(template))
        {
            template = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CELL_LAYOUT_CONFIG_TEMPLATE.csv");
        }

        if (!File.Exists(template))
        {
            template = System.IO.Path.Combine(Environment.CurrentDirectory, "CELL_LAYOUT_CONFIG_TEMPLATE.csv");
        }

        _lastConfigPath = template;
        Process.Start(new ProcessStartInfo(template) { UseShellExecute = true });
    }

    private void ReloadConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_lastConfigPath) || !File.Exists(_lastConfigPath))
        {
            LoadConfigButton_Click(sender, e);
            return;
        }

        ApplyConfigCsv(_lastConfigPath);
        LoadInputToScreen(_input);
        ResetSelectionFromInput();
        GenerateAndRender();
    }

    private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        _input = ReadInputFromScreen();

        if (string.IsNullOrWhiteSpace(_lastConfigPath))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Current CSV Config",
                Filter = "Excel CSV (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = "CELL_LAYOUT_CONFIG_CURRENT.csv"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            _lastConfigPath = dialog.FileName;
        }

        SaveConfigCsv(_lastConfigPath);
        LoadInputToScreen(_input);
        GenerateAndRender();
    }

    private void GenerateAeroScriptButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GenerateCurrentAeroScriptPackage();
        }
        catch (Exception ex)
        {
            AppendDeploymentLog($"[생성 실패] {ex.Message}");
            ScriptJobText.Text = "Script 생성 실패";
        }
    }

    private async void UploadAeroScriptButton_Click(object sender, RoutedEventArgs e)
    {
        await RunScriptUiOperationAsync(async cancellationToken =>
        {
            var package = _currentScriptPackage ?? GenerateCurrentAeroScriptPackage();
            var client = await GetAutomation1DirectClientAsync(cancellationToken);
            var response = await client.UploadAsync(package, cancellationToken);
            ShowAutomation1Response("Controller 기록", response);
        });
    }

    private async void ControllerConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        await RunScriptUiOperationAsync(async cancellationToken =>
        {
            var client = await GetAutomation1DirectClientAsync(cancellationToken, forceReconnect: true);
            AppendDeploymentLog(
                $"[직접 연결] Automation1 Controller {ControllerHostBox.Text.Trim()}:" +
                $"{ReadInt(ControllerPortBox, Automation1ConnectionOptions.DefaultControllerPort)} API 접속 시도");
            var response = client.LastConnectionInfo
                           ?? throw new InvalidOperationException("Automation1 direct connection information is unavailable.");
            EnsureAutomation1Success("연결 확인", response);
            ShowAutomation1Response("연결 확인", response);
        });
    }

    private void OpenLocalScriptFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = ResolveLocalScriptPath();
            var directory = System.IO.Path.GetDirectoryName(path)
                            ?? throw new InvalidOperationException("Local Script 저장 폴더를 계산할 수 없습니다.");
            Directory.CreateDirectory(directory);

            var arguments = File.Exists(path) ? $"/select,\"{path}\"" : $"\"{directory}\"";
            Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppendDeploymentLog($"[저장 위치 열기 실패] {ex.Message}");
        }
    }

    private async void RunAeroScriptButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmHardwareExecution(_currentScriptPackage?.GenerationMode ?? GetSelectedScriptMode()))
        {
            return;
        }

        await RunScriptUiOperationAsync(async cancellationToken =>
        {
            var package = _currentScriptPackage
                          ?? throw new InvalidOperationException("먼저 Script를 생성하고 Controller File System에 기록해야 합니다.");
            var client = await GetAutomation1DirectClientAsync(cancellationToken);
            var response = await client.RunAsync(
                package.JobId,
                ReadHardwareReadiness(package.GenerationMode),
                cancellationToken);
            ShowAutomation1Response("실행 명령", response);
        });
    }

    private async void CompileAeroScriptButton_Click(object sender, RoutedEventArgs e)
    {
        await RunScriptUiOperationAsync(async cancellationToken =>
        {
            var package = _currentScriptPackage
                          ?? throw new InvalidOperationException(
                              "AeroScript를 먼저 생성하고 Controller에 기록한 뒤 Compile 검사를 실행하십시오.");
            var client = await GetAutomation1DirectClientAsync(cancellationToken);
            var response = await client.CompileAsync(package.JobId, cancellationToken);
            ShowAutomation1Response("Compile check", response);
        });
    }

    private async void QueryAeroScriptStatusButton_Click(object sender, RoutedEventArgs e)
    {
        await RunScriptUiOperationAsync(async cancellationToken =>
        {
            var package = _currentScriptPackage
                          ?? throw new InvalidOperationException("조회할 Script Job이 없습니다.");
            var client = await GetAutomation1DirectClientAsync(cancellationToken);
            var response = await client.GetStatusAsync(package.JobId, cancellationToken);
            ShowAutomation1Response("상태 조회", response);
        });
    }

    private async void StopAeroScriptButton_Click(object sender, RoutedEventArgs e)
    {
        await RunScriptUiOperationAsync(async cancellationToken =>
        {
            var package = _currentScriptPackage
                          ?? throw new InvalidOperationException("중지할 Script Job이 없습니다.");
            var client = await GetAutomation1DirectClientAsync(cancellationToken);
            var status = await client.StopAsync(package.JobId, cancellationToken);
            ShowAutomation1Response("실행 중지", status);
        });
    }

    private async void ExecuteAeroScriptWorkflowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmHardwareExecution(GetSelectedScriptMode()))
        {
            return;
        }

        await RunScriptUiOperationAsync(async cancellationToken =>
        {
            var package = GenerateCurrentAeroScriptPackage();
            var client = await GetAutomation1DirectClientAsync(cancellationToken);

            var upload = await client.UploadAsync(package, cancellationToken);
            EnsureAutomation1Success("Controller 기록", upload);
            ShowAutomation1Response("Controller 기록", upload);

            var compile = await client.CompileAsync(package.JobId, cancellationToken);
            EnsureAutomation1Success("Compile check", compile);
            ShowAutomation1Response("Compile check", compile);

            var run = await client.RunAsync(
                package.JobId,
                ReadHardwareReadiness(package.GenerationMode),
                cancellationToken);
            EnsureAutomation1Success("실행 명령", run);
            ShowAutomation1Response("실행 명령", run);

            string? lastStatusKey = null;
            while (true)
            {
                await Task.Delay(250, cancellationToken);
                var status = await client.GetStatusAsync(package.JobId, cancellationToken);
                EnsureAutomation1Success("상태 조회", status);
                var statusKey = $"{status.State}|{status.TaskState}|{status.Message}|{status.Error}";
                if (!statusKey.Equals(lastStatusKey, StringComparison.Ordinal))
                {
                    ShowAutomation1Response("상태", status);
                    lastStatusKey = statusKey;
                }

                if (status.State == Automation1DirectState.Completed)
                {
                    AppendDeploymentLog("[완료] 원격 Automation1 Task가 ProgramComplete 상태입니다.");
                    return;
                }

                if (status.State == Automation1DirectState.Failed)
                {
                    throw new InvalidOperationException(status.Message);
                }
            }
        }, TimeSpan.FromMinutes(30));
    }

    private AeroScriptPackage GenerateCurrentAeroScriptPackage()
    {
        _input = ReadInputFromScreen();
        GenerateAndRender();
        var result = _lastResult ?? throw new InvalidOperationException("좌표 결과가 생성되지 않았습니다.");
        var selectedHeads = ParseScannerHeadSet(_input.HighlightScannerHeads);

        IEnumerable<CellCommand> commands = result.MofExecutionCommands.Where(command => command.InField);
        if (selectedHeads.Count > 0)
        {
            commands = commands.Where(command => selectedHeads.Contains(command.ScannerIndex));
        }

        if (SelectedCoordinatesOnlyCheckBox.IsChecked == true)
        {
            commands = commands.Where(command => _selectedPointKeys.Contains(PointKey(command)));
        }

        var commandList = commands.OrderBy(command => command.MofSequence).ToArray();
        if (commandList.Length == 0)
        {
            throw new InvalidOperationException("No process coordinates are selected for AeroScript generation.");
        }

        var options = ReadAeroScriptGenerationOptions();
        var source = _aeroScriptGenerator.Generate(_input, commandList, options);
        var taskIndex = Math.Max(1, ReadInt(Automation1TaskIndexBox, 1));
        var controllerFile = ControllerFileNameBox.Text.Trim();
        ValidateControllerFileNameForClient(controllerFile);
        var localScriptPath = AeroScriptLocalFileStore.Save(LocalScriptPathBox.Text, source, AppContext.BaseDirectory);

        _currentScriptPackage = AeroScriptPackage.Create(
            controllerFile,
            source,
            taskIndex,
            commandList.Length,
            options.Mode,
            AeroScriptGenerator.ResolveRequiredAxisNames(commandList, options),
            PreserveControllerJobFileCheckBox.IsChecked == true);
        LocalScriptPathBox.Text = localScriptPath;
        ScriptPreviewBox.Text = source;
        ScriptJobText.Text =
            $"생성 완료: Job={_currentScriptPackage.JobId}, 좌표={commandList.Length}, " +
            $"Mode={options.Mode}, SHA-256={_currentScriptPackage.Sha256[..16]}..., Task={taskIndex}";
        AppendDeploymentLog(
            $"[생성] Client PC에서 {options.Mode}, {commandList.Length}개 좌표로 " +
            $"로컬 파일 저장 완료: {localScriptPath}");
        AppendDeploymentLog(
            $"[Controller 대상] 직접 연결 후 Controller File System의 " +
            $"{_currentScriptPackage.ControllerFileName}에 기록");
        var firstCommand = commandList.First();
        AppendDeploymentLog(
            $"[좌표 기준] Script 이동값은 Process Gx/Gy=({firstCommand.Gx:0.######}, {firstCommand.Gy:0.######}), " +
            $"Review 표시/측정 좌표=({firstCommand.ReviewCoordinateX:0.######}, {firstCommand.ReviewCoordinateY:0.######})입니다. " +
            "두 좌표는 기준 원점과 물리 Offset이 다르므로 동일한 값이 아니며 Review 좌표를 Scanner 이동 명령에 직접 사용하지 않습니다.");
        return _currentScriptPackage;
    }

    private string ResolveLocalScriptPath()
    {
        return AeroScriptLocalFileStore.ResolvePath(LocalScriptPathBox.Text, AppContext.BaseDirectory);
    }

    private static void ValidateControllerFileNameForClient(string controllerFile)
    {
        if (string.IsNullOrWhiteSpace(controllerFile) ||
            controllerFile.Contains('\\') ||
            controllerFile.Contains(':') ||
            controllerFile.Contains("..", StringComparison.Ordinal) ||
            !controllerFile.EndsWith(".ascript", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Controller File은 Client PC의 D:\\ 경로가 아닙니다. " +
                "'programs/mof_generated.ascript'처럼 Controller 내부 경로를 '/'로 입력하고, " +
                "Client 저장 위치는 Local Script File에 입력하십시오.");
        }
    }

    private AeroScriptGenerationOptions ReadAeroScriptGenerationOptions()
    {
        return new AeroScriptGenerationOptions
        {
            Mode = GetSelectedScriptMode(),
            StageAxisName = StageAxisNameBox.Text.Trim(),
            AxisXTemplate = AxisXTemplateBox.Text.Trim(),
            AxisYTemplate = AxisYTemplateBox.Text.Trim(),
            StartYPosition = ReadDouble(StartYPositionBox, 500),
            StageTravelDistance = ReadDouble(StageTravelDistanceBox, 40),
            StageSpeed = ReadDouble(StageSpeedBox, 20),
            ScannerRapidSpeed = ReadDouble(ScannerRapidSpeedBox, 1000),
            CoordinatedSpeed = ReadDouble(CoordinatedSpeedBox, 100),
            RampRate = ReadDouble(RampRateBox, 3_000_000),
            TrajectoryFirFilter = ReadDouble(TrajectoryFirFilterBox, 3),
            MotionUpdateRateKhz = ReadDouble(MotionUpdateRateBox, 100),
            ExecuteNumLines = Math.Max(1, ReadInt(ExecuteNumLinesBox, 110)),
            SetupDwellSeconds = ReadDouble(SetupDwellSecondsBox, 0.2),
            MoveDelayMilliseconds = ReadDouble(MoveDelayMillisecondsBox, 0.1),
            WaitStepY = ReadDouble(WaitStepYBox, 10),
            SoftwareLimitLow = ReadDouble(SoftwareLimitLowBox, -10_000),
            SoftwareLimitHigh = ReadDouble(SoftwareLimitHighBox, 10_000),
            EnableAxes = EnableAxesCheckBox.IsChecked == true,
            DisableAxesAtEnd = DisableAxesAtEndCheckBox.IsChecked == true,
            IncludeLaserLibraryImport = IncludeLaserLibraryCheckBox.IsChecked == true,
            LaserLibraryFileName = LaserLibraryNameBox.Text.Trim()
        };
    }

    private AeroScriptGenerationMode GetSelectedScriptMode()
    {
        var tag = (ScriptModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return Enum.TryParse<AeroScriptGenerationMode>(tag, out var mode)
            ? mode
            : AeroScriptGenerationMode.VirtualWaitSimulation;
    }

    private bool ConfirmHardwareExecution(AeroScriptGenerationMode mode)
    {
        if (mode != AeroScriptGenerationMode.HardwareCoordinateProgram)
        {
            return true;
        }

        return MessageBox.Show(
                   this,
                   "Hardware Coordinate Program을 원격 Automation1 Controller에서 실행합니다.\n" +
                   "축 이름, Software Limit, 속도, Ramp, 장비 Interlock과 Laser 안전 상태를 확인했습니까?",
                   "Automation1 Hardware 실행 확인",
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Warning,
                   MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private Automation1HardwareReadiness ReadHardwareReadiness(AeroScriptGenerationMode generationMode)
    {
        if (generationMode == AeroScriptGenerationMode.VirtualWaitSimulation)
        {
            return Automation1HardwareReadiness.Simulation;
        }

        return new Automation1HardwareReadiness(
            EquipmentMotionReadyCheckBox.IsChecked == true,
            EquipmentInterlockReadyCheckBox.IsChecked == true,
            EquipmentLaserReadyCheckBox.IsChecked == true,
            EquipmentOperatorConfirmCheckBox.IsChecked == true);
    }

    private async Task<Automation1DirectClient> GetAutomation1DirectClientAsync(
        CancellationToken cancellationToken,
        bool forceReconnect = false)
    {
        var options = new Automation1ConnectionOptions(
            ControllerHostBox.Text.Trim(),
            ReadInt(ControllerPortBox, Automation1ConnectionOptions.DefaultControllerPort),
            Automation1ConnectionMode.NoAuthentication,
            "",
            "",
            "",
            StartControllerIfStoppedCheckBox.IsChecked == true);

        if (!forceReconnect && _automation1Client is not null && _connectedAutomation1Options == options)
        {
            return _automation1Client;
        }

        if (_automation1Client is not null)
        {
            await _automation1Client.DisposeAsync();
        }

        _automation1Client = new Automation1DirectClient(options);
        await _automation1Client.ConnectAsync(cancellationToken);
        _connectedAutomation1Options = options;
        return _automation1Client;
    }

    private async Task RunScriptUiOperationAsync(
        Func<CancellationToken, Task> operation,
        TimeSpan? timeout = null)
    {
        _scriptWorkflowCancellation?.Cancel();
        _scriptWorkflowCancellation?.Dispose();
        _scriptWorkflowCancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        SetScriptButtonsEnabled(false);

        try
        {
            await operation(_scriptWorkflowCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            AppendDeploymentLog("[취소] 통신 또는 실행 완료 대기 시간이 초과되었습니다.");
            ScriptJobText.Text = "작업 취소 또는 제한시간 초과";
        }
        catch (Exception ex)
        {
            AppendDeploymentLog($"[오류] {ex.Message}");
            ScriptJobText.Text = $"오류: {ex.Message}";
        }
        finally
        {
            SetScriptButtonsEnabled(true);
        }
    }

    private void ShowAutomation1Response(string operation, Automation1ConnectionInfo response)
    {
        AppendDeploymentLog($"[{operation}] {response.Message}");
        ScriptJobText.Text = response.Message;
    }

    private void ShowAutomation1Response(string operation, Automation1DirectStatus response)
    {
        AppendDeploymentLog(
            $"[{operation}] State={response.State}, TaskState={response.TaskState}, Message={response.Message}");
        if (!string.IsNullOrWhiteSpace(response.Error))
        {
            AppendDeploymentLog($"[{operation} detail] {response.Error}");
        }

        if (!string.IsNullOrWhiteSpace(response.ControllerAuditFileName))
        {
            AppendDeploymentLog($"[Controller 기록] Audit={response.ControllerAuditFileName}");
        }

        ScriptJobText.Text =
            $"Job={response.JobId}, State={response.State}, Task={response.TaskIndex}, {response.Message}";
    }

    private static void EnsureAutomation1Success(string operation, Automation1ConnectionInfo response)
    {
        if (!response.IsRunning)
        {
            throw new InvalidOperationException($"{operation} 실패: Automation1 Controller가 실행 중이 아닙니다.");
        }
    }

    private static void EnsureAutomation1Success(string operation, Automation1DirectStatus response)
    {
        if (response.State == Automation1DirectState.Failed)
        {
            throw new InvalidOperationException($"{operation} 실패: {response.Message} {response.Error}".Trim());
        }
    }

    private void AppendDeploymentLog(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
        DeploymentLogBox.AppendText((DeploymentLogBox.Text.Length == 0 ? "" : Environment.NewLine) + line);
        DeploymentLogBox.ScrollToEnd();
    }

    private void DeploymentLogBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        DeploymentLogBox.Clear();
        e.Handled = true;
    }

    private void SetScriptButtonsEnabled(bool isEnabled)
    {
        GenerateAeroScriptButton.IsEnabled = isEnabled;
        ControllerConnectionButton.IsEnabled = isEnabled;
        UploadAeroScriptButton.IsEnabled = isEnabled;
        CompileAeroScriptButton.IsEnabled = isEnabled;
        RunAeroScriptButton.IsEnabled = isEnabled;
        QueryAeroScriptStatusButton.IsEnabled = isEnabled;
        StopAeroScriptButton.IsEnabled = true;
        ExecuteAeroScriptWorkflowButton.IsEnabled = isEnabled;
    }

    private void SelectAllScannersButton_Click(object sender, RoutedEventArgs e)
    {
        var count = Math.Max(1, ReadInt(ScannerCountBox, _input.ScannerCount));
        _input.HighlightScannerHeads = string.Join(",", Enumerable.Range(1, count));
        LoadInputToScreen(_input);
        GenerateAndRender(selectHighlightedScannerPoints: true);
    }

    private void ClearScannerSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        _input.HighlightScannerHeads = "";
        _selectedPointKeys.Clear();
        _selectionAnchor = null;
        LoadInputToScreen(_input);
        GenerateAndRender(keepEmptySelection: true);
    }

    private void GenerateAndRender(bool selectHighlightedScannerPoints = false, bool keepEmptySelection = false)
    {
        _lastResult = _service.Generate(_input);
        if (selectHighlightedScannerPoints)
        {
            SelectAllProcessablePointsForHighlightedScanners();
        }
        else if (_selectedPointKeys.Count == 0 && !keepEmptySelection)
        {
            ResetSelectionFromInput();
        }

        BuildMatrixCanvas(DesignMatrixCanvas, "Design");
        BuildMatrixCanvas(ProcessMatrixCanvas, "Process");
        BuildMatrixCanvas(ReviewMatrixCanvas, "Review");
        BuildDoeMatrixPanel();

        var selected = _lastResult.Commands.FirstOrDefault(x => x.IsSelectedCell);
        var selectedScannerCount = ParseScannerHeadSet(_input.HighlightScannerHeads).Count;
        var inFieldCount = _lastResult.Commands.Count(x => x.InField);
        var filteredCount = GetVisibleMatrixCommands().Count;
        SummaryText.Text =
            $"AK1 Stage 기준 = ({_lastResult.Ak1GlobalX:0.######}, {_lastResult.Ak1GlobalY:0.######}) mm   " +
            $"전체 좌표 = {_lastResult.Commands.Count}, X 커버 좌표 = {inFieldCount}, 선택 Scanner = {selectedScannerCount}, 표시 좌표 = {filteredCount}, 선택 좌표 = {_selectedPointKeys.Count}   " +
            $"Camera→H1 물리 Offset = ({_input.ReviewToFirstScannerOffsetX:0.###}, {_input.ReviewToFirstScannerOffsetY:0.###}) mm   " +
            $"H1 초기위치 = ({_input.FirstScannerInitialStageX:0.###}, {_input.FirstScannerInitialStageY:0.###}) / 기대값 = ({_lastResult.ExpectedFirstScannerStageX:0.###}, {_lastResult.ExpectedFirstScannerStageY:0.###}) / 원점검증 = {(_lastResult.FirstScannerOriginValid ? "정상" : "불일치")}   " +
            $"반전 Y = {_lastResult.TurnaroundStageY:0.###} mm / 배치검증 = {(_lastResult.EquipmentOrderValid ? "정상" : "확인필요")}   " +
            $"Review 기준 = H{_lastResult.SelectedReviewScanner.Index} DOE{_lastResult.SelectedDoeBeam.BeamNo:00}";

        FormulaText.Text =
            "동작 순서: Home에서 정방향으로 Review Camera를 먼저 통과해 Scanner 뒤쪽까지 이동한 뒤 방향을 반전합니다. 역방향 복귀 중 MOF 좌표를 Y 먼 위치부터 순서대로 가공하고, Review Camera에서 후측정한 다음 Home으로 복귀합니다. " +
            "H1 초기 Stage 위치는 ReviewCenter + CameraToH1PhysicalOffset과 허용오차 안에서 같아야 합니다. Process G는 실제 H1 초기위치에서 만든 Scanner Center 기준이며, Review 좌표는 ScannerRelative(Stage축) + CameraToScannerPhysicalOffset + DOE Stage Offset으로 계산합니다. " +
            "Dynamic Review Correction은 가공 후 측정오차를 다음 가공에 보정하는 값으로, 고정 물리 Offset과 별도로 관리합니다. 스캐너 박스 또는 주변 클릭 영역을 누르면 복수 선택/해제가 됩니다. 선택된 스캐너가 있으면 좌표 Matrix는 해당 스캐너의 X 가공 가능 범위에 포함되는 좌표만 표시합니다. " +
            "Matrix 셀은 드래그로 연속 선택하고, Shift 클릭으로 기준 셀부터 현재 셀까지 범위 선택하며, Ctrl 클릭/드래그로 추가 또는 해제할 수 있습니다. " +
            "Ctrl + 마우스 휠로 Board와 Matrix를 확대/축소하면 마지막 비율이 다음 실행에도 유지됩니다.";

        DrawLayout();
    }

    private void DrawLayout()
    {
        if (_lastResult is null)
        {
            return;
        }

        LayoutCanvas.Children.Clear();
        ResizeLayoutCanvas();

        DrawTitle("기판 셀 선택 및 지그재그 스캐너 배치", 20, 8, 21, FontWeights.Bold);
        DrawMotionSequence();

        var compactHeader = LayoutCanvas.Width < 1000.0;
        var boardLeft = 24.0;
        var boardTop = compactHeader ? 112.0 : 104.0;
        var boardWidth = LayoutCanvas.Width - 48.0;
        var boardHeight = Math.Max(220.0, LayoutCanvas.Height - 390.0);

        DrawBoardFrame(boardLeft, boardTop, boardWidth, boardHeight);
        DrawCellBlocks(boardLeft, boardTop, boardWidth, boardHeight);
        DrawScannerBandLabels(boardLeft, boardTop + boardHeight + 10.0, boardWidth, boardHeight);
        DrawScannerHeads(boardLeft, boardTop + boardHeight + 76.0, boardWidth);
        if (compactHeader)
        {
            DrawLegend(20.0, 82.0);
        }
        else
        {
            DrawLegend(Math.Max(20.0, LayoutCanvas.Width - 610.0), 14.0);
        }
    }

    private void DrawMotionSequence()
    {
        if (_lastResult is null)
        {
            return;
        }

        var forward = _input.ForwardTransportSignY >= 0 ? "+Y" : "-Y";
        var reverse = _input.ForwardTransportSignY >= 0 ? "-Y" : "+Y";
        var text = $"① Home  →  ② Review Camera 통과  →  ③ Scanner 뒤쪽/반전 ({forward})  →  ④ 역방향 MOF ({reverse})  →  ⑤ Review 후측정  →  ⑥ Home";
        var reservedWidth = LayoutCanvas.Width >= 1000.0 ? 640.0 : 40.0;
        var badgeWidth = Math.Clamp(LayoutCanvas.Width - reservedWidth, 120.0, 940.0);
        DrawBadge(text, 20, 43, badgeWidth, 34,
            new SolidColorBrush(Color.FromRgb(12, 38, 58)),
            new SolidColorBrush(Color.FromRgb(34, 211, 238)),
            new SolidColorBrush(Color.FromRgb(207, 250, 254)),
            13,
            FontWeights.SemiBold);
    }

    private void ResizeLayoutCanvas()
    {
        var viewportWidth = Math.Max(980, LayoutScrollViewer.ViewportWidth);
        var viewportHeight = Math.Max(430, LayoutScrollViewer.ViewportHeight);
        var scaledWidth = Math.Max(viewportWidth, 1280.0) * _boardZoom;
        var scaledHeight = Math.Max(viewportHeight, 560.0) * _boardZoom;
        LayoutCanvas.Width = Math.Max(MinimumLayoutCanvasWidth, scaledWidth);
        LayoutCanvas.Height = Math.Max(MinimumLayoutCanvasHeight, scaledHeight);
    }

    private void DrawBoardFrame(double left, double top, double width, double height)
    {
        var frame = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush(Color.FromRgb(8, 13, 24)),
            Stroke = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            StrokeThickness = 1.2
        };
        Canvas.SetLeft(frame, left);
        Canvas.SetTop(frame, top);
        LayoutCanvas.Children.Add(frame);

        DrawAlignKey(left + 10, top + 10, "AK1");
        DrawAlignKey(left + 10, top + height - 18, "AK2");
        DrawAlignKey(left + width - 18, top + 10, "AK3");
        DrawAlignKey(left + width - 18, top + height - 18, "AK4");
    }

    private void DrawCellBlocks(double boardLeft, double boardTop, double boardWidth, double boardHeight)
    {
        if (_lastResult is null)
        {
            return;
        }

        var scale = GetBoardScale(boardWidth, boardHeight);
        var visualPitchX = Math.Max(1.0, _input.CellPitchX * scale);
        var visualPitchY = Math.Max(1.0, _input.CellPitchY * scale);
        var cellW = Math.Max(0.8, Math.Min(visualPitchX * 0.72, visualPitchX - 0.2));
        var cellH = Math.Max(0.8, Math.Min(visualPitchY * 0.72, visualPitchY - 0.2));

        DrawHighlightedScannerAreas(boardLeft, boardTop, scale);

        foreach (var command in _lastResult.Commands)
        {
            var x = boardLeft + 28 + command.LocalX * scale;
            var y = boardTop + 20 + command.LocalY * scale;
            var isSelected = IsPointSelected(command);
            var isHeadSelected = command.IsHighlightedScanner;

            var fill = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            if (isHeadSelected)
            {
                fill = new SolidColorBrush(command.InField ? Color.FromRgb(14, 116, 144) : Color.FromRgb(22, 78, 99));
            }
            if (isSelected)
            {
                fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }

            var stroke = command.ScannerIndex % 2 == 1
                ? new SolidColorBrush(Color.FromRgb(250, 204, 21))
                : new SolidColorBrush(Color.FromRgb(167, 139, 250));

            var rect = new Rectangle
            {
                Width = cellW,
                Height = cellH,
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = isSelected ? 2.6 : 1.0,
                Tag = command,
                Cursor = Cursors.Hand
            };
            rect.MouseLeftButtonDown += CellRect_MouseLeftButtonDown;
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            LayoutCanvas.Children.Add(rect);

            var hitSizeW = Math.Max(cellW, 20);
            var hitSizeH = Math.Max(cellH, 20);
            var hitRect = new Rectangle
            {
                Width = hitSizeW,
                Height = hitSizeH,
                Fill = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                StrokeThickness = 0,
                Tag = command,
                Cursor = Cursors.Hand
            };
            hitRect.MouseLeftButtonDown += CellRect_MouseLeftButtonDown;
            Canvas.SetLeft(hitRect, x + (cellW - hitSizeW) * 0.5);
            Canvas.SetTop(hitRect, y + (cellH - hitSizeH) * 0.5);
            LayoutCanvas.Children.Add(hitRect);

            if (cellW > 34 && cellH > 24)
            {
                DrawText(command.MatrixPointName, x + 5, y + 4, Math.Min(13, Math.Max(8, cellH * 0.28)), FontWeights.SemiBold, Brushes.White);
            }
        }

        foreach (var blockGroup in _lastResult.Commands.GroupBy(x => x.CellBlock))
        {
            var first = blockGroup.OrderBy(x => x.Row).ThenBy(x => x.Column).First();
            var x = boardLeft + 28 + first.LocalX * scale;
            var y = Math.Max(boardTop + 8, boardTop + 20 + first.LocalY * scale - 24);
            DrawBadge($"Cell#{first.CellBlock}", x - 2, y, 58, 20,
                new SolidColorBrush(Color.FromArgb(210, 15, 23, 42)),
                new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                12,
                FontWeights.Bold);
        }
    }

    private double GetBoardScale(double boardWidth, double boardHeight)
    {
        var blockPitchX = EffectiveBlockPitchX(_input);
        var blockPitchY = EffectiveBlockPitchY(_input);
        var maxLocalX = (_input.CellBlockColumns - 1) * blockPitchX
                        + _input.CellFirstX
                        + Math.Max(1, _input.CellColumns - 1) * _input.CellPitchX
                        + _input.PatternOffsetX;
        var maxLocalY = (_input.CellBlockRows - 1) * blockPitchY
                        + _input.CellFirstY
                        + Math.Max(1, _input.CellRows - 1) * _input.CellPitchY
                        + _input.PatternOffsetY;
        var scannerMaxLocalX = _lastResult?.Scanners.Max(x => Math.Abs(x.CenterX - _lastResult.Ak1GlobalX) + x.FieldHalfX) ?? 0;
        maxLocalX = Math.Max(maxLocalX, scannerMaxLocalX);
        var scaleX = (boardWidth - 60) / Math.Max(maxLocalX + _input.CellPitchX, 1);
        var scaleY = (boardHeight - 42) / Math.Max(maxLocalY + _input.CellPitchY, 1);
        return Math.Min(scaleX, scaleY);
    }

    private void DrawScannerHeads(double left, double top, double width)
    {
        if (_lastResult is null)
        {
            return;
        }

        var boardScale = GetBoardScale(width, Math.Max(220.0, LayoutCanvas.Height - 390.0));
        var minScannerY = _lastResult.Scanners.Min(x => x.CenterY);
        var boxWidth = 74.0;
        var boxHeight = 64.0;

        foreach (var scanner in _lastResult.Scanners)
        {
            var localX = scanner.CenterX - _lastResult.Ak1GlobalX;
            var x = left + 28 + localX * boardScale - boxWidth * 0.5;
            var yOffset = Math.Max(0, (scanner.CenterY - minScannerY) * boardScale);
            if (yOffset > 0)
            {
                yOffset = Math.Max(78, yOffset);
            }

            var y = top + yOffset;
            var isActiveScanner = scanner.IsHighlighted;
            var fill = isActiveScanner
                ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                : new SolidColorBrush(Color.FromRgb(15, 23, 42));

            var hit = new Rectangle
            {
                Width = boxWidth + 28,
                Height = boxHeight + 34,
                Fill = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                StrokeThickness = 0,
                Tag = scanner,
                Cursor = Cursors.Hand
            };
            hit.MouseLeftButtonDown += ScannerRect_MouseLeftButtonDown;
            Canvas.SetLeft(hit, x - 14);
            Canvas.SetTop(hit, y - 12);
            LayoutCanvas.Children.Add(hit);

            var box = new Rectangle
            {
                Width = boxWidth,
                Height = boxHeight,
                Fill = fill,
                Stroke = isActiveScanner
                    ? new SolidColorBrush(Color.FromRgb(125, 211, 252))
                    : new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                StrokeThickness = isActiveScanner ? 2.8 : 1.6,
                Tag = scanner,
                Cursor = Cursors.Hand
            };
            box.MouseLeftButtonDown += ScannerRect_MouseLeftButtonDown;
            Canvas.SetLeft(box, x);
            Canvas.SetTop(box, y);
            LayoutCanvas.Children.Add(box);

            DrawText($"Scanner\n#{scanner.Index}", x + 8, y + 14, 15, FontWeights.SemiBold, isActiveScanner ? Brushes.White : new SolidColorBrush(Color.FromRgb(203, 213, 225)));
            DrawText($"({scanner.CenterX:0.#}, {scanner.CenterY:0.#})", x - 4, y + boxHeight + 6, 10, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(148, 163, 184)));
        }
    }

    private void DrawHighlightedScannerAreas(double boardLeft, double boardTop, double scale)
    {
        if (_lastResult is null)
        {
            return;
        }

        var theta = _input.ThetaAlignDeg * Math.PI / 180.0;
        var cos = Math.Cos(theta);
        var sin = Math.Sin(theta);

        var boardHeight = Math.Max(220.0, LayoutCanvas.Height - 390.0);

        foreach (var scanner in _lastResult.Scanners.Where(x => x.IsHighlighted))
        {
            var dx = scanner.CenterX - _lastResult.Ak1GlobalX;
            var dyAtAk1 = 0.0;
            var localX = cos * dx + sin * dyAtAk1;

            var x = boardLeft + 28 + (localX - scanner.FieldHalfX) * scale;
            var y = boardTop + 20;
            var width = scanner.FieldHalfX * 2 * scale;
            var height = Math.Max(8, boardHeight - 40);

            var area = new Rectangle
            {
                Width = Math.Max(3, width),
                Height = Math.Max(3, height),
                Fill = new SolidColorBrush(Color.FromArgb(92, 8, 145, 178)),
                Stroke = new SolidColorBrush(Color.FromRgb(34, 211, 238)),
                StrokeDashArray = new DoubleCollection { 5, 4 },
                StrokeThickness = 2.4
            };
            Canvas.SetLeft(area, x);
            Canvas.SetTop(area, y);
            LayoutCanvas.Children.Add(area);
        }
    }

    private void DrawScannerBandLabels(double boardLeft, double labelTop, double boardWidth, double boardHeight)
    {
        if (_lastResult is null)
        {
            return;
        }

        var highlighted = _lastResult.Scanners.Where(x => x.IsHighlighted).OrderBy(x => x.CenterX).ToList();
        if (highlighted.Count == 0)
        {
            return;
        }

        var scale = GetBoardScale(boardWidth, boardHeight);
        var theta = _input.ThetaAlignDeg * Math.PI / 180.0;
        var cos = Math.Cos(theta);
        var sin = Math.Sin(theta);
        var labelWidth = Math.Min(235, Math.Max(160, boardWidth * 0.32));
        var labelHeight = 24.0;
        var rowGap = 28.0;
        var occupiedRows = new List<List<(double Left, double Right)>> { new(), new() };

        foreach (var scanner in highlighted)
        {
            var dx = scanner.CenterX - _lastResult.Ak1GlobalX;
            var localX = cos * dx + sin * 0.0;
            var bandCenterX = boardLeft + 28 + localX * scale;
            var labelLeft = Math.Clamp(bandCenterX - labelWidth * 0.5, boardLeft + 8, boardLeft + boardWidth - labelWidth - 8);
            var labelRight = labelLeft + labelWidth;
            var rowIndex = 0;

            for (var row = 0; row < occupiedRows.Count; row++)
            {
                if (occupiedRows[row].All(x => labelRight < x.Left - 8 || labelLeft > x.Right + 8))
                {
                    rowIndex = row;
                    break;
                }

                rowIndex = row;
            }

            occupiedRows[rowIndex].Add((labelLeft, labelRight));
            var text = $"{scanner.Name} X Band  {scanner.CenterX - scanner.FieldHalfX:0.#} ~ {scanner.CenterX + scanner.FieldHalfX:0.#} mm / Y 이동 커버";
            DrawBadge(text, labelLeft, labelTop + rowIndex * rowGap, labelWidth, labelHeight,
                new SolidColorBrush(Color.FromRgb(8, 47, 73)),
                new SolidColorBrush(Color.FromRgb(34, 211, 238)),
                new SolidColorBrush(Color.FromRgb(207, 250, 254)),
                11,
                FontWeights.SemiBold);
        }
    }


    private void CellRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: CellCommand command })
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var key = PointKey(command);
                if (!_selectedPointKeys.Add(key))
                {
                    _selectedPointKeys.Remove(key);
                }
            }
            else
            {
                _selectedPointKeys.Clear();
                _selectedPointKeys.Add(PointKey(command));
            }

            _input.SelectedCellBlock = command.CellBlock;
            _input.SelectedCellColumn = command.Column;
            _input.SelectedCellRow = command.Row;
            LoadInputToScreen(_input);
            GenerateAndRender();
        }
    }

    private void ScannerRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ScannerModel scanner })
        {
            _input.ReviewBasisScannerHead = scanner.Index;
            var selectedHeads = ParseScannerHeadSet(_input.HighlightScannerHeads);
            if (!selectedHeads.Add(scanner.Index))
            {
                selectedHeads.Remove(scanner.Index);
            }

            _input.HighlightScannerHeads = string.Join(",", selectedHeads.OrderBy(x => x));
            LoadInputToScreen(_input);
            GenerateAndRender(selectHighlightedScannerPoints: true, keepEmptySelection: true);
        }
    }

    private void SelectAllProcessablePointsForHighlightedScanners()
    {
        _selectedPointKeys.Clear();
        _selectionAnchor = null;
        if (_lastResult is null)
        {
            return;
        }

        var selectedHeads = ParseScannerHeadSet(_input.HighlightScannerHeads);
        var processable = _lastResult.Commands
            .Where(command => command.InField && selectedHeads.Contains(command.ScannerIndex))
            .OrderBy(command => command.MofSequence)
            .ToArray();

        foreach (var command in processable)
        {
            _selectedPointKeys.Add(PointKey(command));
        }

        _selectionAnchor = processable.FirstOrDefault();
        if (_selectionAnchor is not null)
        {
            SetInputSelection(_selectionAnchor);
        }
    }

    private void MatrixScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        _matrixCellSize = Math.Clamp(_matrixCellSize + (e.Delta > 0 ? 8 : -8), 48, 180);
        SaveViewState();
        if (_lastResult is not null)
        {
            BuildMatrixCanvas(DesignMatrixCanvas, "Design");
            BuildMatrixCanvas(ProcessMatrixCanvas, "Process");
            BuildMatrixCanvas(ReviewMatrixCanvas, "Review");
        }

        e.Handled = true;
    }

    private void DrawMatrixHeader(Canvas canvas, string text, double x, double y, double width, double height, double fontSize, Brush fill)
    {
        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = fill,
            Stroke = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);

        DrawCanvasText(canvas, text, x + 4, y + 4, width - 8, height - 8, fontSize, FontWeights.SemiBold, new SolidColorBrush(Color.FromRgb(226, 232, 240)));
    }

    private void DrawMatrixCell(Canvas canvas, CellCommand command, string mode, double x, double y, double width, double height, double fontSize)
    {
        var selected = IsPointSelected(command);
        var fill = selected
            ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
            : command.IsHighlightedScanner
                ? new SolidColorBrush(Color.FromRgb(14, 116, 144))
                : new SolidColorBrush(Color.FromRgb(30, 41, 59));

        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = fill,
            Stroke = selected
                ? new SolidColorBrush(Color.FromRgb(253, 230, 138))
                : new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            StrokeThickness = selected ? 2.2 : 1,
            Tag = command,
            Cursor = Cursors.Hand,
            ToolTip = $"{command.CellIndex}\n" +
                      $"역방향 MOF 실행 순서: #{command.MofSequence}\n" +
                      $"Camera 기준 상대좌표: {command.ReviewCameraRelativeMatrix} mm\n" +
                      $"Camera→{command.ScannerName} 물리 Offset: {command.ScannerPhysicalOffsetMatrix} mm\n" +
                      $"Scanner 상대좌표: {command.ScannerRelativeMatrix} mm\n" +
                      $"Offset으로 재계산한 Scanner 상대좌표: {command.ScannerRelativeFromPhysicalOffsetMatrix} mm\n" +
                      $"Origin/Offset 일치 오차: {command.PhysicalTransformErrorMatrix} mm\n" +
                      $"Review Camera 좌표: {command.ReviewMatrix} mm\n" +
                      $"MOF Gx/Gy: {command.ProcessGMatrix}"
        };
        rect.MouseLeftButtonDown += MatrixCell_MouseLeftButtonDown;
        rect.MouseEnter += MatrixCell_MouseEnter;
        rect.MouseLeftButtonUp += MatrixCell_MouseLeftButtonUp;
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);

        var text = FormatMatrixCell(command, mode);
        DrawCanvasText(canvas, text, x + 4, y + 4, width - 8, height - 8, fontSize, FontWeights.Normal, selected ? Brushes.White : new SolidColorBrush(Color.FromRgb(203, 213, 225)));
    }

    private void MatrixCell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CellCommand command })
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var key = PointKey(command);
        var wasSelected = _selectedPointKeys.Contains(key);

        _isMatrixDragSelecting = true;
        _matrixSelectionChangedDuringDrag = false;
        _matrixDragAddMode = (modifiers & ModifierKeys.Control) == ModifierKeys.Control ? !wasSelected : true;
        _dragVisitedPointKeys.Clear();
        _dragVisitedPointKeys.Add(key);

        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            SelectMatrixRange(command, additive: (modifiers & ModifierKeys.Control) == ModifierKeys.Control);
            _matrixSelectionChangedDuringDrag = true;
            CompleteMatrixDragSelection();
            e.Handled = true;
            return;
        }

        ApplySingleMatrixSelection(command, modifiers);
        UpdateMatrixCellVisual(sender as Shape, command);
        _matrixSelectionChangedDuringDrag = true;
        e.Handled = true;
    }

    private void MatrixCell_MouseEnter(object sender, MouseEventArgs e)
    {
        if (!_isMatrixDragSelecting || e.LeftButton != MouseButtonState.Pressed || sender is not FrameworkElement { Tag: CellCommand command })
        {
            return;
        }

        var key = PointKey(command);
        if (!_dragVisitedPointKeys.Add(key))
        {
            return;
        }

        if (_matrixDragAddMode)
        {
            _selectedPointKeys.Add(key);
        }
        else
        {
            _selectedPointKeys.Remove(key);
        }

        SetInputSelection(command);
        UpdateMatrixCellVisual(sender as Shape, command);
        _matrixSelectionChangedDuringDrag = true;
        e.Handled = true;
    }

    private void MatrixCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CompleteMatrixDragSelection();
        e.Handled = true;
    }

    private void ApplySingleMatrixSelection(CellCommand command, ModifierKeys modifiers)
    {
        var key = PointKey(command);
        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (!_selectedPointKeys.Add(key))
            {
                _selectedPointKeys.Remove(key);
            }
        }
        else
        {
            _selectedPointKeys.Clear();
            _selectedPointKeys.Add(key);
        }

        SetInputSelection(command);
        _selectionAnchor = command;
    }

    private void SelectMatrixRange(CellCommand command, bool additive)
    {
        if (!additive)
        {
            _selectedPointKeys.Clear();
        }

        var anchor = _selectionAnchor ?? command;
        var ordered = GetVisibleMatrixCommands()
            .OrderBy(x => x.CellBlock)
            .ThenBy(x => x.Row)
            .ThenBy(x => x.Column)
            .ToList();
        var anchorIndex = ordered.FindIndex(x => PointKey(x) == PointKey(anchor));
        var targetIndex = ordered.FindIndex(x => PointKey(x) == PointKey(command));

        if (anchorIndex < 0 || targetIndex < 0)
        {
            _selectedPointKeys.Add(PointKey(command));
        }
        else
        {
            var start = Math.Min(anchorIndex, targetIndex);
            var end = Math.Max(anchorIndex, targetIndex);
            for (var index = start; index <= end; index++)
            {
                _selectedPointKeys.Add(PointKey(ordered[index]));
            }
        }

        SetInputSelection(command);
    }

    private void CompleteMatrixDragSelection()
    {
        if (!_isMatrixDragSelecting)
        {
            return;
        }

        _isMatrixDragSelecting = false;
        _dragVisitedPointKeys.Clear();

        if (_matrixSelectionChangedDuringDrag)
        {
            LoadInputToScreen(_input);
            GenerateAndRender();
        }

        _matrixSelectionChangedDuringDrag = false;
    }

    private void SetInputSelection(CellCommand command)
    {
        _input.SelectedCellBlock = command.CellBlock;
        _input.SelectedCellColumn = command.Column;
        _input.SelectedCellRow = command.Row;
    }

    private void UpdateMatrixCellVisual(Shape? shape, CellCommand command)
    {
        if (shape is null)
        {
            return;
        }

        var selected = IsPointSelected(command);
        shape.Fill = selected
            ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
            : command.IsHighlightedScanner
                ? new SolidColorBrush(Color.FromRgb(14, 116, 144))
                : new SolidColorBrush(Color.FromRgb(30, 41, 59));
        shape.Stroke = selected
            ? new SolidColorBrush(Color.FromRgb(253, 230, 138))
            : new SolidColorBrush(Color.FromRgb(51, 65, 85));
        shape.StrokeThickness = selected ? 2.2 : 1;
    }

    private static void DrawCanvasText(Canvas canvas, string text, double x, double y, double width, double height, double fontSize, FontWeight weight, Brush brush)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = brush,
            Width = Math.Max(1, width),
            Height = Math.Max(1, height),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        canvas.Children.Add(block);
    }

    private void LayoutScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        _boardZoom = Math.Clamp(_boardZoom + (e.Delta > 0 ? 0.12 : -0.12), 0.35, 4.0);
        SaveViewState();
        DrawLayout();
        e.Handled = true;
    }

    private void BuildMatrixCanvas(Canvas canvas, string mode)
    {
        if (_lastResult is null)
        {
            return;
        }

        canvas.Children.Clear();

        var headerWidth = Math.Max(72, _matrixCellSize * 0.88);
        var cellWidth = _matrixCellSize;
        var cellHeight = _matrixCellSize;
        var rowHeaderHeight = 26.0;
        var fontSize = Math.Clamp(_matrixCellSize / 6.2, 8, 18);
        var blockHeaderHeight = 28.0;
        var y = rowHeaderHeight;

        DrawMatrixHeader(canvas, "Cell#", 0, 0, headerWidth, rowHeaderHeight, fontSize, new SolidColorBrush(Color.FromRgb(30, 41, 59)));
        for (var column = 0; column < _input.CellColumns; column++)
        {
            DrawMatrixHeader(canvas, ToColumnLetter(column), headerWidth + column * cellWidth, 0, cellWidth, rowHeaderHeight, fontSize, new SolidColorBrush(Color.FromRgb(30, 41, 59)));
        }

        var visibleCommands = GetVisibleMatrixCommands();
        if (visibleCommands.Count == 0)
        {
            DrawMatrixHeader(canvas, "선택된 스캐너의 X 가공 가능 범위에 포함되는 좌표가 없습니다.", 0, y, headerWidth + _input.CellColumns * cellWidth, 44, fontSize, new SolidColorBrush(Color.FromRgb(22, 78, 99)));
            canvas.Width = headerWidth + _input.CellColumns * cellWidth + 20;
            canvas.Height = y + 64;
            return;
        }

        foreach (var blockGroup in visibleCommands.GroupBy(x => x.CellBlock).OrderBy(x => x.Key))
        {
            DrawMatrixHeader(canvas, $"Cell#{blockGroup.Key}", 0, y, headerWidth + _input.CellColumns * cellWidth, blockHeaderHeight, fontSize, new SolidColorBrush(Color.FromRgb(15, 23, 42)));
            y += blockHeaderHeight;

            foreach (var rowGroup in blockGroup.GroupBy(x => x.Row).OrderBy(x => x.Key))
            {
                DrawMatrixHeader(canvas, (rowGroup.Key + 1).ToString(CultureInfo.InvariantCulture), 0, y, headerWidth, cellHeight, fontSize, new SolidColorBrush(Color.FromRgb(17, 24, 39)));

                foreach (var command in rowGroup.OrderBy(x => x.Column))
                {
                    DrawMatrixCell(canvas, command, mode, headerWidth + command.Column * cellWidth, y, cellWidth, cellHeight, fontSize);
                }

                y += cellHeight;
            }
        }

        canvas.Width = headerWidth + _input.CellColumns * cellWidth + 20;
        canvas.Height = y + 20;
    }

    private void BuildDoeMatrixPanel()
    {
        if (_lastResult is null)
        {
            return;
        }

        _suppressDoeChange = true;
        DoeMatrixPanel.Children.Clear();

        foreach (var beam in _lastResult.DoeBeams.OrderBy(x => x.BeamNo))
        {
            var radio = new RadioButton
            {
                GroupName = "Doe16",
                IsChecked = beam.BeamNo == _input.ReviewBasisDoeBeam,
                Tag = beam,
                Margin = new Thickness(6),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Content = new Border
                {
                    BorderBrush = beam.BeamNo == _input.ReviewBasisDoeBeam
                        ? new SolidColorBrush(Color.FromRgb(253, 230, 138))
                        : new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    BorderThickness = new Thickness(1.4),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4),
                    Background = beam.BeamNo == _input.ReviewBasisDoeBeam
                        ? new SolidColorBrush(Color.FromRgb(180, 83, 9))
                        : new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    Child = new TextBlock
                    {
                        Text = $"DOE{beam.BeamNo:00}\nR{beam.Row} C{beam.Column}\n{beam.MatrixCoordinate}",
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 9.5,
                        FontWeight = beam.BeamNo == _input.ReviewBasisDoeBeam ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = beam.BeamNo == _input.ReviewBasisDoeBeam ? Brushes.White : new SolidColorBrush(Color.FromRgb(203, 213, 225))
                    }
                }
            };
            radio.Checked += DoeRadio_Checked;
            DoeMatrixPanel.Children.Add(radio);
        }

        _suppressDoeChange = false;
    }

    private void DoeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressDoeChange || sender is not FrameworkElement { Tag: DoeBeamModel beam })
        {
            return;
        }

        _input.ReviewBasisDoeBeam = beam.BeamNo;
        LoadInputToScreen(_input);
        GenerateAndRender();
    }

    private static string FormatMatrixCell(CellCommand command, string mode)
    {
        var coordinate = mode switch
        {
            "Design" => command.DesignLocalMatrix,
            "Process" => command.ProcessGMatrix,
            "Review" => command.ReviewMatrix,
            _ => command.DesignLocalMatrix
        };

        var extra = mode switch
        {
            "Process" => $"{command.ScannerName}",
            "Review" => command.DoeSelection,
            _ => ""
        };

        return string.IsNullOrWhiteSpace(extra)
            ? $"{command.MatrixPointName}\n{coordinate}"
            : $"{command.MatrixPointName}\n{coordinate}\n{extra}";
    }

    private void DrawLegend(double left, double top)
    {
        DrawLegendBox(left, top, Color.FromRgb(245, 158, 11), "선택 좌표");
        DrawLegendBox(left + 132, top, Color.FromRgb(14, 116, 144), "선택 스캐너 가공 가능 좌표");
        DrawLegendBox(left + 360, top, Color.FromRgb(34, 211, 238), "선택 스캐너 / X 가공 Band");
    }

    private void DrawLegendBox(double x, double y, Color color, string text)
    {
        var box = new Rectangle
        {
            Width = 18,
            Height = 18,
            Fill = new SolidColorBrush(color),
            Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(box, x);
        Canvas.SetTop(box, y);
        LayoutCanvas.Children.Add(box);
        DrawText(text, x + 25, y - 1, 13, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(203, 213, 225)));
    }

    private void DrawAlignKey(double x, double y, string text)
    {
        var ak = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(Color.FromRgb(255, 204, 77)),
            Stroke = new SolidColorBrush(Color.FromRgb(117, 85, 0)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(ak, x);
        Canvas.SetTop(ak, y);
        LayoutCanvas.Children.Add(ak);
        DrawText(text, x + 13, y - 4, 12, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(253, 230, 138)));
    }

    private void DrawTitle(string text, double x, double y, double size, FontWeight weight)
    {
        DrawText(text, x, y, size, weight, new SolidColorBrush(Color.FromRgb(248, 250, 252)));
    }

    private void DrawBadge(string text, double x, double y, double width, double height, Brush background, Brush borderBrush, Brush foreground, double fontSize, FontWeight weight)
    {
        var safeX = double.IsFinite(x) ? x : 0.0;
        var safeY = double.IsFinite(y) ? y : 0.0;
        var safeWidth = NormalizeVisualDimension(width, 1.0);
        var safeHeight = NormalizeVisualDimension(height, 1.0);
        var safeFontSize = NormalizeVisualDimension(fontSize, 12.0);
        var remainingCanvasWidth = Math.Max(1.0, LayoutCanvas.Width - Math.Max(0.0, safeX));
        safeWidth = Math.Min(safeWidth, remainingCanvasWidth);

        var badge = new Border
        {
            Width = safeWidth,
            Height = safeHeight,
            Background = background,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new TextBlock
            {
                Text = text,
                FontSize = safeFontSize,
                FontWeight = weight,
                Foreground = foreground,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            }
        };
        Canvas.SetLeft(badge, safeX);
        Canvas.SetTop(badge, safeY);
        LayoutCanvas.Children.Add(badge);
    }

    private static double NormalizeVisualDimension(double value, double fallback) =>
        double.IsFinite(value) && value > 0.0 ? value : fallback;

    private void DrawText(string text, double x, double y, double size, FontWeight weight, Brush brush)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = brush,
            TextAlignment = TextAlignment.Center,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        LayoutCanvas.Children.Add(block);
    }

    private void ConfigureParameterTooltips()
    {
        SetTip(BoardXBox, "Board X", "기판의 전체 X 방향 길이입니다. Board 표시 영역과 AK3/AK4 방향 기준 폭을 결정합니다.", "AK 유효 X 거리 = BoardX - 2 * AK Margin X", "Board");
        SetTip(BoardYBox, "Board Y", "기판의 전체 Y 방향 길이입니다. Board 표시 영역과 AK2/AK4 방향 기준 높이를 결정합니다.", "AK 유효 Y 거리 = BoardY - 2 * AK Margin Y", "Board");
        SetTip(AkMarginXBox, "AK Margin X", "기판 Edge에서 Align Key까지의 X 방향 설계 여유거리입니다.", "AK1~AK3 거리 계산과 Board 설명에 사용", "Board");
        SetTip(AkMarginYBox, "AK Margin Y", "기판 Edge에서 Align Key까지의 Y 방향 설계 여유거리입니다.", "AK1~AK2 거리 계산과 Board 설명에 사용", "Board");
        SetTip(HomeStageYBox, "Home Stage Y", "공정 시작과 종료 시 기판 Stage가 대기하는 Y 원점 위치입니다. 공정은 Home에서 Scanner 방향으로 전진한 뒤 역방향 MOF, Review 후측정, Home 복귀 순서로 수행됩니다.", "Motion: Home → Review → Scanner → Reverse MOF → Review → Home", "Board");
        SetTip(ForwardTransportSignYBox, "Forward Transport Y Sign", "Home에서 Review Camera를 먼저 지나 Scanner 쪽으로 이동하는 정물류 방향의 Stage Y 부호입니다. +1이면 +Y가 전진이고 역방향 MOF는 -Y입니다. -1이면 반대입니다.", "+1: forward +Y / MOF -Y, -1: forward -Y / MOF +Y", "Board");

        SetTip(ReviewCenterXBox, "Review Center X", "Review Camera 중심이 Stage 좌표계에서 가지는 X 좌표입니다. AK1 pixel 측정값을 Stage 좌표로 환산할 때 기준점입니다.", "AK1_X = ReviewCenterX + (AK1_U - U0) * PixelScaleX", "Review");
        SetTip(ReviewCenterYBox, "Review Center Y", "Review Camera 중심이 Stage 좌표계에서 가지는 Y 좌표입니다. AK1 pixel 측정값을 Stage 좌표로 환산할 때 기준점입니다.", "AK1_Y = ReviewCenterY + (AK1_V - V0) * PixelScaleY", "Review");
        SetTip(U0Box, "Review U0", "Review 영상의 중심 U pixel 좌표입니다. 실제 측정 AK1 U와의 차이가 X 방향 mm offset으로 변환됩니다.", "dU = AK1_U - U0", "Review");
        SetTip(V0Box, "Review V0", "Review 영상의 중심 V pixel 좌표입니다. 실제 측정 AK1 V와의 차이가 Y 방향 mm offset으로 변환됩니다.", "dV = AK1_V - V0", "Review");
        SetTip(ScaleXBox, "Pixel Scale X", "Review Camera pixel 1개가 Stage X 방향으로 몇 mm인지 나타내는 환산 계수입니다.", "Stage dX = dU * PixelScaleX", "Review");
        SetTip(ScaleYBox, "Pixel Scale Y", "Review Camera pixel 1개가 Stage Y 방향으로 몇 mm인지 나타내는 환산 계수입니다.", "Stage dY = dV * PixelScaleY", "Review");
        SetTip(Ak1UBox, "Measured AK1 U", "Review Camera 화면에서 측정된 AK1의 U pixel 좌표입니다. AK1 Stage Anchor를 만드는 실제 측정값입니다.", "AK1_X = ReviewCenterX + (AK1_U - U0) * ScaleX", "Review");
        SetTip(Ak1VBox, "Measured AK1 V", "Review Camera 화면에서 측정된 AK1의 V pixel 좌표입니다. AK1 Stage Anchor를 만드는 실제 측정값입니다.", "AK1_Y = ReviewCenterY + (AK1_V - V0) * ScaleY", "Review");
        SetTip(ThetaBox, "Theta Align", "Review 측정으로 계산된 기판 회전 보정각입니다. Recipe Local 좌표를 Stage 좌표로 변환할 때 회전 행렬에 적용됩니다.", "Pstage = AK1 + R(theta) * Plocal", "Review");

        SetTip(CellFirstXBox, "Cell First X", "AK1을 기준으로 첫 번째 Cell#의 첫 번째 가공점 A1까지 떨어진 X 거리입니다.", "A1_X = CellFirstX + PatternOffsetX", "Cell");
        SetTip(CellFirstYBox, "Cell First Y", "AK1을 기준으로 첫 번째 Cell#의 첫 번째 가공점 A1까지 떨어진 Y 거리입니다.", "A1_Y = CellFirstY + PatternOffsetY", "Cell");
        SetTip(CellPitchXBox, "Cell Pitch X", "Cell# 내부 가공홀 Matrix에서 열 방향 간격입니다. A열, B열, C열 사이 거리를 의미합니다.", "B1_X = A1_X + CellPitchX", "Cell");
        SetTip(CellPitchYBox, "Cell Pitch Y", "Cell# 내부 가공홀 Matrix에서 행 방향 간격입니다. 1행, 2행, 3행 사이 거리를 의미합니다.", "A2_Y = A1_Y + CellPitchY", "Cell");
        SetTip(PatternOffsetXBox, "Pattern Offset X", "Cell 내부에서 실제 가공 Pattern이 첫 기준점에서 추가로 이동하는 X offset입니다.", "Plocal.X = BlockOriginX + FirstX + Col*PitchX + PatternOffsetX", "Cell");
        SetTip(PatternOffsetYBox, "Pattern Offset Y", "Cell 내부에서 실제 가공 Pattern이 첫 기준점에서 추가로 이동하는 Y offset입니다.", "Plocal.Y = BlockOriginY + FirstY + Row*PitchY + PatternOffsetY", "Cell");
        SetTip(CellColumnsBox, "Cell Columns", "Cell# 하나 안에 들어가는 가공점 Matrix의 열 개수입니다. 화면에는 A, B, C... 알파벳 열로 표시됩니다.", "예: 3이면 A/B/C 열 생성", "Cell");
        SetTip(CellRowsBox, "Cell Rows", "Cell# 하나 안에 들어가는 가공점 Matrix의 행 개수입니다. 화면에는 1, 2, 3... 숫자 행으로 표시됩니다.", "예: 4이면 1/2/3/4 행 생성", "Cell");
        SetTip(CellBlockColumnsBox, "Cell Block Columns", "기판 위에 Cell# 블록을 X 방향으로 몇 개 배치할지 설정합니다.", "전체 Cell# 수 = BlockColumns * BlockRows", "Cell");
        SetTip(CellBlockRowsBox, "Cell Block Rows", "기판 위에 Cell# 블록을 Y 방향으로 몇 개 배치할지 설정합니다.", "Cell# 번호는 위에서 아래, 왼쪽에서 오른쪽 순서로 생성됩니다.", "Cell");
        SetTip(CellBlockPitchXBox, "Cell Block Pitch X", "Cell# 블록과 다음 Cell# 블록 사이의 X 방향 거리입니다. 0이면 내부 Matrix 폭을 기준으로 자동 계산해 겹침을 방지합니다.", "BlockOriginX = BlockColumn * EffectiveBlockPitchX", "Cell");
        SetTip(CellBlockPitchYBox, "Cell Block Pitch Y", "Cell# 블록과 다음 Cell# 블록 사이의 Y 방향 거리입니다. 0이면 내부 Matrix 높이를 기준으로 자동 계산합니다.", "BlockOriginY = BlockRow * EffectiveBlockPitchY", "Cell");
        SetTip(SelectedCellBlockBox, "Selected Cell#", "현재 선택된 Cell# 번호입니다. Board나 Matrix에서 Cell을 클릭하면 자동으로 갱신됩니다.", "예: Cell#2의 B3 선택", "Cell");
        SetTip(SelectedCellColumnBox, "Selected Col", "현재 선택된 가공점의 열 Index입니다. 내부 계산은 0부터 시작하고, 화면 표시는 A, B, C로 변환됩니다.", "0=A, 1=B, 2=C", "Cell");
        SetTip(SelectedCellRowBox, "Selected Row", "현재 선택된 가공점의 행 Index입니다. 내부 계산은 0부터 시작하고, 화면 표시는 1, 2, 3으로 변환됩니다.", "0=1행, 1=2행", "Cell");

        SetTip(ScannerCountBox, "Scanner Count", "장비에 배치된 Scanner Head 개수입니다. 하단 Zigzag Scanner UI의 Head 수를 결정합니다.", "H1, H2, H3... 생성", "Scanner");
        SetTip(HighlightHeadsBox, "Highlight Heads", "Board UI에서 가공 가능 영역을 강조할 Scanner 번호 목록입니다. 콤마로 여러 개를 입력할 수 있습니다.", "예: 1,5 또는 2,4,6", "Scanner");
        SetTip(FirstScannerInitialXBox, "H1 Initial Stage X", "장비 설계 또는 Teaching으로 확정한 H1 Scanner 중심의 초기 Stage X 좌표입니다. Scanner 배치는 이 값을 실제 기준으로 사용합니다.", "Expected H1.X = ReviewCenterX + CameraToH1OffsetX", "Scanner");
        SetTip(FirstScannerInitialYBox, "H1 Initial Stage Y", "장비 설계 또는 Teaching으로 확정한 H1 Scanner 중심의 초기 Stage Y 좌표입니다. 짝수 Head는 이 기준에 Even Y Offset을 더합니다.", "Expected H1.Y = ReviewCenterY + CameraToH1OffsetY", "Scanner");
        SetTip(CameraToScannerOffsetXBox, "Review Camera → H1 Physical Offset X", "Review Camera 광학 중심에서 H1 Scanner 가공 중심까지의 고정된 X 방향 설계/캘리브레이션 거리입니다. 리뷰 측정 위치를 스캐너 상대 가공좌표로 옮기는 핵심 기준이며, 가공 후 오차 보정값과 구분해 관리합니다.", "H1.CenterX = ReviewCenterX + PhysicalOffsetX", "Scanner");
        SetTip(CameraToScannerOffsetYBox, "Review Camera → H1 Physical Offset Y", "Review Camera 광학 중심에서 H1 Scanner 가공 중심까지의 고정된 Y 방향 설계/캘리브레이션 거리입니다. 기판이 Y로 이동할 때 동일한 가공점을 H1 아래로 보내기 위한 기준 거리입니다.", "H1.CenterY = ReviewCenterY + PhysicalOffsetY", "Scanner");
        SetTip(ScannerOriginToleranceBox, "Scanner Origin Tolerance", "입력한 H1 초기 Stage 위치와 ReviewCenter + PhysicalOffset으로 계산한 H1 기대 위치 사이에 허용할 최대 오차입니다. 초과하면 화면에 원점검증 불일치가 표시됩니다.", "abs(H1Initial - ExpectedH1) <= Tolerance", "Scanner");
        SetTip(ScannerPitchXBox, "Scanner Pitch X", "인접 Scanner Head 사이의 X 방향 중심 간격입니다.", "H2_X - H1_X", "Scanner");
        SetTip(EvenYOffsetBox, "Even Y Offset", "짝수 Scanner Head가 홀수 Head 기준선에서 Y 방향으로 얼마나 어긋나 배치되는지 나타냅니다.", "Zigzag 배치용 Y offset", "Scanner");
        SetTip(FieldHalfXBox, "Process Area Half X", "Scanner가 가공 가능한 영역의 X 반폭입니다. Scanner를 클릭하면 CenterX ± HalfX 범위 안의 가공점이 강조됩니다.", "abs(TargetX - ScannerCenterX) <= HalfX", "Scanner");
        SetTip(FieldHalfYBox, "Process Area Half Y", "Scanner Head의 설계상 Y 방향 field 반폭 참고값입니다. MOF에서는 기판이 Y 방향으로 이동하므로 가공 가능 여부는 X 커버리지로 판단하고, UI process band는 Board Y 전체로 표시합니다.", "MOF processability = abs(TargetX - ScannerCenterX) <= HalfX", "Scanner");
        SetTip(ReviewBasisHeadBox, "Review Basis Head", "Review 좌표계를 어느 Scanner Head의 DOE Beam을 기준으로 표현할지 선택합니다.", "ReviewCoordinate = ProcessStage - SelectedHeadDoeStage", "Scanner");
        SetTip(ReviewBasisBeamBox, "DOE Beam 1-16", "DOE 4x4 Beam 중 Review 좌표계의 기준으로 사용할 Beam 번호입니다.", "1~16, 좌상단부터 행 우선 순서", "Doe");
        SetTip(DoePitchXBox, "DOE Pitch X", "DOE 16 Beam 내부에서 Beam 간 X 방향 간격입니다.", "BeamOffsetX = (Column - 1.5) * DoePitchX", "Doe");
        SetTip(DoePitchYBox, "DOE Pitch Y", "DOE 16 Beam 내부에서 Beam 간 Y 방향 간격입니다.", "BeamOffsetY = (Row - 1.5) * DoePitchY", "Doe");

        SetTip(OffsetXBox, "Dynamic Review Correction X", "가공 후 Review 측정에서 확인된 X 오차를 다음 가공좌표에 되먹임하는 동적 보정값입니다. Camera→Scanner 고정 물리 Offset과는 수명주기와 목적이 다릅니다.", "Pprocess.X = Pdesign.X + ReviewCorrectionX", "Offset");
        SetTip(OffsetYBox, "Dynamic Review Correction Y", "가공 후 Review 측정에서 확인된 Y 오차를 다음 가공좌표에 되먹임하는 동적 보정값입니다. 설비 기구 치수인 Camera→Scanner 고정 물리 Offset과 별도로 저장합니다.", "Pprocess.Y = Pdesign.Y + ReviewCorrectionY", "Offset");

        SetTip(GenerateButton, "Generate / Refresh", "현재 입력값을 기준으로 Cell 배치, Stage 좌표, Scanner 가공좌표, Review 좌표계를 다시 계산합니다.", "입력값 변경 후 누르면 화면 전체 갱신", "None");
        SetTip(OpenConfigButton, "Open CSV Template in Excel", "CSV 템플릿을 Excel 또는 Windows 기본 CSV 앱으로 엽니다. 값을 저장한 뒤 Reload 또는 Load로 화면에 반영합니다.", "Excel에서 저장 -> Reload Current CSV", "None");
        SetTip(LoadConfigButton, "Load Saved CSV Config", "저장된 CSV 설정 파일을 선택해 화면 입력값과 좌표 배치를 갱신합니다.", "Key,Value 형식 CSV", "None");
        SetTip(ReloadConfigButton, "Reload Current CSV", "마지막으로 열거나 로드한 CSV 파일을 다시 읽어 화면을 갱신합니다.", "Excel에서 수정 저장 후 바로 Reload", "None");
        SetTip(SaveConfigButton, "Save Current CSV", "현재 화면 입력값을 CSV 파일에 저장합니다. CSV 설정을 수정한 뒤 파일까지 업데이트할 때 사용합니다.", "화면 입력값 -> CSV 저장", "None");
        SetTip(SelectAllScannersButton, "All Scanner Select", "전체 Scanner를 선택해서 모든 Scanner의 X process band 기준으로 가공 가능한 좌표를 Matrix View에 표시합니다.", "전체 Scanner 선택 -> 좌표 View 필터 적용", "None");
        SetTip(ClearScannerSelectionButton, "Clear Scanner", "Scanner 선택을 모두 해제합니다. 선택 Scanner가 없으면 Matrix View는 전체 좌표를 표시합니다.", "Scanner 선택 해제 -> 전체 좌표 표시", "None");
    }

    private void SetTip(Control control, string title, string description, string formula, string diagramKind)
    {
        ToolTipService.SetInitialShowDelay(control, 250);
        ToolTipService.SetShowDuration(control, 30000);

        var panel = new StackPanel { MaxWidth = 420 };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.Bold,
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 32, 51)),
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(37, 54, 77))
        });
        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(244, 247, 251)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(210, 219, 230)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 8, 0, 8),
            Padding = new Thickness(7),
            Child = new TextBlock
            {
                Text = formula,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(32, 44, 60))
            }
        });
        if (diagramKind != "None")
        {
            panel.Children.Add(CreateTooltipDiagram(diagramKind));
        }

        control.ToolTip = new ToolTip { Content = panel };
    }

    private Canvas CreateTooltipDiagram(string kind)
    {
        var canvas = new Canvas { Width = 300, Height = 112, Background = Brushes.White };

        if (kind == "Board")
        {
            DrawDiagramRect(canvas, 30, 25, 230, 62, Brushes.White, Color.FromRgb(183, 160, 37));
            DrawDiagramDot(canvas, 42, 36, "AK1");
            DrawDiagramDot(canvas, 242, 36, "AK3");
            DrawDiagramDot(canvas, 42, 74, "AK2");
            DrawDiagramDot(canvas, 242, 74, "AK4");
            DrawDiagramText(canvas, "Board X", 126, 8, 12);
            DrawDiagramText(canvas, "Board Y", 262, 51, 12);
        }
        else if (kind == "Review")
        {
            DrawDiagramRect(canvas, 38, 22, 92, 68, Brushes.White, Color.FromRgb(93, 143, 169));
            DrawDiagramText(canvas, "Review\nCamera", 54, 42, 12);
            DrawDiagramDot(canvas, 190, 55, "AK1");
            DrawDiagramText(canvas, "pixel offset -> mm\nAK1 Stage Anchor", 145, 75, 12);
        }
        else if (kind == "Cell")
        {
            DrawDiagramDot(canvas, 28, 22, "AK1");
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    DrawDiagramRect(canvas, 88 + col * 42, 28 + row * 24, 24, 14, Brushes.White, Color.FromRgb(178, 158, 47));
                }
            }
            DrawDiagramText(canvas, "First X/Y", 42, 58, 12);
            DrawDiagramText(canvas, "Pitch X", 128, 8, 12);
            DrawDiagramText(canvas, "Pitch Y", 224, 55, 12);
        }
        else if (kind == "Scanner")
        {
            DrawDiagramRect(canvas, 34, 34, 54, 42, new SolidColorBrush(Color.FromRgb(144, 211, 78)), Colors.Black);
            DrawDiagramText(canvas, "H1", 52, 47, 13);
            DrawDiagramRect(canvas, 122, 57, 54, 42, Brushes.White, Colors.Black);
            DrawDiagramText(canvas, "H2", 140, 70, 13);
            DrawDiagramRect(canvas, 205, 34, 62, 42, new SolidColorBrush(Color.FromArgb(40, 80, 170, 255)), Color.FromRgb(30, 91, 190));
            DrawDiagramText(canvas, "Process\nArea", 214, 42, 12);
        }
        else if (kind == "Doe")
        {
            for (var row = 0; row < 4; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    var beam = row * 4 + col + 1;
                    DrawDiagramRect(canvas, 78 + col * 34, 16 + row * 22, 28, 18, Brushes.White, Color.FromRgb(93, 143, 169));
                    DrawDiagramText(canvas, beam.ToString(CultureInfo.InvariantCulture), 87 + col * 34, 18 + row * 22, 10);
                }
            }
            DrawDiagramText(canvas, "DOE 4x4 Beam", 84, 95, 12);
        }
        else if (kind == "Offset")
        {
            DrawDiagramDot(canvas, 85, 58, "Target");
            DrawDiagramDot(canvas, 190, 42, "Corrected");
            DrawDiagramText(canvas, "Review Error 반대 방향\nProcess Offset 적용", 82, 78, 12);
        }
        else
        {
            DrawDiagramRect(canvas, 54, 25, 190, 60, Brushes.White, Color.FromRgb(93, 143, 169));
            DrawDiagramText(canvas, "CSV\nKey,Value", 116, 44, 13);
        }

        return canvas;
    }

    private static void DrawDiagramRect(Canvas canvas, double x, double y, double width, double height, Brush fill, Color stroke)
    {
        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = fill,
            Stroke = new SolidColorBrush(stroke),
            StrokeThickness = 1.2
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }

    private static void DrawDiagramDot(Canvas canvas, double x, double y, string text)
    {
        var dot = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Color.FromRgb(255, 204, 77)),
            Stroke = new SolidColorBrush(Color.FromRgb(117, 85, 0)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(dot, x);
        Canvas.SetTop(dot, y);
        canvas.Children.Add(dot);
        DrawDiagramText(canvas, text, x + 11, y - 4, 11);
    }

    private static void DrawDiagramText(Canvas canvas, string text, double x, double y, double fontSize)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = new SolidColorBrush(Color.FromRgb(37, 54, 77)),
            TextWrapping = TextWrapping.Wrap
        };
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        canvas.Children.Add(block);
    }

    private void LoadInputToScreen(CoordinateInput input)
    {
        BoardXBox.Text = Format(input.BoardSizeX);
        BoardYBox.Text = Format(input.BoardSizeY);
        AkMarginXBox.Text = Format(input.AlignMarginX);
        AkMarginYBox.Text = Format(input.AlignMarginY);
        HomeStageYBox.Text = Format(input.HomeStageY);
        ForwardTransportSignYBox.Text = input.ForwardTransportSignY.ToString(CultureInfo.InvariantCulture);
        ReviewCenterXBox.Text = Format(input.ReviewCenterGlobalX);
        ReviewCenterYBox.Text = Format(input.ReviewCenterGlobalY);
        U0Box.Text = Format(input.ReviewPixelCenterU);
        V0Box.Text = Format(input.ReviewPixelCenterV);
        ScaleXBox.Text = Format(input.PixelScaleX);
        ScaleYBox.Text = Format(input.PixelScaleY);
        Ak1UBox.Text = Format(input.MeasuredAk1U);
        Ak1VBox.Text = Format(input.MeasuredAk1V);
        ThetaBox.Text = Format(input.ThetaAlignDeg);
        CellFirstXBox.Text = Format(input.CellFirstX);
        CellFirstYBox.Text = Format(input.CellFirstY);
        CellPitchXBox.Text = Format(input.CellPitchX);
        CellPitchYBox.Text = Format(input.CellPitchY);
        PatternOffsetXBox.Text = Format(input.PatternOffsetX);
        PatternOffsetYBox.Text = Format(input.PatternOffsetY);
        CellColumnsBox.Text = input.CellColumns.ToString(CultureInfo.InvariantCulture);
        CellRowsBox.Text = input.CellRows.ToString(CultureInfo.InvariantCulture);
        CellBlockColumnsBox.Text = input.CellBlockColumns.ToString(CultureInfo.InvariantCulture);
        CellBlockRowsBox.Text = input.CellBlockRows.ToString(CultureInfo.InvariantCulture);
        CellBlockPitchXBox.Text = Format(input.CellBlockPitchX);
        CellBlockPitchYBox.Text = Format(input.CellBlockPitchY);
        SelectedCellBlockBox.Text = input.SelectedCellBlock.ToString(CultureInfo.InvariantCulture);
        SelectedCellColumnBox.Text = input.SelectedCellColumn.ToString(CultureInfo.InvariantCulture);
        SelectedCellRowBox.Text = input.SelectedCellRow.ToString(CultureInfo.InvariantCulture);
        ScannerCountBox.Text = input.ScannerCount.ToString(CultureInfo.InvariantCulture);
        HighlightHeadsBox.Text = input.HighlightScannerHeads;
        FirstScannerInitialXBox.Text = Format(input.FirstScannerInitialStageX);
        FirstScannerInitialYBox.Text = Format(input.FirstScannerInitialStageY);
        CameraToScannerOffsetXBox.Text = Format(input.ReviewToFirstScannerOffsetX);
        CameraToScannerOffsetYBox.Text = Format(input.ReviewToFirstScannerOffsetY);
        ScannerOriginToleranceBox.Text = Format(input.ScannerOriginTolerance);
        ScannerPitchXBox.Text = Format(input.ScannerPitchX);
        EvenYOffsetBox.Text = Format(input.EvenScannerYOffset);
        FieldHalfXBox.Text = Format(input.ScannerFieldHalfX);
        FieldHalfYBox.Text = Format(input.ScannerFieldHalfY);
        ReviewBasisHeadBox.Text = input.ReviewBasisScannerHead.ToString(CultureInfo.InvariantCulture);
        ReviewBasisBeamBox.Text = input.ReviewBasisDoeBeam.ToString(CultureInfo.InvariantCulture);
        DoePitchXBox.Text = Format(input.DoeBeamPitchX);
        DoePitchYBox.Text = Format(input.DoeBeamPitchY);
        OffsetXBox.Text = Format(input.ProcessOffsetGlobalX);
        OffsetYBox.Text = Format(input.ProcessOffsetGlobalY);
    }

    private void LoadViewState()
    {
        try
        {
            if (!File.Exists(ViewStatePath))
            {
                return;
            }

            var state = JsonSerializer.Deserialize<ViewState>(File.ReadAllText(ViewStatePath));
            if (state is null)
            {
                return;
            }

            _boardZoom = Math.Clamp(state.BoardZoom, 0.35, 4.0);
            _matrixCellSize = Math.Clamp(state.MatrixCellSize, 48, 180);
        }
        catch
        {
            // View scale is a convenience setting only; invalid files should not block the demo.
        }
    }

    private void SaveViewState()
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(ViewStatePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var state = new ViewState
            {
                BoardZoom = _boardZoom,
                MatrixCellSize = _matrixCellSize
            };
            File.WriteAllText(ViewStatePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Saving the last zoom level is non-critical; keep UI interaction uninterrupted.
        }
    }

    private CoordinateInput ReadInputFromScreen()
    {
        var columns = ReadInt(CellColumnsBox, _input.CellColumns);
        var rows = ReadInt(CellRowsBox, _input.CellRows);
        var blockColumns = ReadInt(CellBlockColumnsBox, _input.CellBlockColumns);
        var blockRows = ReadInt(CellBlockRowsBox, _input.CellBlockRows);
        var scannerCount = ReadInt(ScannerCountBox, _input.ScannerCount);

        return new CoordinateInput
        {
            BoardSizeX = ReadDouble(BoardXBox, _input.BoardSizeX),
            BoardSizeY = ReadDouble(BoardYBox, _input.BoardSizeY),
            AlignMarginX = ReadDouble(AkMarginXBox, _input.AlignMarginX),
            AlignMarginY = ReadDouble(AkMarginYBox, _input.AlignMarginY),
            HomeStageY = ReadDouble(HomeStageYBox, _input.HomeStageY),
            ForwardTransportSignY = ReadSignedInt(ForwardTransportSignYBox, _input.ForwardTransportSignY) >= 0 ? 1 : -1,
            ReviewCenterGlobalX = ReadDouble(ReviewCenterXBox, _input.ReviewCenterGlobalX),
            ReviewCenterGlobalY = ReadDouble(ReviewCenterYBox, _input.ReviewCenterGlobalY),
            ReviewPixelCenterU = ReadDouble(U0Box, _input.ReviewPixelCenterU),
            ReviewPixelCenterV = ReadDouble(V0Box, _input.ReviewPixelCenterV),
            PixelScaleX = ReadDouble(ScaleXBox, _input.PixelScaleX),
            PixelScaleY = ReadDouble(ScaleYBox, _input.PixelScaleY),
            MeasuredAk1U = ReadDouble(Ak1UBox, _input.MeasuredAk1U),
            MeasuredAk1V = ReadDouble(Ak1VBox, _input.MeasuredAk1V),
            ThetaAlignDeg = ReadDouble(ThetaBox, _input.ThetaAlignDeg),
            CellFirstX = ReadDouble(CellFirstXBox, _input.CellFirstX),
            CellFirstY = ReadDouble(CellFirstYBox, _input.CellFirstY),
            CellPitchX = ReadDouble(CellPitchXBox, _input.CellPitchX),
            CellPitchY = ReadDouble(CellPitchYBox, _input.CellPitchY),
            PatternOffsetX = ReadDouble(PatternOffsetXBox, _input.PatternOffsetX),
            PatternOffsetY = ReadDouble(PatternOffsetYBox, _input.PatternOffsetY),
            CellColumns = columns,
            CellRows = rows,
            CellBlockColumns = blockColumns,
            CellBlockRows = blockRows,
            CellBlockPitchX = ReadDouble(CellBlockPitchXBox, _input.CellBlockPitchX),
            CellBlockPitchY = ReadDouble(CellBlockPitchYBox, _input.CellBlockPitchY),
            SelectedCellBlock = Clamp(ReadInt(SelectedCellBlockBox, _input.SelectedCellBlock), 1, blockColumns * blockRows),
            SelectedCellColumn = Clamp(ReadZeroBasedInt(SelectedCellColumnBox, _input.SelectedCellColumn), 0, Math.Max(0, columns - 1)),
            SelectedCellRow = Clamp(ReadZeroBasedInt(SelectedCellRowBox, _input.SelectedCellRow), 0, Math.Max(0, rows - 1)),
            ScannerCount = scannerCount,
            HighlightScannerHeads = HighlightHeadsBox.Text,
            FirstScannerInitialStageX = ReadDouble(FirstScannerInitialXBox, _input.FirstScannerInitialStageX),
            FirstScannerInitialStageY = ReadDouble(FirstScannerInitialYBox, _input.FirstScannerInitialStageY),
            ScannerOriginTolerance = Math.Abs(ReadDouble(ScannerOriginToleranceBox, _input.ScannerOriginTolerance)),
            ReviewToFirstScannerOffsetX = ReadDouble(CameraToScannerOffsetXBox, _input.ReviewToFirstScannerOffsetX),
            ReviewToFirstScannerOffsetY = ReadDouble(CameraToScannerOffsetYBox, _input.ReviewToFirstScannerOffsetY),
            ScannerPitchX = ReadDouble(ScannerPitchXBox, _input.ScannerPitchX),
            EvenScannerYOffset = ReadDouble(EvenYOffsetBox, _input.EvenScannerYOffset),
            ScannerFieldHalfX = ReadDouble(FieldHalfXBox, _input.ScannerFieldHalfX),
            ScannerFieldHalfY = ReadDouble(FieldHalfYBox, _input.ScannerFieldHalfY),
            ReviewBasisScannerHead = Clamp(ReadInt(ReviewBasisHeadBox, _input.ReviewBasisScannerHead), 1, scannerCount),
            ReviewBasisDoeBeam = Clamp(ReadInt(ReviewBasisBeamBox, _input.ReviewBasisDoeBeam), 1, 16),
            DoeBeamPitchX = ReadDouble(DoePitchXBox, _input.DoeBeamPitchX),
            DoeBeamPitchY = ReadDouble(DoePitchYBox, _input.DoeBeamPitchY),
            ProcessOffsetGlobalX = ReadDouble(OffsetXBox, _input.ProcessOffsetGlobalX),
            ProcessOffsetGlobalY = ReadDouble(OffsetYBox, _input.ProcessOffsetGlobalY)
        };
    }

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    private static string ToColumnLetter(int zeroBasedColumn)
    {
        var value = zeroBasedColumn + 1;
        var text = "";
        while (value > 0)
        {
            value--;
            text = (char)('A' + value % 26) + text;
            value /= 26;
        }

        return text;
    }

    private static int ColumnLetterToIndex(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "Cell#")
        {
            return -1;
        }

        var value = 0;
        foreach (var ch in text.Trim().ToUpperInvariant())
        {
            if (ch < 'A' || ch > 'Z')
            {
                return -1;
            }

            value = value * 26 + (ch - 'A' + 1);
        }

        return value - 1;
    }

    private static string PointKey(CellCommand command) => $"{command.CellBlock}:{command.Column}:{command.Row}";

    private bool IsPointSelected(CellCommand command) => _selectedPointKeys.Contains(PointKey(command));

    private IReadOnlyList<CellCommand> GetVisibleMatrixCommands()
    {
        if (_lastResult is null)
        {
            return Array.Empty<CellCommand>();
        }

        var selectedHeads = ParseScannerHeadSet(_input.HighlightScannerHeads);
        if (selectedHeads.Count == 0)
        {
            return _lastResult.Commands;
        }

        return _lastResult.Commands.Where(x => x.IsHighlightedScanner).ToList();
    }

    private static HashSet<int> ParseScannerHeadSet(string text)
    {
        var heads = new HashSet<int>();
        foreach (var token in text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var head) && head > 0)
            {
                heads.Add(head);
            }
        }

        return heads;
    }

    private void ResetSelectionFromInput()
    {
        _selectedPointKeys.Clear();
        _selectedPointKeys.Add($"{_input.SelectedCellBlock}:{_input.SelectedCellColumn}:{_input.SelectedCellRow}");
        _selectionAnchor = _lastResult?.Commands.FirstOrDefault(x =>
            x.CellBlock == _input.SelectedCellBlock &&
            x.Column == _input.SelectedCellColumn &&
            x.Row == _input.SelectedCellRow);
    }

    private static double EffectiveBlockPitchX(CoordinateInput input)
    {
        if (input.CellBlockPitchX > 0)
        {
            return input.CellBlockPitchX;
        }

        return Math.Max(1, input.CellColumns) * Math.Max(1, input.CellPitchX) + Math.Max(1, input.CellPitchX);
    }

    private static double EffectiveBlockPitchY(CoordinateInput input)
    {
        if (input.CellBlockPitchY > 0)
        {
            return input.CellBlockPitchY;
        }

        return Math.Max(1, input.CellRows) * Math.Max(1, input.CellPitchY) + Math.Max(1, input.CellPitchY);
    }

    private void ApplyConfigCsv(string path)
    {
        var loadedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',', 2);
            if (parts.Length != 2 || parts[0].Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var key = parts[0].Trim();
            loadedKeys.Add(key);
            ApplyConfigValue(key, parts[1].Trim());
        }

        var hasExplicitFirstScannerX = loadedKeys.Contains(nameof(CoordinateInput.FirstScannerInitialStageX)) || loadedKeys.Contains("FirstScannerCenterX");
        var hasExplicitFirstScannerY = loadedKeys.Contains(nameof(CoordinateInput.FirstScannerInitialStageY)) || loadedKeys.Contains("FirstScannerCenterY");
        var hasPhysicalOffsetX = loadedKeys.Contains(nameof(CoordinateInput.ReviewToFirstScannerOffsetX));
        var hasPhysicalOffsetY = loadedKeys.Contains(nameof(CoordinateInput.ReviewToFirstScannerOffsetY));

        // New CSV files may provide both values for consistency validation. Older files
        // usually provide only one side, so derive the missing side after all rows load.
        if (!hasExplicitFirstScannerX)
        {
            _input.FirstScannerInitialStageX = _input.ReviewCenterGlobalX + _input.ReviewToFirstScannerOffsetX;
        }
        else if (!hasPhysicalOffsetX)
        {
            _input.ReviewToFirstScannerOffsetX = _input.FirstScannerInitialStageX - _input.ReviewCenterGlobalX;
        }

        if (!hasExplicitFirstScannerY)
        {
            _input.FirstScannerInitialStageY = _input.ReviewCenterGlobalY + _input.ReviewToFirstScannerOffsetY;
        }
        else if (!hasPhysicalOffsetY)
        {
            _input.ReviewToFirstScannerOffsetY = _input.FirstScannerInitialStageY - _input.ReviewCenterGlobalY;
        }
    }

    private void SaveConfigCsv(string path)
    {
        var lines = new[]
        {
            "Key,Value",
            CsvLine(nameof(CoordinateInput.BoardSizeX), _input.BoardSizeX),
            CsvLine(nameof(CoordinateInput.BoardSizeY), _input.BoardSizeY),
            CsvLine(nameof(CoordinateInput.AlignMarginX), _input.AlignMarginX),
            CsvLine(nameof(CoordinateInput.AlignMarginY), _input.AlignMarginY),
            CsvLine(nameof(CoordinateInput.HomeStageY), _input.HomeStageY),
            CsvLine(nameof(CoordinateInput.ForwardTransportSignY), _input.ForwardTransportSignY),
            CsvLine(nameof(CoordinateInput.ReviewCenterGlobalX), _input.ReviewCenterGlobalX),
            CsvLine(nameof(CoordinateInput.ReviewCenterGlobalY), _input.ReviewCenterGlobalY),
            CsvLine(nameof(CoordinateInput.ReviewPixelCenterU), _input.ReviewPixelCenterU),
            CsvLine(nameof(CoordinateInput.ReviewPixelCenterV), _input.ReviewPixelCenterV),
            CsvLine(nameof(CoordinateInput.PixelScaleX), _input.PixelScaleX),
            CsvLine(nameof(CoordinateInput.PixelScaleY), _input.PixelScaleY),
            CsvLine(nameof(CoordinateInput.MeasuredAk1U), _input.MeasuredAk1U),
            CsvLine(nameof(CoordinateInput.MeasuredAk1V), _input.MeasuredAk1V),
            CsvLine(nameof(CoordinateInput.ThetaAlignDeg), _input.ThetaAlignDeg),
            CsvLine(nameof(CoordinateInput.CellFirstX), _input.CellFirstX),
            CsvLine(nameof(CoordinateInput.CellFirstY), _input.CellFirstY),
            CsvLine(nameof(CoordinateInput.CellPitchX), _input.CellPitchX),
            CsvLine(nameof(CoordinateInput.CellPitchY), _input.CellPitchY),
            CsvLine(nameof(CoordinateInput.PatternOffsetX), _input.PatternOffsetX),
            CsvLine(nameof(CoordinateInput.PatternOffsetY), _input.PatternOffsetY),
            CsvLine(nameof(CoordinateInput.CellColumns), _input.CellColumns),
            CsvLine(nameof(CoordinateInput.CellRows), _input.CellRows),
            CsvLine(nameof(CoordinateInput.CellBlockColumns), _input.CellBlockColumns),
            CsvLine(nameof(CoordinateInput.CellBlockRows), _input.CellBlockRows),
            CsvLine(nameof(CoordinateInput.CellBlockPitchX), _input.CellBlockPitchX),
            CsvLine(nameof(CoordinateInput.CellBlockPitchY), _input.CellBlockPitchY),
            CsvLine(nameof(CoordinateInput.ScannerCount), _input.ScannerCount),
            CsvLine(nameof(CoordinateInput.HighlightScannerHeads), _input.HighlightScannerHeads),
            CsvLine(nameof(CoordinateInput.FirstScannerInitialStageX), _input.FirstScannerInitialStageX),
            CsvLine(nameof(CoordinateInput.FirstScannerInitialStageY), _input.FirstScannerInitialStageY),
            CsvLine(nameof(CoordinateInput.ScannerOriginTolerance), _input.ScannerOriginTolerance),
            CsvLine(nameof(CoordinateInput.ReviewToFirstScannerOffsetX), _input.ReviewToFirstScannerOffsetX),
            CsvLine(nameof(CoordinateInput.ReviewToFirstScannerOffsetY), _input.ReviewToFirstScannerOffsetY),
            CsvLine(nameof(CoordinateInput.ScannerPitchX), _input.ScannerPitchX),
            CsvLine(nameof(CoordinateInput.EvenScannerYOffset), _input.EvenScannerYOffset),
            CsvLine(nameof(CoordinateInput.ScannerFieldHalfX), _input.ScannerFieldHalfX),
            CsvLine(nameof(CoordinateInput.ScannerFieldHalfY), _input.ScannerFieldHalfY),
            CsvLine(nameof(CoordinateInput.ReviewBasisScannerHead), _input.ReviewBasisScannerHead),
            CsvLine(nameof(CoordinateInput.ReviewBasisDoeBeam), _input.ReviewBasisDoeBeam),
            CsvLine(nameof(CoordinateInput.DoeBeamPitchX), _input.DoeBeamPitchX),
            CsvLine(nameof(CoordinateInput.DoeBeamPitchY), _input.DoeBeamPitchY),
            CsvLine(nameof(CoordinateInput.ProcessOffsetGlobalX), _input.ProcessOffsetGlobalX),
            CsvLine(nameof(CoordinateInput.ProcessOffsetGlobalY), _input.ProcessOffsetGlobalY)
        };

        File.WriteAllLines(path, lines);
    }

    private static string CsvLine(string key, double value) => $"{key},{Format(value)}";

    private static string CsvLine(string key, int value) => $"{key},{value.ToString(CultureInfo.InvariantCulture)}";

    private static string CsvLine(string key, string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"{key},\"{escaped}\"";
    }

    private void ApplyConfigValue(string key, string value)
    {
        value = value.Trim().Trim('"');
        var number = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
        var integer = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : (int)Math.Round(number);

        switch (key)
        {
            case nameof(CoordinateInput.BoardSizeX): _input.BoardSizeX = number; break;
            case nameof(CoordinateInput.BoardSizeY): _input.BoardSizeY = number; break;
            case nameof(CoordinateInput.AlignMarginX): _input.AlignMarginX = number; break;
            case nameof(CoordinateInput.AlignMarginY): _input.AlignMarginY = number; break;
            case nameof(CoordinateInput.HomeStageY): _input.HomeStageY = number; break;
            case nameof(CoordinateInput.ForwardTransportSignY): _input.ForwardTransportSignY = integer >= 0 ? 1 : -1; break;
            case nameof(CoordinateInput.ReviewCenterGlobalX): _input.ReviewCenterGlobalX = number; break;
            case nameof(CoordinateInput.ReviewCenterGlobalY): _input.ReviewCenterGlobalY = number; break;
            case nameof(CoordinateInput.ReviewPixelCenterU): _input.ReviewPixelCenterU = number; break;
            case nameof(CoordinateInput.ReviewPixelCenterV): _input.ReviewPixelCenterV = number; break;
            case nameof(CoordinateInput.PixelScaleX): _input.PixelScaleX = number; break;
            case nameof(CoordinateInput.PixelScaleY): _input.PixelScaleY = number; break;
            case nameof(CoordinateInput.MeasuredAk1U): _input.MeasuredAk1U = number; break;
            case nameof(CoordinateInput.MeasuredAk1V): _input.MeasuredAk1V = number; break;
            case nameof(CoordinateInput.ThetaAlignDeg): _input.ThetaAlignDeg = number; break;
            case nameof(CoordinateInput.CellFirstX): _input.CellFirstX = number; break;
            case nameof(CoordinateInput.CellFirstY): _input.CellFirstY = number; break;
            case nameof(CoordinateInput.CellPitchX): _input.CellPitchX = number; break;
            case nameof(CoordinateInput.CellPitchY): _input.CellPitchY = number; break;
            case nameof(CoordinateInput.PatternOffsetX): _input.PatternOffsetX = number; break;
            case nameof(CoordinateInput.PatternOffsetY): _input.PatternOffsetY = number; break;
            case nameof(CoordinateInput.CellColumns): _input.CellColumns = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellRows): _input.CellRows = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellBlockColumns): _input.CellBlockColumns = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellBlockRows): _input.CellBlockRows = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellBlockPitchX): _input.CellBlockPitchX = number; break;
            case nameof(CoordinateInput.CellBlockPitchY): _input.CellBlockPitchY = number; break;
            case nameof(CoordinateInput.ScannerCount): _input.ScannerCount = Math.Max(1, integer); break;
            case nameof(CoordinateInput.HighlightScannerHeads): _input.HighlightScannerHeads = value; break;
            case nameof(CoordinateInput.FirstScannerInitialStageX): _input.FirstScannerInitialStageX = number; break;
            case nameof(CoordinateInput.FirstScannerInitialStageY): _input.FirstScannerInitialStageY = number; break;
            case nameof(CoordinateInput.ScannerOriginTolerance): _input.ScannerOriginTolerance = Math.Abs(number); break;
            case nameof(CoordinateInput.ReviewToFirstScannerOffsetX): _input.ReviewToFirstScannerOffsetX = number; break;
            case nameof(CoordinateInput.ReviewToFirstScannerOffsetY): _input.ReviewToFirstScannerOffsetY = number; break;
            // Backward compatibility for CSV files created before the physical offset was explicit.
            case "FirstScannerCenterX": _input.FirstScannerInitialStageX = number; break;
            case "FirstScannerCenterY": _input.FirstScannerInitialStageY = number; break;
            case nameof(CoordinateInput.ScannerPitchX): _input.ScannerPitchX = number; break;
            case nameof(CoordinateInput.EvenScannerYOffset): _input.EvenScannerYOffset = number; break;
            case nameof(CoordinateInput.ScannerFieldHalfX): _input.ScannerFieldHalfX = number; break;
            case nameof(CoordinateInput.ScannerFieldHalfY): _input.ScannerFieldHalfY = number; break;
            case nameof(CoordinateInput.ReviewBasisScannerHead): _input.ReviewBasisScannerHead = Math.Max(1, integer); break;
            case nameof(CoordinateInput.ReviewBasisDoeBeam): _input.ReviewBasisDoeBeam = Clamp(integer, 1, 16); break;
            case nameof(CoordinateInput.DoeBeamPitchX): _input.DoeBeamPitchX = number; break;
            case nameof(CoordinateInput.DoeBeamPitchY): _input.DoeBeamPitchY = number; break;
            case nameof(CoordinateInput.ProcessOffsetGlobalX): _input.ProcessOffsetGlobalX = number; break;
            case nameof(CoordinateInput.ProcessOffsetGlobalY): _input.ProcessOffsetGlobalY = number; break;
        }
    }

    private static double ReadDouble(TextBox textBox, double fallback)
    {
        if (double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        textBox.Text = Format(fallback);
        return fallback;
    }

    private static int ReadInt(TextBox textBox, int fallback)
    {
        if (int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Math.Max(1, value);
        }

        textBox.Text = fallback.ToString(CultureInfo.InvariantCulture);
        return fallback;
    }

    private static int ReadSignedInt(TextBox textBox, int fallback)
    {
        if (int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        textBox.Text = fallback.ToString(CultureInfo.InvariantCulture);
        return fallback;
    }

    private static int ReadZeroBasedInt(TextBox textBox, int fallback)
    {
        if (int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Math.Max(0, value);
        }

        textBox.Text = fallback.ToString(CultureInfo.InvariantCulture);
        return fallback;
    }

    private sealed class ViewState
    {
        public double BoardZoom { get; set; } = 1.0;
        public double MatrixCellSize { get; set; } = 86;
    }
}
