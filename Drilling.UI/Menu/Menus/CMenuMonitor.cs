using System.Globalization;
using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Product;
using Drilling.Common.Station;
using System.Windows.Media;

namespace Drilling.UI.Menu.Menus;

public sealed class CMenuMonitor : IMenu
{
    private readonly IInterfaceManager _interfaceManager;
    private readonly IMotionManager _motionManager;
    private readonly CInterLockManager _interLockManager;
    private readonly IProductManager _productManager;
    private readonly Func<string> _selectedTabAccessor;
    private readonly Action<string> _selectedTabSetter;
    private readonly Action<string> _setStatusMessage;
    private readonly Action _refreshShellStatus;
    private readonly Func<Task> _refreshCurrentScreen;
    private string _selectedAxisId = "GX";
    private string _selectedPowerMeterProcessName = "";

    private static readonly string[] MonitorTabs =
    [
        "IO",
        "MOTOR",
        "LASER",
        "CHILLER",
        "ATTENUATOR",
        "BET",
        "POWER METER",
        "PRODUCT",
        "ETC"
    ];

    public CMenuMonitor(
        IInterfaceManager interfaceManager,
        IMotionManager motionManager,
        CInterLockManager interLockManager,
        IProductManager productManager,
        Func<string> selectedTabAccessor,
        Action<string> selectedTabSetter,
        Action<string> setStatusMessage,
        Action refreshShellStatus,
        Func<Task> refreshCurrentScreen)
    {
        _interfaceManager = interfaceManager;
        _motionManager = motionManager;
        _interLockManager = interLockManager;
        _productManager = productManager;
        _selectedTabAccessor = selectedTabAccessor;
        _selectedTabSetter = selectedTabSetter;
        _setStatusMessage = setStatusMessage;
        _refreshShellStatus = refreshShellStatus;
        _refreshCurrentScreen = refreshCurrentScreen;

        SelectTabCommand = new CButtonCommand(async parameter => await SelectTab(parameter));
        ExecuteOperationCommand = new CButtonCommand(async parameter => await ExecuteOperation(parameter));
        SetOutputOnCommand = new CButtonCommand(async parameter => await SetOutput(parameter, true));
        SetOutputOffCommand = new CButtonCommand(async parameter => await SetOutput(parameter, false));
    }

    public EN_MENU Menu => EN_MENU.Monitor;

    public IReadOnlyList<ST_SCREEN_SECTION> DeviceTabs { get; private set; } = [];

    public string SelectedTab { get; private set; } = "IO";

    public string Title { get; private set; } = "";

    public string Subtitle { get; private set; } = "";

    public string StatusPanelTitle { get; private set; } = "";

    public string OperationPanelTitle { get; private set; } = "";

    public string ParameterPanelTitle { get; private set; } = "";

    public string TrendPanelTitle { get; private set; } = "";

    public string HistoryPanelTitle { get; private set; } = "";

    public IReadOnlyList<ST_MONITOR_TAB> Tabs { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_IO_ROW> InputRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_IO_ROW> OutputRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_AXIS_ROW> AxisRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_COMMAND_HISTORY_ROW> CommandHistoryRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_STATUS_ROW> StatusRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> OperationButtons { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_PARAMETER_ROW> OperationFields { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_PARAMETER_ROW> ParameterRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_BET_TABLE_ROW> BetTableRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_TREND_POINT> TrendPoints { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_SUMMARY_ITEM> SummaryItems { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_POSITION_ROW> PositionRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_PRODUCT_ITEM> ProductItems { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_PRODUCT_HEAD_ROW> ProductHeadRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_PRODUCT_HISTORY_ROW> ProductHistoryRows { get; private set; } = [];

    public IReadOnlyList<ST_PWM_PROCESS_ROW> PwmProcessRows { get; private set; } = [];

    public IReadOnlyList<ST_PWM_STEP_ROW> PwmStepRows { get; private set; } = [];

    public IReadOnlyList<ST_PWM_SETTING_ROW> PwmSettingRows { get; private set; } = [];

    public IReadOnlyList<ST_PWM_DEVICE_ROW> PwmDeviceRows { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> PwmProcessButtons { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> PwmStepButtons { get; private set; } = [];

    public IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> PwmRunButtons { get; private set; } = [];

    public CButtonCommand SelectTabCommand { get; }

    public CButtonCommand ExecuteOperationCommand { get; }

    public CButtonCommand SetOutputOnCommand { get; }

    public CButtonCommand SetOutputOffCommand { get; }

    public string SelectedAxisId => _selectedAxisId;

    public ST_MONITOR_AXIS_ROW? SelectedAxisRow
    {
        get => AxisRows.FirstOrDefault(row => row.Axis.Equals(_selectedAxisId, StringComparison.OrdinalIgnoreCase));
        set
        {
            if (value is not null && !string.IsNullOrWhiteSpace(value.Axis))
            {
                _selectedAxisId = value.Axis;
            }
        }
    }

    public bool IsIo => SelectedTab == "IO";

    public bool IsMotor => SelectedTab == "MOTOR";

    public bool IsLaser => SelectedTab == "LASER";

    public bool IsChiller => SelectedTab == "CHILLER";

    public bool IsAttenuator => SelectedTab == "ATTENUATOR";

    public bool IsBet => SelectedTab == "BET";

    public bool IsPowerMeter => SelectedTab == "POWER METER";

    public bool IsProduct => SelectedTab == "PRODUCT";

    public bool IsControlDevice => IsLaser || IsChiller;

    public bool IsGenericDevice => !IsIo && !IsMotor && !IsLaser && !IsChiller && !IsAttenuator && !IsBet && !IsPowerMeter && !IsProduct;

    public async Task<CScreenViewModel> Build(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetDeviceStatus(cancellationToken);
        var communication = await _interfaceManager.GetCommunicationStatus(cancellationToken);
        var selectedTab = NormalizeMonitorTab(_selectedTabAccessor());
        var selectedModule = GetMonitorModule(selectedTab);
        var interfaceHistory = selectedModule is null
            ? []
            : await _interfaceManager.ReadInterfaceHistory(
                selectedModule.Value,
                maxRows: 40,
                cancellationToken: cancellationToken);
        var betTable = selectedTab == "BET"
            ? await _interfaceManager.LoadBETData(cancellationToken)
            : [];
        var powerMeterTable = selectedTab == "POWER METER"
            ? await _interfaceManager.LoadPowerMeterData(_selectedPowerMeterProcessName, cancellationToken)
            : ST_POWER_METER_TABLE_DATA.Empty;
        if (selectedTab == "POWER METER")
        {
            _selectedPowerMeterProcessName = powerMeterTable.SelectedFileName;
        }
        var (product, productHistory, productError) = selectedTab == "PRODUCT"
            ? await LoadProductDisplay(cancellationToken)
            : (null, [], "");
        var tabs = MonitorTabs
            .Select(tab => new ST_MONITOR_TAB(tab, tab == selectedTab))
            .ToArray();

        var axisRows = CreateAxisRows(snapshot, _selectedAxisId);

        if (axisRows.Count > 0 && !axisRows.Any(row => row.IsSelected))
        {
            _selectedAxisId = axisRows[0].Axis;
            axisRows = CreateAxisRows(snapshot, _selectedAxisId);
        }

        Apply(
            CreateLegacyTabs(tabs),
            selectedTab,
            $"MONITOR / {selectedTab}",
            GetSubtitle(selectedTab),
            GetStatusPanelTitle(selectedTab),
            GetOperationPanelTitle(selectedTab),
            GetParameterPanelTitle(selectedTab),
            GetTrendPanelTitle(selectedTab),
            GetHistoryPanelTitle(selectedTab),
            tabs,
            CreateInputRows(snapshot),
            CreateOutputRows(snapshot),
            axisRows,
            CreateCommandHistoryRows(selectedTab, interfaceHistory),
            CreateStatusRows(selectedTab, snapshot, communication),
            CreateOperationButtons(selectedTab),
            CreateOperationFields(selectedTab),
            CreateParameterRows(selectedTab),
            CreateBetTableRows(selectedTab, betTable, snapshot),
            CreateTrendPoints(selectedTab),
            CreateSummaryItems(selectedTab, snapshot),
            CreatePositionRows(selectedTab, snapshot),
            CreateProductItems(product, productError),
            CreateProductHeadRows(product),
            CreateProductHistoryRows(productHistory),
            CreatePwmProcessRows(selectedTab, powerMeterTable),
            CreatePwmStepRows(selectedTab, snapshot, powerMeterTable),
            CreatePwmSettingRows(selectedTab, powerMeterTable),
            CreatePwmDeviceRows(selectedTab, snapshot),
            CreatePwmProcessButtons(selectedTab),
            CreatePwmStepButtons(selectedTab),
            CreatePwmRunButtons(selectedTab));

        return new CScreenViewModel(
            EN_MENU.Monitor,
            Title,
            Subtitle,
            [
                new("Tab", selectedTab),
                new("Input", InputRows.Count.ToString()),
                new("Output", OutputRows.Count.ToString())
            ],
            [
                new("IO", InputRows.Select(row => new ST_DISPLAY_ITEM(row.Address, row.State, row.Name)).ToArray()),
                new("MOTOR", AxisRows.Select(row => new ST_DISPLAY_ITEM(row.Axis, row.CurrentPosition, row.State)).ToArray()),
                new("LASER", StatusRows.Select(row => new ST_DISPLAY_ITEM(row.Item, row.Value, row.State)).ToArray())
            ],
            monitor: this);
    }

    private async Task<ST_DEVICE_STATUS> GetDeviceStatus(CancellationToken cancellationToken)
    {
        var io = await _motionManager.GetIoStatus(cancellationToken);
        var motors = await _motionManager.GetAxisStatus(cancellationToken);
        var laserStatus = await _interfaceManager.GetLaserStatus(cancellationToken);
        var chillerStatus = await _interfaceManager.GetChillerStatus(cancellationToken);
        var attenuatorStatus = await _interfaceManager.GetAttenuatorStatus(cancellationToken);
        var betStatus = await _interfaceManager.GetBETStatus(cancellationToken);
        var powerMeterStatus = await _interfaceManager.GetPowerMeterStatus(cancellationToken);

        return new ST_DEVICE_STATUS(
            io,
            motors,
            laserStatus,
            chillerStatus,
            attenuatorStatus,
            betStatus,
            powerMeterStatus);
    }

    private async Task SelectTab(object? parameter)
    {
        if (parameter is not string tab || string.IsNullOrWhiteSpace(tab))
        {
            return;
        }

        var selectedTab = NormalizeMonitorTab(tab);
        _selectedTabSetter(selectedTab);
        _setStatusMessage($"Monitor tab {selectedTab} selected.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task ExecuteOperation(object? parameter)
    {
        var label = GetMonitorOperationLabel(parameter);

        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        var result = SelectedTab switch
        {
            "MOTOR" => await ExecuteMotorOperation(label),
            "LASER" => await ExecuteLaserOperation(label),
            "CHILLER" => await ExecuteChillerOperation(label),
            "ATTENUATOR" => await ExecuteAttenuatorOperation(label),
            "BET" => await ExecuteBETOperation(label),
            "POWER METER" => await ExecutePowerMeterOperation(label),
            _ => new ST_DEVICE_COMMAND_RESULT(false, $"Monitor {SelectedTab} command is not connected yet: {label}")
        };

        _setStatusMessage(result.Message);

        if (SelectedTab is "MOTOR" or "LASER" or "CHILLER" or "ATTENUATOR" or "BET" or "POWER METER")
        {
            _refreshShellStatus();
            await _refreshCurrentScreen();
        }
    }

    private async Task SetOutput(
        object? parameter,
        bool isOn)
    {
        if (parameter is not string address || string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var result = await _motionManager.SetOutputCommand(address, isOn);
        _setStatusMessage(result.Message);
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task<ST_DEVICE_COMMAND_RESULT> ExecuteMotorOperation(string label)
    {
        var key = NormalizeMonitorOperation(label);

        if (key == "REFRESH")
        {
            return new ST_DEVICE_COMMAND_RESULT(
                true,
                $"Motion {_selectedAxisId} status refreshed.");
        }

        var command = key switch
        {
            "SERVOON" => (Command: EN_MOTION_COMMAND.ServoOn, Parameter: 0.0, Name: "SERVO ON"),
            "SERVOOFF" => (Command: EN_MOTION_COMMAND.ServoOff, Parameter: 0.0, Name: "SERVO OFF"),
            "HOME" => (Command: EN_MOTION_COMMAND.Home, Parameter: 0.0, Name: "HOME"),
            "ABSMOVE" => (Command: EN_MOTION_COMMAND.MoveAbs, Parameter: ReadOperationField("Target Position", 0.0), Name: "ABS MOVE"),
            "RELMOVE" => (Command: EN_MOTION_COMMAND.MoveRel, Parameter: ReadOperationField("Relative Distance", 0.0), Name: "REL MOVE"),
            "STOP" => (Command: EN_MOTION_COMMAND.Stop, Parameter: 0.0, Name: "STOP"),
            "RESETALARM" => (Command: EN_MOTION_COMMAND.ResetAlarm, Parameter: 0.0, Name: "RESET ALARM"),
            _ => (Command: EN_MOTION_COMMAND.Refresh, Parameter: 0.0, Name: "")
        };

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new ST_DEVICE_COMMAND_RESULT(false, $"Unknown motion monitor command: {label}");
        }

        if (command.Command is EN_MOTION_COMMAND.Home or EN_MOTION_COMMAND.MoveAbs or EN_MOTION_COMMAND.MoveRel)
        {
            var interLock = _interLockManager.Evaluate(await GetDeviceStatus(CancellationToken.None));

            if (!interLock.CanManualMove)
            {
                var blockedItem = interLock.Items.FirstOrDefault(item => item.Level != EN_INTERLOCK_LEVEL.Ok);
                var detail = blockedItem is null
                    ? "InterLock is not ready."
                    : $"{blockedItem.Signal}: {blockedItem.Detail}";

                return new ST_DEVICE_COMMAND_RESULT(
                    false,
                    $"Motion {_selectedAxisId} {command.Name} blocked by InterLock. {detail}");
            }
        }

        var result = await _motionManager.ExecuteMotionCommand(
            _selectedAxisId,
            command.Command,
            command.Parameter);

        var message = result.IsSuccess
            ? $"Motion {_selectedAxisId} {command.Name} command OK."
            : result.Message;

        return new ST_DEVICE_COMMAND_RESULT(result.IsSuccess, message);
    }

    private async Task<ST_DEVICE_COMMAND_RESULT> ExecuteLaserOperation(string label)
    {
        var key = NormalizeMonitorOperation(label);

        if (key == "REFRESH")
        {
            var status = await _interfaceManager.RefreshTalonLaserStatus();
            return new ST_DEVICE_COMMAND_RESULT(
                true,
                $"Talon laser refreshed. Power {status.OutputPower.ToString("F3", CultureInfo.InvariantCulture)} W.");
        }

        if (key == "RESETERROR")
        {
            return new ST_DEVICE_COMMAND_RESULT(
                false,
                "Talon reset error command is not defined in the Drilling Talon source.");
        }

        var command = key switch
        {
            "LASERON" => (Command: EN_TALON_COMMAND.SetLaserOnOff, Parameter: 1.0, Name: "LASER ON"),
            "LASEROFF" => (Command: EN_TALON_COMMAND.SetLaserOnOff, Parameter: 0.0, Name: "LASER OFF"),
            "GATEON" => (Command: EN_TALON_COMMAND.SetGateOpenClose, Parameter: 1.0, Name: "GATE ON"),
            "GATEOFF" => (Command: EN_TALON_COMMAND.SetGateOpenClose, Parameter: 0.0, Name: "GATE OFF"),
            "SHUTTEROPEN" => (Command: EN_TALON_COMMAND.SetShutterOpenClose, Parameter: 1.0, Name: "SHUTTER OPEN"),
            "SHUTTERCLOSE" => (Command: EN_TALON_COMMAND.SetShutterOpenClose, Parameter: 0.0, Name: "SHUTTER CLOSE"),
            _ => (Command: EN_TALON_COMMAND.RequestStatusString, Parameter: 0.0, Name: "")
        };

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new ST_DEVICE_COMMAND_RESULT(false, $"Unknown Talon monitor command: {label}");
        }

        var result = await _interfaceManager.ExecuteTalonLaserCommand(command.Command, command.Parameter);

        if (result.IsSuccess)
        {
            await _interfaceManager.RefreshTalonLaserStatus();
        }

        var message = result.IsSuccess
            ? $"Talon {command.Name} command OK. Response: {result.Message}"
            : $"Talon {command.Name} command failed. {result.Message}";

        return new ST_DEVICE_COMMAND_RESULT(result.IsSuccess, message);
    }

    private async Task<ST_DEVICE_COMMAND_RESULT> ExecuteChillerOperation(string label)
    {
        var key = NormalizeMonitorOperation(label);

        if (key == "REFRESH")
        {
            var status = await _interfaceManager.RefreshChillerStatus();
            return new ST_DEVICE_COMMAND_RESULT(
                true,
                $"Chiller refreshed. Temp {status.LiquidTempC.ToString("F1", CultureInfo.InvariantCulture)} C.");
        }

        var command = key switch
        {
            "RUN" => (Command: EN_CHILLER_COMMAND.Run, Parameter: 0.0, Name: "RUN"),
            "STOP" => (Command: EN_CHILLER_COMMAND.Stop, Parameter: 0.0, Name: "STOP"),
            "PUMPONLY" => (Command: EN_CHILLER_COMMAND.PumpOnly, Parameter: 0.0, Name: "PUMP ONLY"),
            "SETTEMP" => (Command: EN_CHILLER_COMMAND.SetTemperature, Parameter: ReadOperationField("Set Temperature", 22.0), Name: "SET TEMP"),
            "RESETALARM" => (Command: EN_CHILLER_COMMAND.ResetAlarm, Parameter: 0.0, Name: "RESET ALARM"),
            _ => (Command: EN_CHILLER_COMMAND.PollLiquidTemp, Parameter: 0.0, Name: "")
        };

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new ST_DEVICE_COMMAND_RESULT(false, $"Unknown Chiller monitor command: {label}");
        }

        var result = await _interfaceManager.ExecuteChillerCommand(command.Command, command.Parameter);

        if (result.IsSuccess)
        {
            await _interfaceManager.RefreshChillerStatus();
        }

        var message = result.IsSuccess
            ? $"Chiller {command.Name} command OK. Response: {result.Message}"
            : $"Chiller {command.Name} command failed. {result.Message}";

        return new ST_DEVICE_COMMAND_RESULT(result.IsSuccess, message);
    }

    private async Task<ST_DEVICE_COMMAND_RESULT> ExecuteAttenuatorOperation(string label)
    {
        var key = NormalizeMonitorOperation(label);

        if (key == "REFRESH")
        {
            var status = await _interfaceManager.RefreshAttenuatorStatus();
            return new ST_DEVICE_COMMAND_RESULT(
                true,
                $"CONEX_AGP refreshed. Position {status.CurrentPosition.ToString("F3", CultureInfo.InvariantCulture)} DEG.");
        }

        var command = key switch
        {
            "MOVEABS" => (Command: EN_ATTENUATOR_COMMAND.MoveAbs, Parameter: ReadOperationField("Target Position", 55.0), Name: "MOVE ABS"),
            "MOVEREL" => (Command: EN_ATTENUATOR_COMMAND.MoveRel, Parameter: ReadOperationField("Relative Move", 0.0), Name: "MOVE REL"),
            "HOME" => (Command: EN_ATTENUATOR_COMMAND.Home, Parameter: 0.0, Name: "HOME"),
            "STOP" => (Command: EN_ATTENUATOR_COMMAND.Stop, Parameter: 0.0, Name: "STOP"),
            "RESETALARM" => (Command: EN_ATTENUATOR_COMMAND.ResetAlarm, Parameter: 0.0, Name: "RESET ALARM"),
            _ => (Command: EN_ATTENUATOR_COMMAND.Refresh, Parameter: 0.0, Name: "")
        };

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new ST_DEVICE_COMMAND_RESULT(false, $"Unknown CONEX_AGP monitor command: {label}");
        }

        var result = await _interfaceManager.ExecuteAttenuatorCommand(command.Command, command.Parameter);

        if (result.IsSuccess)
        {
            await _interfaceManager.RefreshAttenuatorStatus();
        }

        var message = result.IsSuccess
            ? $"CONEX_AGP {command.Name} command OK. Response: {result.Message}"
            : $"CONEX_AGP {command.Name} command failed. {result.Message}";

        return new ST_DEVICE_COMMAND_RESULT(result.IsSuccess, message);
    }

    private async Task<ST_DEVICE_COMMAND_RESULT> ExecuteBETOperation(string label)
    {
        var key = NormalizeMonitorOperation(label);

        if (key == "REFRESH")
        {
            var status = await _interfaceManager.RefreshBETStatus();
            return new ST_DEVICE_COMMAND_RESULT(
                true,
                $"BET refreshed. MAG {status.CurrentMagnification.ToString("F3", CultureInfo.InvariantCulture)}, DIV {status.CurrentDivergence.ToString("F3", CultureInfo.InvariantCulture)}.");
        }

        var targetMag = ReadOperationField("Target Mag", 1.0);
        var targetDiv = ReadOperationField("Target Div", 1.0);
        var command = key switch
        {
            "SETMAG" or "SETDIV" => (Command: EN_BET_COMMAND.MoveManual, Parameter1: targetMag, Parameter2: targetDiv, Name: "SET BET"),
            "HOME" => (Command: EN_BET_COMMAND.InitMotor, Parameter1: 0.0, Parameter2: 0.0, Name: "HOME"),
            "STOP" => (Command: EN_BET_COMMAND.Stop, Parameter1: 0.0, Parameter2: 0.0, Name: "STOP"),
            "RESETALARM" => (Command: EN_BET_COMMAND.ResetAlarm, Parameter1: 0.0, Parameter2: 0.0, Name: "RESET ALARM"),
            _ => (Command: EN_BET_COMMAND.Refresh, Parameter1: 0.0, Parameter2: 0.0, Name: "")
        };

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new ST_DEVICE_COMMAND_RESULT(false, $"Unknown BET monitor command: {label}");
        }

        var result = await _interfaceManager.ExecuteBETCommand(command.Command, command.Parameter1, command.Parameter2);

        if (result.IsSuccess)
        {
            await _interfaceManager.RefreshBETStatus();
        }

        var message = result.IsSuccess
            ? $"BET {command.Name} command OK. Response: {result.Message}"
            : $"BET {command.Name} command failed. {result.Message}";

        return new ST_DEVICE_COMMAND_RESULT(result.IsSuccess, message);
    }

    private async Task<ST_DEVICE_COMMAND_RESULT> ExecutePowerMeterOperation(string label)
    {
        var key = NormalizeMonitorOperation(label);

        if (key == "REFRESH")
        {
            var status = await _interfaceManager.RefreshPowerMeterStatus();
            return new ST_DEVICE_COMMAND_RESULT(
                true,
                $"PowerMeter refreshed. Power {status.MeasuredPower.ToString("F4", CultureInfo.InvariantCulture)} {status.Unit}.");
        }

        if (key is "CREATE" or "DELETE" or "RENAME" or "MODIFY" or "ADD" or "WITH" or "DELETEALL")
        {
            return new ST_DEVICE_COMMAND_RESULT(true, $"PowerMeter table command queued: {label}");
        }

        if (key == "START")
        {
            return await RunPowerMeterMeasureSequence();
        }

        if (key == "STOP")
        {
            var stopResult = await _interfaceManager.ExecutePowerMeterCommand(EN_POWER_METER_COMMAND.StopStreaming);
            return stopResult.IsSuccess
                ? new ST_DEVICE_COMMAND_RESULT(true, "PowerMeter measure sequence stopped.")
                : new ST_DEVICE_COMMAND_RESULT(false, $"PowerMeter stop failed. {stopResult.Message}");
        }

        var command = key switch
        {
            "GETPOWER" or "READPOWER" => (Command: EN_POWER_METER_COMMAND.ReadPower, Parameter: 0.0, Name: "GET POWER"),
            "GETSERIAL" => (Command: EN_POWER_METER_COMMAND.QuerySerialNumber, Parameter: 0.0, Name: "GET SERIAL"),
            "GETWAVELENGTH" => (Command: EN_POWER_METER_COMMAND.QueryWaveLength, Parameter: 0.0, Name: "GET WAVELENGTH"),
            "SETWAVELENGTH" or "SETWAVE" => (Command: EN_POWER_METER_COMMAND.SetWaveLength, Parameter: ReadPwmSetting("WAVELENGTH", 355.0), Name: "SET WAVELENGTH"),
            "GETBEAMPOS" or "QUERYPOS" => (Command: EN_POWER_METER_COMMAND.QueryBeamPosition, Parameter: 0.0, Name: "GET BEAM POS"),
            "RESET" => (Command: EN_POWER_METER_COMMAND.Reset, Parameter: 0.0, Name: "RESET"),
            _ => (Command: EN_POWER_METER_COMMAND.Refresh, Parameter: 0.0, Name: "")
        };

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new ST_DEVICE_COMMAND_RESULT(false, $"Unknown PowerMeter monitor command: {label}");
        }

        var result = await _interfaceManager.ExecutePowerMeterCommand(command.Command, command.Parameter);

        if (result.IsSuccess)
        {
            await _interfaceManager.RefreshPowerMeterStatus();
        }

        var message = result.IsSuccess
            ? $"PowerMeter {command.Name} command OK. Response: {result.Message}"
            : $"PowerMeter {command.Name} command failed. {result.Message}";

        return new ST_DEVICE_COMMAND_RESULT(result.IsSuccess, message);
    }

    private async Task<ST_DEVICE_COMMAND_RESULT> RunPowerMeterMeasureSequence()
    {
        var table = await _interfaceManager.LoadPowerMeterData(_selectedPowerMeterProcessName);

        if (table.Steps.Count == 0)
        {
            return new ST_DEVICE_COMMAND_RESULT(false, "PowerMeter measure step is empty.");
        }

        _selectedPowerMeterProcessName = table.SelectedFileName;

        var waveLength = ReadPwmSetting("WAVELENGTH", 355.0);
        var waveLengthResult = await _interfaceManager.ExecutePowerMeterCommand(
            EN_POWER_METER_COMMAND.SetWaveLength,
            waveLength);

        if (!waveLengthResult.IsSuccess)
        {
            return new ST_DEVICE_COMMAND_RESULT(false, $"PowerMeter wavelength set failed. {waveLengthResult.Message}");
        }

        var updatedSteps = new List<ST_POWER_METER_STEP_DATA>();
        var measuredCount = 0;
        var lastPower = 0.0;

        foreach (var step in table.Steps)
        {
            if (!step.PowerOut)
            {
                updatedSteps.Add(step with { State = "SKIP" });
                continue;
            }

            var cycleCount = Math.Max(1, step.MeasureCycle);
            ST_POWER_METER_STATUS status = ST_POWER_METER_STATUS.Empty;

            for (var cycle = 0; cycle < cycleCount; cycle++)
            {
                var result = await _interfaceManager.ExecutePowerMeterCommand(EN_POWER_METER_COMMAND.ReadPower);

                if (!result.IsSuccess)
                {
                    updatedSteps.Add(step with { State = "ERROR" });
                    await _interfaceManager.SavePowerMeterData(table.SelectedFileName, updatedSteps.Concat(table.Steps.Skip(updatedSteps.Count)).ToArray());
                    return new ST_DEVICE_COMMAND_RESULT(false, $"PowerMeter read failed at step {step.StepNo:000}. {result.Message}");
                }

                status = await _interfaceManager.GetPowerMeterStatus();
            }

            lastPower = status.MeasuredPower;
            measuredCount++;
            updatedSteps.Add(step with
            {
                MeasurePower = status.MeasuredPower,
                State = "OK"
            });
        }

        await _interfaceManager.SavePowerMeterData(table.SelectedFileName, updatedSteps);
        await _refreshCurrentScreen();

        return new ST_DEVICE_COMMAND_RESULT(
            true,
            $"PowerMeter measure completed. Step={measuredCount}, LastPower={lastPower.ToString("F4", CultureInfo.InvariantCulture)} W.");
    }

    private double ReadOperationField(string parameter, double defaultValue)
    {
        var value = OperationFields
            .FirstOrDefault(field => field.Parameter.Equals(parameter, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : defaultValue;
    }

    private double ReadPwmSetting(string parameter, double defaultValue)
    {
        var value = PwmSettingRows
            .FirstOrDefault(row => row.Parameter.Equals(parameter, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : defaultValue;
    }

    private static string GetMonitorOperationLabel(object? parameter)
    {
        return parameter switch
        {
            ST_MONITOR_OPERATION_BUTTON button => button.Label,
            string text => text,
            _ => ""
        };
    }

    private static string NormalizeMonitorOperation(string label)
    {
        return label.Replace("\r", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\n", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("_", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToUpperInvariant();
    }

    private void Apply(
        IReadOnlyList<ST_SCREEN_SECTION> deviceTabs,
        string selectedTab,
        string title,
        string subtitle,
        string statusPanelTitle,
        string operationPanelTitle,
        string parameterPanelTitle,
        string trendPanelTitle,
        string historyPanelTitle,
        IReadOnlyList<ST_MONITOR_TAB> tabs,
        IReadOnlyList<ST_MONITOR_IO_ROW> inputRows,
        IReadOnlyList<ST_MONITOR_IO_ROW> outputRows,
        IReadOnlyList<ST_MONITOR_AXIS_ROW> axisRows,
        IReadOnlyList<ST_MONITOR_COMMAND_HISTORY_ROW> commandHistoryRows,
        IReadOnlyList<ST_MONITOR_STATUS_ROW> statusRows,
        IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> operationButtons,
        IReadOnlyList<ST_MONITOR_PARAMETER_ROW> operationFields,
        IReadOnlyList<ST_MONITOR_PARAMETER_ROW> parameterRows,
        IReadOnlyList<ST_MONITOR_BET_TABLE_ROW> betTableRows,
        IReadOnlyList<ST_MONITOR_TREND_POINT> trendPoints,
        IReadOnlyList<ST_MONITOR_SUMMARY_ITEM> summaryItems,
        IReadOnlyList<ST_MONITOR_POSITION_ROW> positionRows,
        IReadOnlyList<ST_MONITOR_PRODUCT_ITEM> productItems,
        IReadOnlyList<ST_MONITOR_PRODUCT_HEAD_ROW> productHeadRows,
        IReadOnlyList<ST_MONITOR_PRODUCT_HISTORY_ROW> productHistoryRows,
        IReadOnlyList<ST_PWM_PROCESS_ROW> pwmProcessRows,
        IReadOnlyList<ST_PWM_STEP_ROW> pwmStepRows,
        IReadOnlyList<ST_PWM_SETTING_ROW> pwmSettingRows,
        IReadOnlyList<ST_PWM_DEVICE_ROW> pwmDeviceRows,
        IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> pwmProcessButtons,
        IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> pwmStepButtons,
        IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> pwmRunButtons)
    {
        DeviceTabs = deviceTabs;
        SelectedTab = selectedTab;
        Title = title;
        Subtitle = subtitle;
        StatusPanelTitle = statusPanelTitle;
        OperationPanelTitle = operationPanelTitle;
        ParameterPanelTitle = parameterPanelTitle;
        TrendPanelTitle = trendPanelTitle;
        HistoryPanelTitle = historyPanelTitle;
        Tabs = tabs;
        InputRows = inputRows;
        OutputRows = outputRows;
        AxisRows = axisRows;
        CommandHistoryRows = commandHistoryRows;
        StatusRows = statusRows;
        OperationButtons = operationButtons;
        OperationFields = operationFields;
        ParameterRows = parameterRows;
        BetTableRows = betTableRows;
        TrendPoints = trendPoints;
        SummaryItems = summaryItems;
        PositionRows = positionRows;
        ProductItems = productItems;
        ProductHeadRows = productHeadRows;
        ProductHistoryRows = productHistoryRows;
        PwmProcessRows = pwmProcessRows;
        PwmStepRows = pwmStepRows;
        PwmSettingRows = pwmSettingRows;
        PwmDeviceRows = pwmDeviceRows;
        PwmProcessButtons = pwmProcessButtons;
        PwmStepButtons = pwmStepButtons;
        PwmRunButtons = pwmRunButtons;
    }

    private static IReadOnlyList<ST_SCREEN_SECTION> CreateLegacyTabs(IReadOnlyList<ST_MONITOR_TAB> tabs)
    {
        return tabs
            .Select(tab => new ST_SCREEN_SECTION(tab.Name, Array.Empty<ST_DISPLAY_ITEM>()))
            .ToArray();
    }

    private static string NormalizeMonitorTab(string? tab)
    {
        var normalized = (tab ?? "IO").Trim().ToUpperInvariant();
        return normalized switch
        {
            "ATT" => "ATTENUATOR",
            "POWER" or "POWERMETER" or "POWER_METER" => "POWER METER",
            "IO" or "MOTOR" or "LASER" or "CHILLER" or "ATTENUATOR" or "BET" or "POWER METER" or "PRODUCT" or "ETC" => normalized,
            _ => "IO"
        };
    }

    private static string GetSubtitle(string tab)
    {
        return tab switch
        {
            "IO" => "Digital input/output monitor and direct ON/OFF operation",
            "MOTOR" => "Axis position monitor and motor service operation",
            "LASER" => "Talon laser status monitor and laser service operation",
            "CHILLER" => "Chiller status monitor and service operation",
            "ATTENUATOR" => "Conex AGP attenuator position monitor and service operation",
            "BET" => "Beam expander magnification and divergence monitor",
            "POWER METER" => "Power meter measurement monitor and Stage PC measurement-position command",
            "PRODUCT" => "Active product status, head result, and product history monitor",
            _ => "Auxiliary monitor status and service information"
        };
    }

    private static string GetStatusPanelTitle(string tab)
    {
        return tab switch
        {
            "LASER" => "Laser Status",
            "CHILLER" => "Chiller Status",
            "ATTENUATOR" => "Attenuator Status",
            "BET" => "BET Status",
            "POWER METER" => "PowerMeter Status",
            "PRODUCT" => "Product Status",
            _ => "ETC Status"
        };
    }

    private static string GetOperationPanelTitle(string tab)
    {
        return tab switch
        {
            "MOTOR" => "Axis Operation",
            "LASER" => "Laser Operation",
            "CHILLER" => "Chiller Operation",
            "ATTENUATOR" => "Attenuator Operation",
            "BET" => "BET Operation",
            "POWER METER" => "PowerMeter Operation",
            _ => "ETC Operation"
        };
    }

    private static string GetParameterPanelTitle(string tab)
    {
        return tab switch
        {
            "MOTOR" => "Motor Parameter",
            "LASER" => "Laser Parameter",
            "CHILLER" => "Chiller Parameter",
            "ATTENUATOR" => "Attenuator Parameter",
            "BET" => "BET Table",
            "POWER METER" => "PowerMeter Parameter",
            "PRODUCT" => "Head Result",
            _ => "ETC Parameter"
        };
    }

    private static string GetTrendPanelTitle(string tab)
    {
        return tab switch
        {
            "LASER" => "Laser Trend",
            "CHILLER" => "Temperature / Flow Trend",
            "ATTENUATOR" => "Current Position",
            "BET" => "Beam Expander Position",
            "POWER METER" => "Power Trend",
            _ => "Signal Trend"
        };
    }

    private static string GetHistoryPanelTitle(string tab)
    {
        return tab switch
        {
            "MOTOR" => "Motor Command History",
            "LASER" => "Laser Command History",
            "CHILLER" => "Chiller Command History",
            "ATTENUATOR" => "Attenuator Command History",
            "BET" => "BET Command History",
            "POWER METER" => "PowerMeter Command History",
            "PRODUCT" => "Product History",
            _ => "Command History"
        };
    }

    private static IReadOnlyList<ST_MONITOR_IO_ROW> CreateInputRows(ST_DEVICE_STATUS snapshot)
    {
        return snapshot.Io
            .Where(channel => !channel.IsOutput)
            .Select(channel => new ST_MONITOR_IO_ROW(
                channel.Id,
                channel.Address,
                channel.Name,
                OnOffText(channel.IsOn),
                "",
                "",
                ""))
            .ToArray();
    }

    private static IReadOnlyList<ST_MONITOR_IO_ROW> CreateOutputRows(ST_DEVICE_STATUS snapshot)
    {
        return snapshot.Io
            .Where(channel => channel.IsOutput)
            .Select((channel, index) => new ST_MONITOR_IO_ROW(
                channel.Id,
                channel.Address,
                channel.Name,
                OnOffText(channel.IsOn),
                "",
                "",
                "",
                index == 0))
            .ToArray();
    }

    private static IReadOnlyList<ST_MONITOR_AXIS_ROW> CreateAxisRows(
        ST_DEVICE_STATUS snapshot,
        string selectedAxisId)
    {
        return snapshot.Motors
            .Select(axis => new ST_MONITOR_AXIS_ROW(
                axis.AxisId,
                axis.Name,
                FormatAxisPosition(axis.AxisId, axis.CurrentPosition),
                FormatAxisPosition(axis.AxisId, axis.TargetPosition),
                FormatAxisPosition(axis.AxisId, axis.CommandPosition),
                OnOffText(axis.ServoOn),
                axis.HomeCompleted ? "YES" : "NO",
                axis.LimitPlusOn ? "ON" : "OK",
                axis.LimitMinusOn ? "ON" : "OK",
                axis.AlarmOn ? "ALARM" : "-",
                axis.AlarmOn ? "ALARM" : "READY",
                axis.AxisId.Equals(selectedAxisId, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static IReadOnlyList<ST_MONITOR_STATUS_ROW> CreateStatusRows(
        string tab,
        ST_DEVICE_STATUS snapshot,
        IReadOnlyList<ST_DEVICE_COMM_STATUS> communication)
    {
        var module = GetMonitorModule(tab);
        var communicationText = module is null
            ? "-"
            : ToCommunicationText(GetModuleState(communication, module.Value));

        return tab switch
        {
            "LASER" =>
            [
                new("Connection", communicationText, communicationText, "-", "Talon laser connection"),
                new("Output Power", snapshot.Laser.OutputPower.ToString("F3"), snapshot.Laser.PowerOn ? "ON" : "SAFE", "W", "Measured output power"),
                new("Set Power", "1.200", "-", "W", "Target output power"),
                new("Frequency", "20.000", "-", "kHz", "Pulse frequency"),
                new("Gate", snapshot.Laser.GateOn ? "OPEN" : "CLOSE", snapshot.Laser.GateOn ? "OPEN" : "CLOSE", "-", "Laser gate state"),
                new("Shutter", snapshot.Laser.ShutterOpen ? "OPEN" : "CLOSE", snapshot.Laser.ShutterOpen ? "OPEN" : "CLOSE", "-", "Laser shutter state"),
                new("Laser Mode", snapshot.Laser.PowerOn ? "ON" : "SAFE", snapshot.Laser.PowerOn ? "ON" : "SAFE", "-", "Laser operating mode"),
                new("Pulse Width", "100", "-", "us", "Pulse width"),
                new("Diode State", "OK", "OK", "-", "Diode status"),
                new("Temperature", "24.6", "OK", "C", "Laser head temperature"),
                new("Error Code", "0", "OK", "-", "Current error code"),
                new("Interlock State", "READY", "READY", "-", "Interlock condition")
            ],
            "CHILLER" =>
            [
                new("Connection", communicationText, communicationText, "-", "Chiller controller connection"),
                new("Run State", snapshot.Chiller.Running ? "RUN" : "STOP", snapshot.Chiller.Running ? "RUN" : "STOP", "-", "Chiller run status"),
                new("Liquid Temp", snapshot.Chiller.Temperature.ToString("F1"), snapshot.Chiller.AlarmOn ? "ALARM" : "NORMAL", "C", "Outlet liquid temperature"),
                new("Set Temp", "22.0", "SET", "C", "Temperature set point"),
                new("Flow Rate", snapshot.Chiller.Flow.ToString("F1"), snapshot.Chiller.AlarmOn ? "ALARM" : "NORMAL", "L/min", "Liquid flow rate"),
                new("Pressure", snapshot.Chiller.Pressure.ToString("F2"), snapshot.Chiller.AlarmOn ? "ALARM" : "NORMAL", "MPa", "System pressure"),
                new("Pump State", "RUN", "RUN", "-", "Pump operation status"),
                new("Alarm Code", snapshot.Chiller.AlarmOn ? "1" : "0", snapshot.Chiller.AlarmOn ? "ALARM" : "NORMAL", "-", "Active alarm code"),
                new("Warning Code", "0", "NORMAL", "-", "Active warning code"),
                new("Remote Mode", "ON", "ON", "-", "Remote control mode"),
                new("Compressor State", "RUN", "RUN", "-", "Compressor operating state"),
                new("Fan State", "AUTO", "AUTO", "-", "Condenser fan control mode")
            ],
            "ATTENUATOR" =>
            [
                new("Connection", communicationText, communicationText, "-", "Controller connection state"),
                new("Controller", "CONEX_AGP", "OK", "-", "Attenuator controller"),
                new("Current Position", snapshot.Attenuator.CurrentPosition.ToString("F3"), "OK", "DEG", "Current attenuator position"),
                new("Target Position", snapshot.Attenuator.TargetPosition.ToString("F3"), "OK", "DEG", "Target attenuator position"),
                new("Moving", snapshot.Attenuator.CommandState, snapshot.Attenuator.CommandState, "-", "Motion state"),
                new("In Position", IsInPosition(snapshot.Attenuator.CurrentPosition, snapshot.Attenuator.TargetPosition) ? "YES" : "NO", IsInPosition(snapshot.Attenuator.CurrentPosition, snapshot.Attenuator.TargetPosition) ? "YES" : "WARN", "-", "In position status"),
                new("Home State", "DONE", "DONE", "-", "Home completion status"),
                new("Positive Limit", "OFF", "OFF", "-", "Positive limit sensor"),
                new("Negative Limit", "OFF", "OFF", "-", "Negative limit sensor"),
                new("Alarm Code", "0", "OK", "-", "Current alarm code"),
                new("Last Command", snapshot.Attenuator.CommandState, "OK", "-", "Last command name"),
                new("Communication State", communicationText, communicationText, "-", "Communication status")
            ],
            "BET" =>
            [
                new("Connection", communicationText, communicationText, "-", "Beam expander controller link"),
                new("Controller", "BET_CTRL", "OK", "-", "Beam expander controller"),
                new("Magnification", snapshot.Bet.CurrentMagnification.ToString("F3"), "OK", "x", "Current beam magnification"),
                new("Divergence", snapshot.Bet.CurrentDivergence.ToString("F3"), "OK", "x", "Current beam divergence"),
                new("Mag Target", snapshot.Bet.TargetMagnification.ToString("F3"), "OK", "x", "Target magnification"),
                new("Div Target", snapshot.Bet.TargetDivergence.ToString("F3"), "OK", "x", "Target divergence"),
                new("Moving", snapshot.Bet.IsMoving ? "MOVING" : "IDLE", snapshot.Bet.IsMoving ? "MOVING" : "IDLE", "-", "Motion state"),
                new("Mag Home", snapshot.Bet.MagHomeCompleted ? "DONE" : "NO", snapshot.Bet.MagHomeCompleted ? "DONE" : "WARN", "-", "Magnification home state"),
                new("Div Home", snapshot.Bet.DivHomeCompleted ? "DONE" : "NO", snapshot.Bet.DivHomeCompleted ? "DONE" : "WARN", "-", "Divergence home state"),
                new("Alarm Code", snapshot.Bet.AlarmOn ? "1" : "0", snapshot.Bet.AlarmOn ? "ALARM" : "OK", "-", "Current alarm code"),
                new("Last Command", "SET_BET", "OK", "-", "Last command name"),
                new("Communication State", communicationText, communicationText, "-", "Communication status")
            ],
            "POWER METER" =>
            [
                new("Connection", communicationText, communicationText, "-", "Power meter controller connection"),
                new("Model", snapshot.PowerMeter.ModelName, string.IsNullOrWhiteSpace(snapshot.PowerMeter.ModelName) ? "WARN" : "OK", "-", "Power meter model"),
                new("Serial No", snapshot.PowerMeter.SerialNumber, string.IsNullOrWhiteSpace(snapshot.PowerMeter.SerialNumber) || snapshot.PowerMeter.SerialNumber == "-" ? "WARN" : "OK", "-", "Power meter serial number"),
                new("Current Power", snapshot.PowerMeter.MeasuredPower.ToString("F4", CultureInfo.InvariantCulture), "OK", snapshot.PowerMeter.Unit, "Latest measured power"),
                new("Average Power", snapshot.PowerMeter.AveragePower.ToString("F4", CultureInfo.InvariantCulture), "OK", snapshot.PowerMeter.Unit, "Average measured power"),
                new("Min Power", snapshot.PowerMeter.MinPower.ToString("F4", CultureInfo.InvariantCulture), "OK", snapshot.PowerMeter.Unit, "Minimum measured power"),
                new("Max Power", snapshot.PowerMeter.MaxPower.ToString("F4", CultureInfo.InvariantCulture), "OK", snapshot.PowerMeter.Unit, "Maximum measured power"),
                new("WaveLength", snapshot.PowerMeter.WaveLengthNm.ToString("F1", CultureInfo.InvariantCulture), "OK", "nm", "Sensor wavelength setting"),
                new("Beam Pos X", snapshot.PowerMeter.BeamPositionX.ToString("F3", CultureInfo.InvariantCulture), "OK", "mm", "Measured beam X position"),
                new("Beam Pos Y", snapshot.PowerMeter.BeamPositionY.ToString("F3", CultureInfo.InvariantCulture), "OK", "mm", "Measured beam Y position"),
                new("Sample Count", snapshot.PowerMeter.SampleCount.ToString(CultureInfo.InvariantCulture), "OK", "ea", "Accumulated sample count"),
                new("Measure State", snapshot.PowerMeter.IsMeasuring ? "RUN" : "IDLE", snapshot.PowerMeter.IsMeasuring ? "RUN" : "IDLE", "-", "Measurement state"),
                new("Last Command", string.IsNullOrWhiteSpace(snapshot.PowerMeter.LastCommand) ? "-" : snapshot.PowerMeter.LastCommand, snapshot.PowerMeter.LastError == EN_POWER_METER_ERROR.Ok ? "OK" : "ERROR", "-", "Last command name")
            ],
            _ =>
            [
                new("System Mode", "SIM", "SIM", "-", "Device-free monitor mode"),
                new("Update Rate", "500", "OK", "ms", "Monitor refresh interval"),
                new("Data Source", "Simulation", "OK", "-", "Current data provider"),
                new("Operator", "Engineer", "OK", "-", "Current user level")
            ]
        };
    }

    private static EN_EQP_MODULE? GetMonitorModule(string tab)
    {
        return tab switch
        {
            "MOTOR" => EN_EQP_MODULE.Motion,
            "LASER" => EN_EQP_MODULE.TalonLaser,
            "CHILLER" => EN_EQP_MODULE.Chiller,
            "ATTENUATOR" => EN_EQP_MODULE.Attenuator,
            "BET" => EN_EQP_MODULE.Bet,
            "POWER METER" => EN_EQP_MODULE.PowerMeter,
            _ => null
        };
    }

    private static EN_COMM_STATE GetModuleState(
        IReadOnlyList<ST_DEVICE_COMM_STATUS> communication,
        EN_EQP_MODULE module)
    {
        return communication.FirstOrDefault(status => status.Module == module)?.ConnectionState
            ?? EN_COMM_STATE.Offline;
    }

    private static string ToCommunicationText(EN_COMM_STATE state)
    {
        return state switch
        {
            EN_COMM_STATE.Online => "ONLINE",
            EN_COMM_STATE.Simulation => "SIMULATION",
            _ => "OFFLINE"
        };
    }

    private static IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> CreateOperationButtons(string tab)
    {
        return tab switch
        {
            "MOTOR" =>
            [
                new("SERVO ON", "Servo", "Green"),
                new("SERVO OFF", "Servo", "Dark"),
                new("HOME", "Home", "Blue"),
                new("ABS MOVE", "Abs", "Blue"),
                new("REL MOVE", "Rel", "Dark"),
                new("STOP", "Stop", "Red"),
                new("RESET ALARM", "Alarm", "Dark"),
                new("REFRESH", "Refresh", "Dark")
            ],
            "LASER" =>
            [
                new("LASER ON", "Laser", "Green"),
                new("LASER OFF", "Laser", "Dark"),
                new("GATE ON", "Gate", "Green"),
                new("GATE OFF", "Gate", "Dark"),
                new("SHUTTER OPEN", "Shutter", "Green"),
                new("SHUTTER CLOSE", "Shutter", "Dark"),
                new("RESET ERROR", "Alarm", "Blue"),
                new("REFRESH", "Refresh", "Dark")
            ],
            "CHILLER" =>
            [
                new("RUN", "Run", "Green"),
                new("STOP", "Stop", "Red"),
                new("PUMP ONLY", "Pump", "Blue"),
                new("SET\nTEMP", "Temp", "Blue"),
                new("RESET ALARM", "Alarm", "Dark"),
                new("REFRESH", "Refresh", "Dark")
            ],
            "ATTENUATOR" =>
            [
                new("MOVE ABS", "Move", "Blue"),
                new("MOVE REL", "Move", "Blue"),
                new("HOME", "Home", "Blue"),
                new("STOP", "Stop", "Red"),
                new("RESET ALARM", "Alarm", "Dark"),
                new("REFRESH", "Refresh", "Dark")
            ],
            "BET" =>
            [
                new("SET MAG", "Move", "Blue"),
                new("SET DIV", "Move", "Blue"),
                new("HOME", "Home", "Blue"),
                new("STOP", "Stop", "Red"),
                new("RESET ALARM", "Alarm", "Dark"),
                new("REFRESH", "Refresh", "Dark")
            ],
            _ =>
            [
                new("REFRESH", "Refresh", "Blue"),
                new("RESET", "Reset", "Dark")
            ]
        };
    }

    private static IReadOnlyList<ST_MONITOR_PARAMETER_ROW> CreateParameterRows(string tab)
    {
        return tab switch
        {
            "MOTOR" =>
            [
                new("Home Speed", "100.000", "mm/sec", "Warn"),
                new("Move Speed", "300.000", "mm/sec", "Warn"),
                new("Accel", "500.000", "mm/sec2", "Warn"),
                new("Decel", "500.000", "mm/sec2", "Warn"),
                new("In Position Range", "0.010", "mm"),
                new("Home Offset", "0.000", "mm", "Warn"),
                new("Positive Limit", "120.000", "mm", "Warn"),
                new("Negative Limit", "-120.000", "mm"),
                new("On Delay", "10", "ms"),
                new("Off Delay", "10", "ms")
            ],
            "LASER" =>
            [
                new("Laser Power", "1.200", "W", "Warn"),
                new("Frequency", "20.000", "kHz", "Warn"),
                new("Mark Speed", "900", "mm/s", "Warn"),
                new("Jump Speed", "1500", "mm/s"),
                new("Laser On Delay", "8", "us", "Warn"),
                new("Laser Off Delay", "12", "us", "Warn"),
                new("Shot Count", "48000", "count"),
                new("Time Mode", "10", "ms"),
                new("Count Mode", "48000", "count")
            ],
            "CHILLER" =>
            [
                new("Set Temperature", "22.0", "C", "Warn"),
                new("Temp High Limit", "28.0", "C"),
                new("Temp Low Limit", "18.0", "C"),
                new("Flow Low Limit", "8.0", "L/min", "Warn"),
                new("Pressure High Limit", "0.70", "MPa"),
                new("Alarm Delay", "5", "sec"),
                new("On Delay", "3", "sec"),
                new("Off Delay", "5", "sec"),
                new("Communication Timeout", "3000", "ms")
            ],
            "ATTENUATOR" =>
            [
                new("Default Position", "0.000", "DEG", "Warn"),
                new("Process Target Position", "55.000", "DEG", "Warn"),
                new("Move Speed", "50.000", "DEG/sec", "Warn"),
                new("Accel", "100.000", "DEG/sec2"),
                new("Decel", "100.000", "DEG/sec2"),
                new("Position Tolerance", "0.100", "DEG", "Warn"),
                new("Home Offset", "0.000", "DEG", "Warn"),
                new("Positive Limit", "360.000", "DEG"),
                new("Negative Limit", "-120.000", "DEG"),
                new("Move Timeout", "30.000", "sec", "Warn"),
                new("On Delay", "50", "ms", "Warn"),
                new("Off Delay", "50", "ms", "Warn")
            ],
            "BET" =>
            [
                new("Default Magnification", "1.000", "x", "Warn"),
                new("Default Divergence", "1.000", "x", "Warn"),
                new("Mag Move Speed", "0.250", "x/sec", "Warn"),
                new("Div Move Speed", "0.250", "x/sec", "Warn"),
                new("Mag Tolerance", "0.001", "x"),
                new("Div Tolerance", "0.001", "x"),
                new("Positive Limit", "4.000", "x"),
                new("Negative Limit", "0.250", "x"),
                new("Move Timeout", "20.000", "sec", "Warn"),
                new("On Delay", "20", "ms"),
                new("Off Delay", "20", "ms")
            ],
            "POWER METER" =>
            [
                new("WaveLength", "355.0", "nm", "Warn"),
                new("Power High Limit", "1.5000", "W", "Warn"),
                new("Power Low Limit", "0.8000", "W", "Warn"),
                new("Average Count", "10", "count", "Warn"),
                new("Measure Time", "1000", "ms"),
                new("Measure Interval", "100", "ms"),
                new("Stage Move Timeout", "30.000", "sec", "Warn"),
                new("Stage X Target", "0.000", "mm"),
                new("Stage Y Target", "0.000", "mm"),
                new("Stage Z Target", "0.000", "mm"),
                new("Read Command", "pw?", "-"),
                new("Position Command", "pos", "-")
            ],
            _ =>
            [
                new("Refresh Interval", "500", "ms", "Warn"),
                new("Log Retention", "30", "day")
            ]
        };
    }

    private static IReadOnlyList<ST_MONITOR_PARAMETER_ROW> CreateOperationFields(string tab)
    {
        return tab switch
        {
            "MOTOR" =>
            [
                new("Target Position", "12.340", ""),
                new("Relative Distance", "0.000", ""),
                new("Speed", "300.000", ""),
                new("Accel", "500.000", ""),
                new("Decel", "500.000", "")
            ],
            "LASER" =>
            [
                new("Set Power", "1.200", "W"),
                new("Frequency", "20.000", "kHz"),
                new("Pulse Width", "100", "us"),
                new("On Delay", "8", "us"),
                new("Off Delay", "12", "us")
            ],
            "CHILLER" =>
            [
                new("Set Temperature", "22.0", "C"),
                new("High Temp Limit", "28.0", "C"),
                new("Low Temp Limit", "18.0", "C"),
                new("Flow Limit", "8.0", "L/min")
            ],
            "ATTENUATOR" =>
            [
                new("Target Position", "55.000", "DEG"),
                new("Relative Move", "0.000", "DEG"),
                new("Move Speed", "50.000", "DEG/sec"),
                new("Move Timeout", "30.000", "sec")
            ],
            "BET" =>
            [
                new("Target Mag", "1.000", "x"),
                new("Target Div", "1.000", "x"),
                new("Move Speed", "0.250", "x/sec"),
                new("Move Timeout", "20.000", "sec")
            ],
            "POWER METER" =>
            [
                new("WaveLength", "355.0", "nm"),
                new("Stage X", "0.000", "mm"),
                new("Stage Y", "0.000", "mm"),
                new("Stage Z", "0.000", "mm"),
                new("Measure Time", "1000", "ms"),
                new("Sample Count", "10", "ea")
            ],
            _ =>
            [
                new("Refresh Interval", "500", "ms"),
                new("Timeout", "3000", "ms")
            ]
        };
    }

    private static IReadOnlyList<ST_MONITOR_BET_TABLE_ROW> CreateBetTableRows(
        string tab,
        IReadOnlyList<ST_BET_TABLE_DATA> table,
        ST_DEVICE_STATUS snapshot)
    {
        if (tab != "BET")
        {
            return [];
        }

        return table
            .OrderBy(row => row.Index)
            .Select(row =>
            {
                var selected = IsInPosition(row.Magnification, snapshot.Bet.TargetMagnification) &&
                    IsInPosition(row.Divergence, snapshot.Bet.TargetDivergence);

                return new ST_MONITOR_BET_TABLE_ROW(
                    (row.Index + 1).ToString("D2"),
                    row.Use,
                    row.SpotSize.ToString("F3"),
                    row.Magnification.ToString("F3"),
                    row.Divergence.ToString("F3"),
                    row.Magnification.ToString("F3"),
                    row.Divergence.ToString("F3"),
                    row.SpotSizeOffset.ToString("F3"),
                    row.Use ? selected ? "ACTIVE" : "OK" : "DISABLED",
                    selected);
            })
            .ToArray();
    }

    private static IReadOnlyList<ST_MONITOR_COMMAND_HISTORY_ROW> CreateCommandHistoryRows(
        string tab,
        IReadOnlyList<ST_INTERFACE_HISTORY> interfaceHistory)
    {
        var rows = interfaceHistory
            .Where(item => item.Action.Equals("COMMAND", StringComparison.OrdinalIgnoreCase) ||
                item.Action.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                item.Action.Contains("CONNECT", StringComparison.OrdinalIgnoreCase) ||
                item.Action.Equals("DISCONNECT", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.OccurredAt)
            .Take(12)
            .Select(item => new ST_MONITOR_COMMAND_HISTORY_ROW(
                item.OccurredAt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                "LOG",
                item.NickName,
                FormatInterfaceHistoryCommand(item),
                FormatInterfaceHistoryTarget(item),
                FormatInterfaceHistoryResult(item)))
            .ToArray();

        if (rows.Length > 0)
        {
            return rows;
        }

        return tab switch
        {
            "MOTOR" =>
            [
                new("2026-05-15 10:24:12", "ENG1", "GX", "ABS MOVE", "12.340", "OK"),
                new("2026-05-15 10:23:58", "ENG1", "GY", "HOME", "0.000", "OK"),
                new("2026-05-15 10:23:43", "ENG1", "ATTENUATOR", "STOP", "-", "OK"),
                new("2026-05-15 10:23:28", "ENG1", "SCANNER_01_GX", "ABS MOVE", "12.340", "OK"),
                new("2026-05-15 10:23:15", "ENG1", "SCANNER_02_GY", "REL MOVE", "-2.000", "OK")
            ],
            "LASER" =>
            [
                new("10:24:12.345", "ENG1", "LASER OFF", "-", "-", "OK"),
                new("10:23:58.112", "ENG1", "GATE OFF", "-", "-", "OK"),
                new("10:23:35.876", "ENG1", "SET POWER", "1.200 W", "-", "OK"),
                new("10:23:18.552", "ENG1", "SHUTTER CLOSE", "-", "-", "OK"),
                new("10:22:45.231", "ENG1", "LASER ON", "1.200 W", "-", "OK"),
                new("10:22:32.009", "ENG1", "RESET ERROR", "0", "-", "OK"),
                new("10:22:01.678", "ENG1", "SET FREQUENCY", "20.000 kHz", "-", "OK"),
                new("10:21:44.210", "ENG1", "GATE ON", "-", "-", "OK")
            ],
            "CHILLER" =>
            [
                new("2026-05-15 10:24:12", "ENG1", "RUN", "-", "-", "OK"),
                new("2026-05-15 10:22:45", "ENG1", "SET TEMP", "22.0 C", "-", "OK"),
                new("2026-05-15 10:21:31", "ENG1", "RESET ALARM", "-", "-", "OK"),
                new("2026-05-15 10:20:18", "ENG1", "PUMP ONLY", "-", "-", "OK"),
                new("2026-05-15 10:18:55", "ENG1", "STOP", "-", "-", "OK"),
                new("2026-05-15 10:17:32", "ENG1", "REFRESH", "-", "-", "OK"),
                new("2026-05-15 10:16:05", "ENG1", "SET TEMP", "21.5 C", "-", "OK"),
                new("2026-05-15 10:14:41", "ENG1", "RUN", "-", "-", "OK")
            ],
            "ATTENUATOR" =>
            [
                new("10:24:12.345", "ENG1", "MOVE ABS", "55.000", "-", "OK"),
                new("10:23:45.112", "ENG1", "HOME", "-", "-", "OK"),
                new("10:23:12.876", "ENG1", "STOP", "-", "-", "OK"),
                new("10:22:58.552", "ENG1", "RESET ALARM", "-", "-", "OK"),
                new("10:22:31.231", "ENG1", "REFRESH", "-", "-", "OK"),
                new("10:22:10.009", "ENG1", "MOVE REL", "+10.000", "-", "OK"),
                new("10:21:44.210", "ENG1", "MOVE ABS", "40.000", "-", "OK")
            ],
            "BET" =>
            [
                new("10:24:12.345", "ENG1", "SET MAG", "1.000 x", "-", "OK"),
                new("10:23:45.112", "ENG1", "SET DIV", "1.000 x", "-", "OK"),
                new("10:23:12.876", "ENG1", "HOME", "-", "-", "OK"),
                new("10:22:58.552", "ENG1", "RESET ALARM", "-", "-", "OK"),
                new("10:22:31.231", "ENG1", "REFRESH", "-", "-", "OK"),
                new("10:22:10.009", "ENG1", "SET MAG", "1.250 x", "-", "OK"),
                new("10:21:44.210", "ENG1", "SET DIV", "0.950 x", "-", "OK")
            ],
            "POWER METER" =>
            [
                new("10:24:12.345", "ENG1", "READ POWER", "1.2040 W", "-", "OK"),
                new("10:23:58.112", "ENG1", "SET WAVE", "355.0 nm", "-", "OK"),
                new("10:23:35.876", "ENG1", "GET SERIAL", "PM-20260515-01", "-", "OK"),
                new("10:23:18.552", "ENG1", "GET WAVELENGTH", "355.0 nm", "-", "OK"),
                new("10:22:45.231", "ENG1", "STEP ADD", "PWM_HEAD05", "-", "OK"),
                new("10:22:32.009", "ENG1", "START", "POWER_CHECK.PWM", "-", "OK"),
                new("10:22:01.678", "ENG1", "STOP", "POWER_CHECK.PWM", "-", "OK")
            ],
            _ =>
            [
                new("10:24:12.345", "ENG1", tab, "REFRESH", "-", "OK"),
                new("10:23:58.112", "ENG1", tab, "STATUS READ", "-", "OK"),
                new("10:23:35.876", "ENG1", tab, "PARAMETER LOAD", "-", "OK")
            ]
        };
    }

    private static string FormatInterfaceHistoryCommand(ST_INTERFACE_HISTORY item)
    {
        if (item.Action.Equals("COMMAND", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(item.BeforeState) ? "SEND" : item.BeforeState;
        }

        return item.Action.ToUpperInvariant();
    }

    private static string FormatInterfaceHistoryTarget(ST_INTERFACE_HISTORY item)
    {
        if (item.Action.Equals("COMMAND", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(item.AfterState) ? "-" : item.AfterState;
        }

        return string.IsNullOrWhiteSpace(item.BeforeState)
            ? "-"
            : $"{item.BeforeState} -> {item.AfterState}";
    }

    private static string FormatInterfaceHistoryResult(ST_INTERFACE_HISTORY item)
    {
        if (item.Action.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return "ERROR";
        }

        if (!string.IsNullOrWhiteSpace(item.Detail) &&
            item.Detail.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return "ERROR";
        }

        if (item.AfterState.Contains("OFFLINE", StringComparison.OrdinalIgnoreCase))
        {
            return "NG";
        }

        return "OK";
    }

    private static IReadOnlyList<ST_MONITOR_TREND_POINT> CreateTrendPoints(string tab)
    {
        return tab switch
        {
            "CHILLER" =>
            [
                new("09:54", 128, 132, 154),
                new("10:00", 126, 132, 154),
                new("10:06", 127, 132, 154),
                new("10:12", 125, 132, 154),
                new("10:18", 126, 132, 154),
                new("10:24", 126, 132, 154)
            ],
            "POWER METER" =>
            [
                new("09:54", 126, 130, 134),
                new("10:00", 124, 129, 132),
                new("10:06", 128, 131, 136),
                new("10:12", 122, 128, 131),
                new("10:18", 125, 130, 135),
                new("10:24", 127, 132, 138)
            ],
            _ =>
            [
                new("09:54", 112, 134, 0),
                new("10:00", 110, 132, 0),
                new("10:06", 108, 134, 0),
                new("10:12", 106, 132, 0),
                new("10:18", 108, 133, 0),
                new("10:24", 107, 132, 0)
            ]
        };
    }

    private static IReadOnlyList<ST_MONITOR_SUMMARY_ITEM> CreateSummaryItems(string tab, ST_DEVICE_STATUS snapshot)
    {
        return tab switch
        {
            "LASER" =>
            [
                new("Output Power", snapshot.Laser.OutputPower.ToString("F3"), "W", "Accent"),
                new("Temperature", "24.6", "C", "Warn")
            ],
            "CHILLER" =>
            [
                new("Liquid Temp", snapshot.Chiller.Temperature.ToString("F1"), "C", "Accent"),
                new("Set Temp", "22.0", "C", "Warn"),
                new("Flow Rate", snapshot.Chiller.Flow.ToString("F1"), "L/min", "Ok"),
                new("Pressure", snapshot.Chiller.Pressure.ToString("F2"), "MPa", "Accent")
            ],
            "POWER METER" =>
            [
                new("Current Power", snapshot.PowerMeter.MeasuredPower.ToString("F4", CultureInfo.InvariantCulture), snapshot.PowerMeter.Unit, "Accent"),
                new("Average", snapshot.PowerMeter.AveragePower.ToString("F4", CultureInfo.InvariantCulture), snapshot.PowerMeter.Unit, "Ok"),
                new("Max", snapshot.PowerMeter.MaxPower.ToString("F4", CultureInfo.InvariantCulture), snapshot.PowerMeter.Unit, "Warn"),
                new("Min", snapshot.PowerMeter.MinPower.ToString("F4", CultureInfo.InvariantCulture), snapshot.PowerMeter.Unit, "Accent")
            ],
            _ => []
        };
    }

    private static IReadOnlyList<ST_MONITOR_POSITION_ROW> CreatePositionRows(string tab, ST_DEVICE_STATUS snapshot)
    {
        if (tab == "BET")
        {
            return
            [
                new("Target Magnification", snapshot.Bet.TargetMagnification.ToString("F3"), "x", "Warn"),
                new("Target Divergence", snapshot.Bet.TargetDivergence.ToString("F3"), "x", "Warn"),
                new("Mag Error", Math.Abs(snapshot.Bet.CurrentMagnification - snapshot.Bet.TargetMagnification).ToString("F3"), "x", "Warn"),
                new("Div Error", Math.Abs(snapshot.Bet.CurrentDivergence - snapshot.Bet.TargetDivergence).ToString("F3"), "x", "Warn"),
                new("In Position", IsInPosition(snapshot.Bet.CurrentMagnification, snapshot.Bet.TargetMagnification) && IsInPosition(snapshot.Bet.CurrentDivergence, snapshot.Bet.TargetDivergence) ? "YES" : "NO", "", "Ok"),
                new("Moving", snapshot.Bet.IsMoving ? "MOVING" : "IDLE", "", "Ok"),
                new("Last Update", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "")
            ];
        }

        if (tab != "ATTENUATOR")
        {
            return [];
        }

        return
        [
            new("Target Position", snapshot.Attenuator.TargetPosition.ToString("F3"), "DEG", "Warn"),
            new("Position Error", Math.Abs(snapshot.Attenuator.CurrentPosition - snapshot.Attenuator.TargetPosition).ToString("F3"), "DEG", "Warn"),
            new("In Position", IsInPosition(snapshot.Attenuator.CurrentPosition, snapshot.Attenuator.TargetPosition) ? "YES" : "NO", "", "Ok"),
            new("Moving", snapshot.Attenuator.CommandState, "", "Ok"),
            new("Last Update", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "")
        ];
    }

    private async Task<(ST_PRODUCT_DATA? Product, IReadOnlyList<ST_PRODUCT_HISTORY> History, string Error)> LoadProductDisplay(
        CancellationToken cancellationToken)
    {
        try
        {
            var product = _productManager.Current ?? await _productManager.LoadActive(cancellationToken);
            var history = await _productManager.LoadHistory(80, 14, cancellationToken);
            return (product, history, "");
        }
        catch (Exception ex)
        {
            return (null, [], ex.Message);
        }
    }

    private static IReadOnlyList<ST_MONITOR_PRODUCT_ITEM> CreateProductItems(
        ST_PRODUCT_DATA? product,
        string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return
            [
                new("State", "DATA ERROR", "Error"),
                new("Message", error, "Error")
            ];
        }

        if (product is null)
        {
            return
            [
                new("Product ID", "-", "Normal"),
                new("State", "NO ACTIVE PRODUCT", "Warn"),
                new("Result", "-", "Normal")
            ];
        }

        var completedHeads = product.Heads.Count(head => head.Result == EN_PRODUCT_RESULT.OK);
        var ngHeads = product.Heads.Count(head => head.Result == EN_PRODUCT_RESULT.NG);

        return
        [
            new("Product ID", product.ProductId, "Accent"),
            new("Panel ID", product.PanelId, "Normal"),
            new("Lot ID", product.LotId, "Normal"),
            new("Process ID", product.ProcessId, "Normal"),
            new("Recipe", product.RecipeId, "Accent"),
            new("State", product.State.ToString().ToUpperInvariant(), ProductStateTone(product.State, product.Result)),
            new("Result", product.Result.ToString().ToUpperInvariant(), ProductResultTone(product.Result)),
            new("Head OK", completedHeads.ToString(CultureInfo.InvariantCulture), "Ok"),
            new("Head NG", ngHeads.ToString(CultureInfo.InvariantCulture), ngHeads > 0 ? "Error" : "Normal"),
            new("Created", FormatProductDateTime(product.CreatedAt), "Normal"),
            new("Started", FormatProductDateTime(product.StartedAt), "Normal"),
            new("Completed", FormatProductDateTime(product.CompletedAt), "Normal")
        ];
    }

    private static IReadOnlyList<ST_MONITOR_PRODUCT_HEAD_ROW> CreateProductHeadRows(ST_PRODUCT_DATA? product)
    {
        if (product is null)
        {
            return [];
        }

        return product.Heads
            .OrderBy(head => head.HeadNo)
            .Select(head => new ST_MONITOR_PRODUCT_HEAD_ROW(
                $"H{head.HeadNo:00}",
                head.State.ToString().ToUpperInvariant(),
                head.TotalPoints.ToString("N0", CultureInfo.InvariantCulture),
                head.CompletedPoints.ToString("N0", CultureInfo.InvariantCulture),
                head.Result.ToString().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(head.ErrorCode) ? "-" : head.ErrorCode,
                string.IsNullOrWhiteSpace(head.Message) ? "-" : head.Message))
            .ToArray();
    }

    private static IReadOnlyList<ST_PWM_PROCESS_ROW> CreatePwmProcessRows(
        string tab,
        ST_POWER_METER_TABLE_DATA table)
    {
        if (tab != "POWER METER")
        {
            return [];
        }

        return table.Processes
            .Select((process, index) => new ST_PWM_PROCESS_ROW(
                (index + 1).ToString("00", CultureInfo.InvariantCulture),
                process.FileName,
                process.IsSelected
                    ? "ON"
                    : "",
                process.IsSelected
                    ? "LOADED"
                    : "",
                "",
                process.IsSelected))
            .ToArray();
    }

    private static IReadOnlyList<ST_PWM_STEP_ROW> CreatePwmStepRows(
        string tab,
        ST_DEVICE_STATUS snapshot,
        ST_POWER_METER_TABLE_DATA table)
    {
        if (tab != "POWER METER")
        {
            return [];
        }

        var measured = snapshot.PowerMeter.MeasuredPower <= 0
            ? 1.2040
            : snapshot.PowerMeter.MeasuredPower;

        return table.Steps
            .Select((step, index) =>
            {
                var measurePower = index == 0 && measured > 0
                    ? measured.ToString("F4", CultureInfo.InvariantCulture)
                    : step.MeasurePower?.ToString("F4", CultureInfo.InvariantCulture) ?? "-";

                return new ST_PWM_STEP_ROW(
                    step.StepNo.ToString("000", CultureInfo.InvariantCulture),
                    step.OptionName,
                    step.PowerOut ? "ON" : "OFF",
                    step.PowerUnit,
                    step.SettingAtt.ToString("F2", CultureInfo.InvariantCulture),
                    step.SettingPower.ToString("F3", CultureInfo.InvariantCulture),
                    step.SettingFreq.ToString("F1", CultureInfo.InvariantCulture),
                    step.MeasureCycle.ToString(CultureInfo.InvariantCulture),
                    step.MeasureTimeMs.ToString(CultureInfo.InvariantCulture),
                    step.MeasureIntervalMs.ToString(CultureInfo.InvariantCulture),
                    step.StartDelayMs.ToString(CultureInfo.InvariantCulture),
                    step.CoolingTimeMs.ToString(CultureInfo.InvariantCulture),
                    step.Rotator.ToString("F4", CultureInfo.InvariantCulture),
                    measurePower,
                    step.State,
                    index == 0);
            })
            .ToArray();
    }

    private static IReadOnlyList<ST_PWM_SETTING_ROW> CreatePwmSettingRows(
        string tab,
        ST_POWER_METER_TABLE_DATA table)
    {
        if (tab != "POWER METER")
        {
            return [];
        }

        var selectedStep = table.Steps.FirstOrDefault();
        if (selectedStep is null)
        {
            return [];
        }

        return
        [
            new("OPTION NAME", selectedStep.OptionName, "-"),
            new("POWER OUT", selectedStep.PowerOut ? "ON" : "OFF", "-"),
            new("POWER UNIT", selectedStep.PowerUnit, "-"),
            new("SETTING ATT", selectedStep.SettingAtt.ToString("F2", CultureInfo.InvariantCulture), "%"),
            new("SETTING POWER", selectedStep.SettingPower.ToString("F3", CultureInfo.InvariantCulture), "W"),
            new("SETTING FREQ", selectedStep.SettingFreq.ToString("F1", CultureInfo.InvariantCulture), "kHz"),
            new("MEASURE CYCLE", selectedStep.MeasureCycle.ToString(CultureInfo.InvariantCulture), "count"),
            new("MEASURE TIME", selectedStep.MeasureTimeMs.ToString(CultureInfo.InvariantCulture), "ms"),
            new("MEASURE INTERVAL", selectedStep.MeasureIntervalMs.ToString(CultureInfo.InvariantCulture), "ms"),
            new("START DELAY", selectedStep.StartDelayMs.ToString(CultureInfo.InvariantCulture), "ms"),
            new("COOLING TIME", selectedStep.CoolingTimeMs.ToString(CultureInfo.InvariantCulture), "ms"),
            new("WAVELENGTH", "355.0", "nm")
        ];
    }

    private static IReadOnlyList<ST_PWM_DEVICE_ROW> CreatePwmDeviceRows(string tab, ST_DEVICE_STATUS snapshot)
    {
        if (tab != "POWER METER")
        {
            return [];
        }

        var serial = string.IsNullOrWhiteSpace(snapshot.PowerMeter.SerialNumber)
            ? "-"
            : snapshot.PowerMeter.SerialNumber;
        var power = snapshot.PowerMeter.MeasuredPower <= 0
            ? "1.2040"
            : snapshot.PowerMeter.MeasuredPower.ToString("F4", CultureInfo.InvariantCulture);

        return
        [
            new("SEL PWM", "POWER_METER", "-", "REFRESH"),
            new("GET SERIAL", serial, "-", "GET SERIAL"),
            new("GET WAVELENGTH", snapshot.PowerMeter.WaveLengthNm.ToString("F1", CultureInfo.InvariantCulture), "nm", "GET WAVELENGTH"),
            new("SET WAVELENGTH", "355.0", "nm", "SET WAVELENGTH"),
            new("GET POWER", power, snapshot.PowerMeter.Unit, "GET POWER"),
            new("GET BEAM POS", $"{snapshot.PowerMeter.BeamPositionX:F3}, {snapshot.PowerMeter.BeamPositionY:F3}", "mm", "GET BEAM POS")
        ];
    }

    private static IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> CreatePwmProcessButtons(string tab)
    {
        return tab == "POWER METER"
            ?
            [
                new("CREATE", "Add", "Blue"),
                new("DELETE", "Delete", "Red"),
                new("RENAME", "Edit", "Dark"),
                new("MODIFY", "Save", "Green")
            ]
            : [];
    }

    private static IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> CreatePwmStepButtons(string tab)
    {
        return tab == "POWER METER"
            ?
            [
                new("ADD", "Add", "Blue"),
                new("WITH", "Add", "Blue"),
                new("MODIFY", "Edit", "Dark"),
                new("DELETE", "Delete", "Red"),
                new("DELETE ALL", "Delete", "Red")
            ]
            : [];
    }

    private static IReadOnlyList<ST_MONITOR_OPERATION_BUTTON> CreatePwmRunButtons(string tab)
    {
        return tab == "POWER METER"
            ?
            [
                new("START", "Run", "Green"),
                new("STOP", "Stop", "Red")
            ]
            : [];
    }

    private static IReadOnlyList<ST_MONITOR_PRODUCT_HISTORY_ROW> CreateProductHistoryRows(
        IReadOnlyList<ST_PRODUCT_HISTORY> history)
    {
        return history
            .OrderByDescending(item => item.OccurredAt)
            .Select(item => new ST_MONITOR_PRODUCT_HISTORY_ROW(
                item.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                item.ProductId,
                item.RecipeId,
                item.Action,
                item.State,
                item.Result,
                item.Detail))
            .ToArray();
    }

    private static string ProductStateTone(
        EN_PRODUCT_STATE state,
        EN_PRODUCT_RESULT result)
    {
        if (result == EN_PRODUCT_RESULT.NG)
        {
            return "Error";
        }

        return state switch
        {
            EN_PRODUCT_STATE.Running => "Accent",
            EN_PRODUCT_STATE.Completed => "Ok",
            EN_PRODUCT_STATE.Error or EN_PRODUCT_STATE.Scrapped or EN_PRODUCT_STATE.Stopped => "Error",
            _ => "Warn"
        };
    }

    private static string ProductResultTone(EN_PRODUCT_RESULT result)
    {
        return result switch
        {
            EN_PRODUCT_RESULT.OK => "Ok",
            EN_PRODUCT_RESULT.NG => "Error",
            _ => "Warn"
        };
    }

    private static string FormatProductDateTime(DateTimeOffset? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "-";
    }

    private static string OnOffText(bool value)
    {
        return value ? "ON" : "OFF";
    }

    private static string FormatAxisPosition(string axisId, double value)
    {
        return axisId.StartsWith("BET_", StringComparison.OrdinalIgnoreCase)
            ? $"{value:F3} x"
            : value.ToString("F3");
    }

    private static bool IsInPosition(double current, double target)
    {
        return Math.Abs(current - target) <= 0.001;
    }
}

public sealed record ST_MONITOR_TAB(
    string Name,
    bool IsSelected);

public sealed record ST_MONITOR_PRODUCT_ITEM(
    string Name,
    string Value,
    string Tone = "Normal")
{
    public Brush ValueBrush => Tone switch
    {
        "Accent" => CStatusBrush.Simul,
        "Warn" => CStatusBrush.Wait,
        "Ok" => CStatusBrush.Online,
        "Error" => CStatusBrush.Offline,
        _ => CStatusBrush.PrimaryText
    };
}

public sealed record ST_MONITOR_PRODUCT_HEAD_ROW(
    string Head,
    string State,
    string TotalPoints,
    string CompletedPoints,
    string Result,
    string ErrorCode,
    string Message)
{
    public Brush ResultBrush => Result.Trim().ToUpperInvariant() switch
    {
        "OK" => CStatusBrush.Online,
        "NG" => CStatusBrush.Offline,
        _ => CStatusBrush.Wait
    };

    public Brush StateBrush => State.Trim().ToUpperInvariant() switch
    {
        "COMPLETED" or "READY" => CStatusBrush.Online,
        "RUNNING" => CStatusBrush.Simul,
        "ERROR" => CStatusBrush.Offline,
        _ => CStatusBrush.Wait
    };
}

public sealed record ST_MONITOR_PRODUCT_HISTORY_ROW(
    string Time,
    string ProductId,
    string RecipeId,
    string Action,
    string State,
    string Result,
    string Detail)
{
    public Brush ResultBrush => Result.Trim().ToUpperInvariant() switch
    {
        "OK" => CStatusBrush.Online,
        "NG" => CStatusBrush.Offline,
        _ => CStatusBrush.Wait
    };
}

public sealed record ST_MONITOR_IO_ROW(
    string Id,
    string Address,
    string Name,
    string State,
    string OnDelay,
    string OffDelay,
    string Description,
    bool IsSelected = false)
{
    public Brush StateBrush => MonitorStatusBrush(State);

    public Brush RowBrush => IsSelected ? CStatusBrush.Active : CStatusBrush.PrimaryText;

    private static Brush MonitorStatusBrush(string state)
    {
        return state.Trim().ToUpperInvariant() switch
        {
            "ON" or "ONLINE" or "OK" or "READY" or "RUN" or "NORMAL" or "SAFE" or "YES" or "DONE" or "IDLE" => CStatusBrush.Online,
            "OFF" or "CLOSE" or "ERROR" or "ALARM" => CStatusBrush.Offline,
            "WARN" or "WARNING" or "SET" or "WAIT" => CStatusBrush.Wait,
            _ => CStatusBrush.Muted
        };
    }
}

public sealed record ST_MONITOR_AXIS_ROW(
    string Axis,
    string Name,
    string CurrentPosition,
    string TargetPosition,
    string CommandPosition,
    string Servo,
    string Home,
    string LimitPlus,
    string LimitMinus,
    string Alarm,
    string State,
    bool IsSelected = false)
{
    public Brush StateBrush => State.Trim().ToUpperInvariant() switch
    {
        "READY" or "OK" => CStatusBrush.Online,
        "ALARM" or "ERROR" => CStatusBrush.Offline,
        _ => CStatusBrush.Wait
    };

    public Brush ServoBrush => Servo.Trim().ToUpperInvariant() == "ON" ? CStatusBrush.Online : CStatusBrush.Offline;

    public Brush RowBrush => IsSelected ? CStatusBrush.Active : CStatusBrush.PrimaryText;
}

public sealed record ST_MONITOR_STATUS_ROW(
    string Item,
    string Value,
    string State,
    string Unit,
    string Description)
{
    public Brush StateBrush => State.Trim().ToUpperInvariant() switch
    {
        "ON" or "ONLINE" or "OK" or "READY" or "RUN" or "NORMAL" or "SAFE" or "YES" or "DONE" or "IDLE" => CStatusBrush.Online,
        "OFF" or "CLOSE" or "ERROR" or "ALARM" => CStatusBrush.Offline,
        "WARN" or "WARNING" or "SET" or "WAIT" or "SIMULATION" or "SIM" => CStatusBrush.Wait,
        _ => CStatusBrush.Muted
    };

    public Brush ValueBrush => Value.Trim().ToUpperInvariant() switch
    {
        "22.4" or "55.000" or "1.200" or "20.000" or "900" or "50.000" or "30.000" => CStatusBrush.Wait,
        _ => CStatusBrush.PrimaryText
    };
}

public sealed record ST_MONITOR_OPERATION_BUTTON(
    string Label,
    string Icon,
    string Tone)
{
    public Brush BackgroundBrush => Tone switch
    {
        "Green" => CStatusBrush.CommandGreen,
        "Red" => CStatusBrush.CommandRed,
        "Blue" => CStatusBrush.CommandBlue,
        _ => CStatusBrush.CommandDark
    };

    public Brush BorderBrush => Tone switch
    {
        "Green" => CStatusBrush.CommandGreenBorder,
        "Red" => CStatusBrush.CommandRedBorder,
        "Blue" => CStatusBrush.CommandBlueBorder,
        _ => CStatusBrush.CommandDarkBorder
    };

    public Geometry IconGeometry => CMonitorIcon.Get(Icon);
}

public sealed record ST_MONITOR_PARAMETER_ROW(
    string Parameter,
    string Value,
    string Unit,
    string State = "Normal")
{
    public Brush ValueBrush => State switch
    {
        "Accent" => CStatusBrush.Simul,
        "Warn" => CStatusBrush.Wait,
        "Ok" => CStatusBrush.Online,
        _ => CStatusBrush.PrimaryText
    };
}

public sealed record ST_MONITOR_COMMAND_HISTORY_ROW(
    string Time,
    string User,
    string Name,
    string Command,
    string Target,
    string Result)
{
    public Brush ResultBrush => Result.Trim().ToUpperInvariant() switch
    {
        "OK" => CStatusBrush.Online,
        "WARN" => CStatusBrush.Wait,
        "NG" or "ERROR" => CStatusBrush.Offline,
        _ => CStatusBrush.PrimaryText
    };
}

public sealed record ST_MONITOR_BET_TABLE_ROW(
    string No,
    bool Use,
    string BeamSize,
    string Mag,
    string Div,
    string MagPosition,
    string DivPosition,
    string Tolerance,
    string State,
    bool IsSelected = false)
{
    public string UseText => Use ? "ON" : "OFF";

    public Brush UseBrush => Use ? CStatusBrush.Online : CStatusBrush.Muted;

    public Brush StateBrush => State.Trim().ToUpperInvariant() switch
    {
        "ACTIVE" or "SELECTED" or "OK" => CStatusBrush.Online,
        "WARN" => CStatusBrush.Wait,
        "ERROR" => CStatusBrush.Offline,
        _ => CStatusBrush.PrimaryText
    };

    public Brush RowBrush => IsSelected ? CStatusBrush.Active : CStatusBrush.PrimaryText;
}

public sealed record ST_PWM_PROCESS_ROW(
    string No,
    string ProcessName,
    string Use,
    string State,
    string AveragePower,
    bool IsSelected = false)
{
    public Brush StateBrush => State.Trim().ToUpperInvariant() switch
    {
        "LOADED" or "READY" => CStatusBrush.Online,
        "WAIT" => CStatusBrush.Wait,
        "ERROR" => CStatusBrush.Offline,
        _ => CStatusBrush.PrimaryText
    };

    public Brush UseBrush => Use.Trim().ToUpperInvariant() == "ON"
        ? CStatusBrush.Online
        : CStatusBrush.Muted;

    public Brush RowBrush => IsSelected ? CStatusBrush.Active : CStatusBrush.PrimaryText;
}

public sealed record ST_PWM_STEP_ROW(
    string Step,
    string OptionName,
    string PowerOut,
    string PowerUnit,
    string SettingAtt,
    string SettingPower,
    string SettingFreq,
    string MeasureCycle,
    string MeasureTime,
    string MeasureInterval,
    string StartDelay,
    string CycleDelay,
    string Rotator,
    string MeasurePower,
    string State,
    bool IsSelected = false)
{
    public Brush PowerBrush => PowerOut.Trim().ToUpperInvariant() == "ON"
        ? CStatusBrush.Online
        : CStatusBrush.Muted;

    public Brush StateBrush => State.Trim().ToUpperInvariant() switch
    {
        "READY" or "OK" => CStatusBrush.Online,
        "RUN" or "RUNNING" => CStatusBrush.Simul,
        "SKIP" => CStatusBrush.Muted,
        "ERROR" => CStatusBrush.Offline,
        _ => CStatusBrush.Wait
    };

    public Brush RowBrush => IsSelected ? CStatusBrush.Active : CStatusBrush.PrimaryText;
}

public sealed record ST_PWM_SETTING_ROW(
    string Parameter,
    string Value,
    string Unit)
{
    public Brush ValueBrush => Parameter.Contains("POWER", StringComparison.OrdinalIgnoreCase) ||
        Parameter.Contains("ATT", StringComparison.OrdinalIgnoreCase) ||
        Parameter.Contains("FREQ", StringComparison.OrdinalIgnoreCase) ||
        Parameter.Contains("WAVELENGTH", StringComparison.OrdinalIgnoreCase)
        ? CStatusBrush.Wait
        : CStatusBrush.PrimaryText;
}

public sealed record ST_PWM_DEVICE_ROW(
    string Item,
    string Value,
    string Unit,
    string Command)
{
    public Brush ValueBrush => Item.Contains("POWER", StringComparison.OrdinalIgnoreCase) ||
        Item.Contains("WAVELENGTH", StringComparison.OrdinalIgnoreCase)
        ? CStatusBrush.Wait
        : CStatusBrush.PrimaryText;
}

public sealed record ST_MONITOR_TREND_POINT(
    string Time,
    double PrimaryY,
    double SecondaryY,
    double TertiaryY);

public sealed record ST_MONITOR_SUMMARY_ITEM(
    string Name,
    string Value,
    string Unit,
    string State = "Normal")
{
    public Brush ValueBrush => State switch
    {
        "Accent" => CStatusBrush.Simul,
        "Warn" => CStatusBrush.Wait,
        "Ok" => CStatusBrush.Online,
        _ => CStatusBrush.PrimaryText
    };
}

public sealed record ST_MONITOR_POSITION_ROW(
    string Name,
    string Value,
    string Unit,
    string State = "Normal")
{
    public Brush ValueBrush => State switch
    {
        "Accent" => CStatusBrush.Simul,
        "Warn" => CStatusBrush.Wait,
        "Ok" => CStatusBrush.Online,
        _ => CStatusBrush.PrimaryText
    };
}

internal static class CMonitorIcon
{
    private static readonly IReadOnlyDictionary<string, Geometry> Icons =
        new Dictionary<string, Geometry>
        {
            ["Laser"] = Icon("M12,2 V8 M12,16 V22 M2,12 H8 M16,12 H22 M5,5 L9,9 M15,15 L19,19 M19,5 L15,9 M9,15 L5,19"),
            ["Gate"] = Icon("M4,8 H20 M4,16 H20 M7,8 V16 M17,8 V16"),
            ["Shutter"] = Icon("M8,4 H16 V20 H8 Z M10,7 H14 M10,17 H14"),
            ["Reset"] = Icon("M18,9 C17,6 15,4 12,4 C8,4 5,7 5,11 C5,15 8,18 12,18 C15,18 17,16 18,14 M18,5 V9 H14"),
            ["Refresh"] = Icon("M18,9 C17,6 15,4 12,4 C8,4 5,7 5,11 C5,15 8,18 12,18 C15,18 17,16 18,14 M18,5 V9 H14"),
            ["Run"] = Icon("M8,5 L20,12 L8,19 Z"),
            ["Stop"] = Icon("M7,7 H17 V17 H7 Z"),
            ["Pump"] = Icon("M7,12 C7,8 10,6 13,7 C17,8 19,12 17,15 C14,19 8,17 7,12 M13,7 V3 M10,20 H16"),
            ["Temp"] = Icon("M10,14 V5 C10,3.9 10.9,3 12,3 C13.1,3 14,3.9 14,5 V14 C15.2,14.7 16,16 16,17.5 C16,19.7 14.2,21 12,21 C9.8,21 8,19.7 8,17.5 C8,16 8.8,14.7 10,14 M12,8 V17"),
            ["Move"] = Icon("M12,3 V21 M3,12 H21 M12,3 L9,6 M12,3 L15,6 M21,12 L18,9 M21,12 L18,15 M12,21 L9,18 M12,21 L15,18 M3,12 L6,9 M3,12 L6,15"),
            ["Home"] = Icon("M4,11 L12,4 L20,11 M6,10 V20 H10 V14 H14 V20 H18 V10"),
            ["Servo"] = Icon("M12,4 A8,8 0 1 1 12,20 A8,8 0 1 1 12,4 M12,8 V12 L16,14"),
            ["Abs"] = Icon("M5,12 H19 M15,8 L19,12 L15,16 M9,8 L5,12 L9,16"),
            ["Rel"] = Icon("M6,6 H12 V12 H18 M18,12 L14,8 M18,12 L14,16"),
            ["Alarm"] = Icon("M12,3 L22,20 H2 Z M12,8 V13 M12,17 V18"),
            ["Measure"] = Icon("M4,18 L9,8 L13,14 L17,5 L20,10 M4,20 H20"),
            ["Wave"] = Icon("M3,12 C5,6 8,6 10,12 C12,18 15,18 17,12 C18,9 20,8 21,8"),
            ["Position"] = Icon("M12,3 A7,7 0 0 1 19,10 C19,15 12,21 12,21 C12,21 5,15 5,10 A7,7 0 0 1 12,3 M12,8 A2,2 0 1 1 12,12 A2,2 0 1 1 12,8"),
            ["Add"] = Icon("M12,5 V19 M5,12 H19"),
            ["Delete"] = Icon("M5,7 H19 M9,7 V5 H15 V7 M8,10 V19 M12,10 V19 M16,10 V19 M7,7 L8,21 H16 L17,7"),
            ["Save"] = Icon("M5,4 H17 L20,7 V20 H4 V4 H5 M8,4 V10 H16 V4 M8,20 V15 H16 V20"),
            ["Edit"] = Icon("M4,17 V20 H7 L18,9 L15,6 L4,17 M14,7 L17,10")
        };

    public static Geometry Get(string icon)
    {
        return Icons.TryGetValue(icon, out var geometry)
            ? geometry
            : Geometry.Empty;
    }

    private static Geometry Icon(string data)
    {
        var geometry = Geometry.Parse(data);
        geometry.Freeze();
        return geometry;
    }
}





