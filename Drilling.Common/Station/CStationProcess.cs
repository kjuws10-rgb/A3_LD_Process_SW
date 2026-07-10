using System.Globalization;
using Drilling.Common.Log;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Managers;
using Drilling.Common.Product;
using Drilling.Common.Station;

namespace Drilling.Common.Station;

public sealed class CStationProcess
{
    private const string AutoStepPlan = "PLAN";
    private const string AutoStepInterLock = "INTERLOCK";
    private const string AutoStepParameter = "PARAMETER";
    private const string AutoStepDevice = "DEVICE";
    private const string AutoStepScript = "SCRIPT";
    private const string AutoStepTask = "TASK";
    private const string AutoStepWaitDone = "WAIT_DONE";
    private const string AutoStepComplete = "COMPLETE";

    private const string AutoStepWait = "WAIT";
    private const string AutoStepRunning = "RUNNING";
    private const string AutoStepOk = "OK";
    private const string AutoStepDone = "DONE";
    private const string AutoStepError = "ERROR";
    private const string AutoStepStop = "STOP";

    private static readonly IReadOnlyList<ST_AUTO_STEP_INFO> AutoStepInfos =
    [
        new(AutoStepPlan, "Process Plan"),
        new(AutoStepInterLock, "InterLock Check"),
        new(AutoStepParameter, "Process Parameter"),
        new(AutoStepDevice, "Device Ready"),
        new(AutoStepScript, "Automation Script"),
        new(AutoStepTask, "Automation Task"),
        new(AutoStepWaitDone, "Wait Process Done"),
        new(AutoStepComplete, "Complete Result")
    ];

    private static readonly IReadOnlyList<ST_STATION_PROCESS_FLOW_ITEM> ProcessFlowItems =
    [
        new(1, AutoStepPlan, "Process Plan", EN_STATION_STATE.Idle, EN_PROCESS_STEP.ProcessPlanned, EN_SCRIPT_STATUS.NotCreated, AutoStepInterLock, "ALARM"),
        new(2, AutoStepInterLock, "InterLock Check", EN_STATION_STATE.Check, EN_PROCESS_STEP.ReadyToRun, EN_SCRIPT_STATUS.NotCreated, AutoStepParameter, "ALARM"),
        new(3, AutoStepParameter, "Process Parameter", EN_STATION_STATE.Check, EN_PROCESS_STEP.ReadyToRun, EN_SCRIPT_STATUS.NotCreated, AutoStepDevice, "ALARM"),
        new(4, AutoStepDevice, "Device Ready", EN_STATION_STATE.Check, EN_PROCESS_STEP.ReadyToRun, EN_SCRIPT_STATUS.NotCreated, AutoStepScript, "ALARM"),
        new(5, AutoStepScript, "Automation Script", EN_STATION_STATE.Check, EN_PROCESS_STEP.ReadyToRun, EN_SCRIPT_STATUS.Created, AutoStepTask, "ALARM"),
        new(6, AutoStepTask, "Automation Task", EN_STATION_STATE.Process, EN_PROCESS_STEP.Running, EN_SCRIPT_STATUS.Running, AutoStepWaitDone, "ALARM"),
        new(7, AutoStepWaitDone, "Wait Process Done", EN_STATION_STATE.Process, EN_PROCESS_STEP.Running, EN_SCRIPT_STATUS.Running, AutoStepComplete, "ALARM"),
        new(8, AutoStepComplete, "Complete Result", EN_STATION_STATE.Complete, EN_PROCESS_STEP.Completed, EN_SCRIPT_STATUS.Completed, "IDLE/RESET", "ALARM")
    ];

    private readonly IInterfaceManager _interfaceManager;
    private readonly IMotionManager _motionManager;
    private readonly CInterLockManager _interLockManager;
    private readonly IProductManager? _productManager;
    private readonly IAutomationScriptFile _automationScriptFile;
    private readonly ILogManager? _logManager;
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private readonly List<ST_PROCESS_LOG_ITEM> _processLogs = [];
    private readonly Dictionary<string, string> _autoStepStates = CreateAutoStepStateMap();

    private DateTimeOffset? _scriptCreatedAt;
    private DateTimeOffset? _scriptStartedAt;
    private DateTimeOffset? _scriptCompletedAt;
    private ST_AUTOMATION1_SCRIPT? _lastScript;
    private IReadOnlyList<ST_INTERLOCK_ITEM> _lastInterLockItems = [];
    private ST_PROCESS_MODEL? _processModel;
    private ST_PROCESS_STATISTICS _statistics = EmptyStatistics();
    private ST_STATION_PROCESS_STATUS _snapshot;
    private ST_STATION_STATUS _stationStatus;

    public CStationProcess(
        IInterfaceManager interfaceManager,
        IMotionManager motionManager,
        CInterLockManager interLockManager,
        IAutomationScriptFile automationScriptFile,
        IProductManager? productManager = null,
        ILogManager? logManager = null,
        string stationName = "PROCESS",
        string? scriptDirectory = null)
    {
        _interfaceManager = interfaceManager;
        _motionManager = motionManager;
        _interLockManager = interLockManager;
        _productManager = productManager;
        _automationScriptFile = automationScriptFile;
        _logManager = logManager;
        _snapshot = CreateSnapshot(
            null,
            [],
            EN_SCRIPT_STATUS.NotCreated,
            EN_PROCESS_STEP.Idle,
            null);
        _stationStatus = new ST_STATION_STATUS(
            EN_STATION_ID.Process,
            stationName,
            EN_STATION_STATE.Idle,
            EN_PROCESS_STEP.Idle,
            EN_SCRIPT_STATUS.NotCreated,
            "Station idle.",
            DateTimeOffset.Now);
    }

    public ST_STATION_PROCESS_STATUS Current => _snapshot;

    public ST_STATION_STATUS Status => _stationStatus;

    public static IReadOnlyList<ST_STATION_PROCESS_FLOW_ITEM> GetProcessFlow()
    {
        return ProcessFlowItems;
    }

    public async Task<ST_STATION_PROCESS_STATUS> PrepareProcessPlan(
        ST_PROCESS_PLAN processPlan,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _processLogs.Clear();
        _lastInterLockItems = [];
        _scriptCreatedAt = null;
        _scriptStartedAt = null;
        _scriptCompletedAt = null;
        _lastScript = null;
        ResetAutoStepStates();

        _processModel = BuildProcessModel(processPlan);
        var preview = BuildPreview(_processModel, EN_HEAD_PROCESS_STATUS.Ready);
        _statistics = BuildStatistics(preview, 0.0, TimeSpan.Zero);
        await CreateProduct(_processModel, preview, cancellationToken);

        AddProcessLog("INFO", "SCAN_PC", $"Process plan prepared ({processPlan.ProcessId})");
        AddProcessLog("INFO", "PARSER", $"Head parameter parsed ({preview.Count} heads / {_statistics.TotalPoints} points)");
        SetAutoStepState(AutoStepPlan, AutoStepOk);

        _snapshot = CreateSnapshot(
            processPlan,
            preview,
            EN_SCRIPT_STATUS.NotCreated,
            EN_PROCESS_STEP.ProcessPlanned,
            null);

        SetStationState(
            EN_STATION_STATE.Idle,
            EN_PROCESS_STEP.ProcessPlanned,
            EN_SCRIPT_STATUS.NotCreated,
            $"Process plan prepared: {processPlan.ProcessId}");

        return _snapshot;
    }

    public async Task<ST_STATION_PROCESS_STATUS> Start(CancellationToken cancellationToken = default)
    {
        await _runLock.WaitAsync(cancellationToken);

        try
        {
            EnsureStartAllowed();
            var processPlan = CheckProcessPlan();
            await ExecuteAutoStep(
                AutoStepInterLock,
                processPlan,
                (_, token) => CheckInterLock(token),
                cancellationToken);
            await ExecuteAutoStep(
                AutoStepParameter,
                processPlan,
                (stepProcessPlan, _) =>
                {
                    LoadProcessParameter(stepProcessPlan);
                    return Task.CompletedTask;
                },
                cancellationToken);
            await ExecuteAutoStep(
                AutoStepDevice,
                processPlan,
                PrepareProcessDevices,
                cancellationToken);
            await ExecuteAutoStep(
                AutoStepScript,
                processPlan,
                BuildAutomationScript,
                cancellationToken);
            await ExecuteAutoStep(
                AutoStepTask,
                processPlan,
                StartAutomationTask,
                cancellationToken);
            await ExecuteAutoStep(
                AutoStepWaitDone,
                processPlan,
                (_, token) => WaitProcessDone(token),
                cancellationToken);
            await ExecuteAutoStep(
                AutoStepComplete,
                processPlan,
                (_, token) => CompleteProcess(token),
                cancellationToken);

            return _snapshot;
        }
        catch (OperationCanceledException)
        {
            return await Stop(CancellationToken.None);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or TimeoutException or IOException)
        {
            return await SetAlarm(exception.Message, CancellationToken.None);
        }
        finally
        {
            _runLock.Release();
        }
    }

    private void EnsureStartAllowed()
    {
        if (_stationStatus.State == EN_STATION_STATE.Alarm)
        {
            throw new InvalidOperationException("Station alarm is active. Reset alarm before auto start.");
        }
    }

    private ST_PROCESS_PLAN CheckProcessPlan()
    {
        if (_snapshot.ProcessPlan is null)
        {
            SetAutoStepState(AutoStepPlan, AutoStepError);
            throw new InvalidOperationException("Process Plan is not loaded.");
        }

        SetAutoStepState(AutoStepPlan, AutoStepOk);
        SetStationState(
            EN_STATION_STATE.Check,
            EN_PROCESS_STEP.ReadyToRun,
            EN_SCRIPT_STATUS.NotCreated,
            "Checking InterLock and process plan.");

        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            _snapshot.HeadPreviews,
            EN_SCRIPT_STATUS.NotCreated,
            EN_PROCESS_STEP.ReadyToRun,
            null);

        return _snapshot.ProcessPlan
            ?? throw new InvalidOperationException("Process Plan is not loaded.");
    }

    private async Task ExecuteAutoStep(
        string stepKey,
        ST_PROCESS_PLAN processPlan,
        Func<ST_PROCESS_PLAN, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stepName = GetAutoStepName(stepKey);
        SetAutoStepState(stepKey, AutoStepRunning);
        AddProcessLog("INFO", "STEP", $"{stepName} started.");
        RefreshSnapshot();

        try
        {
            await action(processPlan, cancellationToken);
            SetAutoStepState(stepKey, stepKey == AutoStepComplete ? AutoStepDone : AutoStepOk);
            AddProcessLog("INFO", "STEP", $"{stepName} completed.");
            RefreshSnapshot();
        }
        catch (Exception exception)
        {
            SetAutoStepState(stepKey, AutoStepError);
            AddProcessLog("ERROR", "STEP", $"{stepName} failed. {exception.Message}");
            RefreshSnapshot();
            throw;
        }
    }

    private async Task CheckInterLock(CancellationToken cancellationToken)
    {
        AddProcessLog("INFO", "INTERLOCK", "Auto run interlock check started.");
        var interLock = await GetInterLockSummary(cancellationToken);

        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            _snapshot.HeadPreviews,
            EN_SCRIPT_STATUS.NotCreated,
            EN_PROCESS_STEP.ReadyToRun,
            null);

        if (!interLock.CanAutoRun)
        {
            throw new InvalidOperationException(FormatInterLockBlockedMessage(interLock));
        }

        AddProcessLog("INFO", "INTERLOCK", "Auto run interlock check OK.");
    }

    private void LoadProcessParameter(ST_PROCESS_PLAN processPlan)
    {
        AddProcessLog("INFO", "PARAM", $"Process parameter loaded ({processPlan.Parameters.Count} items).");
        AddProcessLog(
            "INFO",
            "ATTN",
            $"Attenuator target loaded ({ReadParameter(processPlan, "AttenuatorTarget", "23.50")}%)");
    }

    private async Task PrepareProcessDevices(
        ST_PROCESS_PLAN processPlan,
        CancellationToken cancellationToken)
    {
        AddProcessLog("INFO", "DEVICE", $"Process device preparation started. Recipe={processPlan.RecipeId}");

        var communication = await _interfaceManager.GetCommunicationStatus(cancellationToken);
        var offlineModules = communication
            .Where(status => status.ConnectionState == EN_COMM_STATE.Offline)
            .Select(status => status.Module.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (offlineModules.Length > 0 && !_interfaceManager.IsSimulation)
        {
            throw new InvalidOperationException(
                $"Process device is offline: {string.Join(", ", offlineModules)}");
        }

        if (offlineModules.Length > 0)
        {
            AddProcessLog(
                "WARN",
                "DEVICE",
                $"Offline device ignored in simulation mode ({string.Join(", ", offlineModules)}).");
            return;
        }

        AddProcessLog("INFO", "DEVICE", "Process devices ready.");
    }

    private async Task BuildAutomationScript(
        ST_PROCESS_PLAN processPlan,
        CancellationToken cancellationToken)
    {
        _lastScript = await _automationScriptFile.Build(GetProcessModel(), cancellationToken);
        _scriptCreatedAt = _lastScript.CreatedAt;
        _statistics = BuildStatistics(_snapshot.HeadPreviews, 35.0, TimeSpan.FromSeconds(12));
        AddProcessLog("INFO", "SCRIPT", $"{_lastScript.FileName} generated. ({_lastScript.TotalPoints} points)");

        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            _snapshot.HeadPreviews,
            EN_SCRIPT_STATUS.Created,
            EN_PROCESS_STEP.ReadyToRun,
            null);
    }

    private async Task StartAutomationTask(
        ST_PROCESS_PLAN processPlan,
        CancellationToken cancellationToken)
    {
        SetStationState(
            EN_STATION_STATE.Process,
            EN_PROCESS_STEP.Running,
            EN_SCRIPT_STATUS.Running,
            $"Process station running. Process: {processPlan.ProcessId}");

        _scriptStartedAt = DateTimeOffset.Now;
        var runningPreview = SetHeadStatus(_snapshot.HeadPreviews, EN_PROCESS_STEP.Running);
        _statistics = BuildStatistics(runningPreview, 56.3, TimeSpan.FromSeconds(45));
        await StartProduct(runningPreview, cancellationToken);
        AddProcessLog("INFO", "A1_TASK", "Automation1 task prepared. Real equipment run is not connected yet.");

        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            runningPreview,
            EN_SCRIPT_STATUS.Running,
            EN_PROCESS_STEP.Running,
            null);
    }

    private async Task WaitProcessDone(CancellationToken cancellationToken)
    {
        AddProcessLog("INFO", "A1_TASK", "Waiting Automation1 task done.");
        await Task.Delay(250, cancellationToken);
        AddProcessLog("INFO", "A1_TASK", "Automation1 task done signal received.");
    }

    private async Task CompleteProcess(CancellationToken cancellationToken)
    {
        _scriptCompletedAt = DateTimeOffset.Now;
        var completedPreview = SetHeadStatus(_snapshot.HeadPreviews, EN_PROCESS_STEP.Completed);
        _statistics = BuildStatistics(completedPreview, 100.0, TimeSpan.FromSeconds(80));
        var result = new ST_PROCESS_RESULT(true, "Station PROCESS completed.", DateTimeOffset.Now);
        await CompleteProduct(completedPreview, result, cancellationToken);
        AddProcessLog("INFO", "A1_TASK", "Automation1 task simulation completed.");
        await ReportProcessResult(result, "COMPLETE", cancellationToken);

        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            completedPreview,
            EN_SCRIPT_STATUS.Completed,
            EN_PROCESS_STEP.Completed,
            result);

        SetStationState(
            EN_STATION_STATE.Complete,
            EN_PROCESS_STEP.Completed,
            EN_SCRIPT_STATUS.Completed,
            "Process station completed.");
    }

    public async Task<ST_STATION_PROCESS_STATUS> Stop(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AddProcessLog("WARN", "OPERATOR", "Station PROCESS stopped by operator.");
        _statistics = BuildStatistics(_snapshot.HeadPreviews, _statistics.ProgressPercent, _statistics.ElapsedTime);
        var result = new ST_PROCESS_RESULT(false, "Station PROCESS stopped by operator.", DateTimeOffset.Now);
        await StopProduct(result.Message, cancellationToken);
        await ReportProcessResult(result, "STOP", cancellationToken);

        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            SetHeadStatus(_snapshot.HeadPreviews, EN_PROCESS_STEP.Stopped),
            EN_SCRIPT_STATUS.NotCreated,
            EN_PROCESS_STEP.Stopped,
            result);
        MarkRunningAutoSteps(AutoStepStop);
        RefreshSnapshot();

        SetStationState(
            EN_STATION_STATE.Stopped,
            EN_PROCESS_STEP.Stopped,
            EN_SCRIPT_STATUS.NotCreated,
            "Station stopped by operator.");

        return _snapshot;
    }

    public async Task<ST_STATION_PROCESS_STATUS> Reset(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_stationStatus.State == EN_STATION_STATE.Process)
        {
            AddProcessLog("WARN", "RESET", "Reset ignored while station is running.");
            RefreshSnapshot();
            return _snapshot;
        }

        if (_stationStatus.State == EN_STATION_STATE.Alarm)
        {
            var interLock = await GetInterLockSummary(cancellationToken);

            if (!interLock.CanAutoRun)
            {
                var message = FormatInterLockBlockedMessage(interLock);
                AddProcessLog("WARN", "RESET", $"Reset blocked. {message}");
                _snapshot = CreateSnapshot(
                    _snapshot.ProcessPlan,
                    SetHeadStatus(_snapshot.HeadPreviews, EN_PROCESS_STEP.Error),
                    EN_SCRIPT_STATUS.Error,
                    EN_PROCESS_STEP.Error,
                    new ST_PROCESS_RESULT(false, message, DateTimeOffset.Now));
                SetStationState(
                    EN_STATION_STATE.Alarm,
                    EN_PROCESS_STEP.Error,
                    EN_SCRIPT_STATUS.Error,
                    $"Reset blocked. {message}");
                return _snapshot;
            }

            AddProcessLog("INFO", "RESET", "Alarm reset accepted. InterLock is clear.");
        }

        _processLogs.Clear();
        _lastInterLockItems = [];
        _scriptCreatedAt = null;
        _scriptStartedAt = null;
        _scriptCompletedAt = null;
        _lastScript = null;
        _processModel = null;
        ResetAutoStepStates();
        _statistics = EmptyStatistics();
        _snapshot = CreateSnapshot(
            null,
            [],
            EN_SCRIPT_STATUS.NotCreated,
            EN_PROCESS_STEP.Idle,
            null);

        SetStationState(
            EN_STATION_STATE.Idle,
            EN_PROCESS_STEP.Idle,
            EN_SCRIPT_STATUS.NotCreated,
            "Station reset to idle.");

        return _snapshot;
    }

    private ST_STATION_PROCESS_STATUS CreateSnapshot(
        ST_PROCESS_PLAN? processPlan,
        IReadOnlyList<ST_HEAD_PATH_DATA> preview,
        EN_SCRIPT_STATUS scriptStatus,
        EN_PROCESS_STEP processStep,
        ST_PROCESS_RESULT? result)
    {
        return new ST_STATION_PROCESS_STATUS(
            processPlan,
            _processModel,
            preview,
            scriptStatus,
            processStep,
            result,
            BuildProcessSequence(processStep),
            BuildCurrentStepDetails(processPlan, scriptStatus, processStep),
            BuildProcessSummary(preview, _statistics),
            _processLogs.ToArray(),
            BuildScriptStatusItems(processPlan, scriptStatus, result),
            BuildScriptLifecycleItems(scriptStatus, processStep),
            _lastInterLockItems,
            _statistics);
    }

    private async Task<ST_INTERLOCK_SUMMARY> GetInterLockSummary(CancellationToken cancellationToken)
    {
        var snapshot = await GetDeviceStatus(cancellationToken);
        var interLock = _interLockManager.Evaluate(snapshot);
        _lastInterLockItems = interLock.Items;
        return interLock;
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

    private async Task CreateProduct(
        ST_PROCESS_MODEL processModel,
        IReadOnlyList<ST_HEAD_PATH_DATA> preview,
        CancellationToken cancellationToken)
    {
        if (_productManager is null)
        {
            return;
        }

        var processPlan = processModel.Plan;
        var productId = ReadAnyParameter(processPlan, processPlan.ProductId, "ProductId", "PRODUCT_ID");
        var panelId = ReadAnyParameter(processPlan, processPlan.PanelId, "PanelId", "PANEL_ID", "PanelID");
        var lotId = ReadAnyParameter(processPlan, processPlan.LotId, "LotId", "LOT_ID", "LotID");
        var headPointCounts = preview.ToDictionary(
            head => head.HeadNo,
            head => head.Points.Count);

        await _productManager.CreateProduct(
            processPlan.ProcessId,
            productId,
            panelId,
            lotId,
            processPlan.RecipeId,
            processPlan.Parameters,
            headPointCounts,
            cancellationToken);
        _processModel = processModel with { Product = _productManager.Current };
        AddProcessLog("INFO", "PRODUCT", $"Product created ({_productManager.Current?.ProductId ?? processPlan.ProcessId})");
    }

    private async Task StartProduct(
        IReadOnlyList<ST_HEAD_PATH_DATA> preview,
        CancellationToken cancellationToken)
    {
        var productId = GetCurrentProcessProductId();
        if (_productManager is null || string.IsNullOrWhiteSpace(productId))
        {
            return;
        }

        await _productManager.StartProduct(productId, cancellationToken);

        foreach (var head in preview.Where(head => head.Status == EN_HEAD_PROCESS_STATUS.Running))
        {
            await _productManager.SetHeadRunning(productId, head.HeadNo, cancellationToken);
        }

        AddProcessLog("INFO", "PRODUCT", $"Product started ({productId})");
    }

    private async Task CompleteProduct(
        IReadOnlyList<ST_HEAD_PATH_DATA> preview,
        ST_PROCESS_RESULT result,
        CancellationToken cancellationToken)
    {
        var productId = GetCurrentProcessProductId();
        if (_productManager is null || string.IsNullOrWhiteSpace(productId))
        {
            return;
        }

        foreach (var head in preview)
        {
            await _productManager.SetHeadResult(
                productId,
                head.HeadNo,
                result.IsSuccess,
                result.IsSuccess ? "" : "1",
                result.Message,
                cancellationToken);
        }

        await _productManager.CompleteProduct(
            productId,
            result.IsSuccess,
            result.Message,
            cancellationToken);
        AddProcessLog("INFO", "PRODUCT", $"Product completed ({productId})");
    }

    private async Task StopProduct(
        string message,
        CancellationToken cancellationToken)
    {
        var productId = GetCurrentProcessProductId();
        if (_productManager is null || string.IsNullOrWhiteSpace(productId))
        {
            return;
        }

        await _productManager.StopProduct(productId, message, cancellationToken);
        AddProcessLog("WARN", "PRODUCT", $"Product stopped ({productId})");
    }

    private async Task SetProductError(
        string message,
        CancellationToken cancellationToken)
    {
        var productId = GetCurrentProcessProductId();
        if (_productManager is null || string.IsNullOrWhiteSpace(productId))
        {
            return;
        }

        await _productManager.SetError(productId, message, cancellationToken);
        AddProcessLog("ERROR", "PRODUCT", $"Product error ({productId})");
    }

    private Task ReportProcessResult(
        ST_PROCESS_RESULT result,
        string action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var processId = _snapshot.ProcessPlan?.ProcessId ?? "-";
        var recipeId = _snapshot.ProcessPlan?.RecipeId ?? "-";
        var productId = GetCurrentProcessProductId() ?? processId;
        var reportState = result.IsSuccess ? "OK" : "NG";
        var detail = FormatProcessResultDetail(result, processId, recipeId, productId);
        var hostNickName = _interfaceManager.GetInterfaceData(EN_EQP_MODULE.WonikCtrl, 0)?.NickName ?? "WONIK_CTRL";

        AddProcessLog("INFO", "RESULT", $"{action} result reported. {detail}");
        _logManager?.WriteStationState(
            _stationStatus.StationName,
            _stationStatus.State.ToString().ToUpperInvariant(),
            $"RESULT_{action}",
            detail);
        _logManager?.WriteInterfaceCommand(
            EN_EQP_MODULE.WonikCtrl,
            hostNickName,
            $"PROCESS_RESULT_{action}",
            reportState,
            detail);

        return Task.CompletedTask;
    }

    private string? GetCurrentProcessProductId()
    {
        if (_productManager?.Current is null || _snapshot.ProcessPlan is null)
        {
            return null;
        }

        return _productManager.Current.ProcessId.Equals(
            _snapshot.ProcessPlan.ProcessId,
            StringComparison.OrdinalIgnoreCase)
                ? _productManager.Current.ProductId
                : null;
    }

    private static IReadOnlyList<ST_HEAD_PATH_DATA> BuildPreview(
        ST_PROCESS_MODEL processModel,
        EN_HEAD_PROCESS_STATUS status)
    {
        return processModel.Heads
            .Where(head => head.Use)
            .Select(head => new ST_HEAD_PATH_DATA(
                head.HeadNo,
                status,
                head.Path))
            .ToArray();
    }

    private static ST_PROCESS_MODEL BuildProcessModel(ST_PROCESS_PLAN processPlan)
    {
        var parameters = new Dictionary<string, string>(
            processPlan.Parameters,
            StringComparer.OrdinalIgnoreCase);
        var headCount = Math.Clamp(ReadIntAny(parameters, 12, "HeadCount", "HEAD_COUNT", "SCANNER_COUNT"), 1, 64);
        var defaultLaserPower = ReadDoubleAny(parameters, 1.0, "LaserPower", "LASER_POWER");
        var defaultFrequency = ReadDoubleAny(parameters, 20.0, "LaserFrequency", "LASER_FREQUENCY");
        var defaultMarkSpeed = ReadDoubleAny(parameters, 900.0, "MarkSpeed", "MARK_SPEED", "STAGE_SPEED");
        var defaultJumpSpeed = ReadDoubleAny(parameters, 1500.0, "JumpSpeed", "JUMP_SPEED");
        var defaultShotCount = ReadIntAny(parameters, 48000, "ShotCount", "SHOT_COUNT");

        var heads = Enumerable.Range(1, headCount)
            .Select(headNo => new ST_HEAD_PROCESS_DATA(
                headNo,
                ReadBoolAny(parameters, true, CreateHeadKeys(headNo, "USE")),
                ReadTextAny(parameters, "CIRCLE", CreateHeadKeys(headNo, "SHAPE")),
                ReadDoubleAny(parameters, defaultLaserPower, CreateHeadKeys(headNo, "LASER_POWER")),
                ReadDoubleAny(parameters, defaultFrequency, CreateHeadKeys(headNo, "LASER_FREQUENCY")),
                ReadIntAny(parameters, defaultShotCount, CreateHeadKeys(headNo, "SHOT_COUNT")),
                ReadDoubleAny(parameters, defaultMarkSpeed, CreateHeadKeys(headNo, "MARK_SPEED")),
                ReadDoubleAny(parameters, defaultJumpSpeed, CreateHeadKeys(headNo, "JUMP_SPEED", "SCANNER_JUMP_SPEED")),
                ReadDoubleAny(parameters, 0.0, CreateHeadKeys(headNo, "OFFSET_X")),
                ReadDoubleAny(parameters, 0.0, CreateHeadKeys(headNo, "OFFSET_Y")),
                BuildHeadShape(headNo, processPlan)))
            .ToArray();

        return new ST_PROCESS_MODEL(
            processPlan,
            null,
            heads,
            parameters,
            DateTimeOffset.Now);
    }

    private static IReadOnlyList<ST_PATH_POINT> BuildHeadShape(int headNo, ST_PROCESS_PLAN processPlan)
    {
        var diameter = ReadDoubleAny(processPlan.Parameters, 0.35, "Diameter", "DIAMETER", "PIXEL_SIZE");
        var radius = diameter / 2.0 + headNo * 0.002;
        var centerX = ((headNo - 1) % 6) * 1.2;
        var centerY = ((headNo - 1) / 6) * 1.2;
        var pointCount = headNo % 3 == 0 ? 6 : 16;

        return Enumerable.Range(0, pointCount)
            .Select(index =>
            {
                var angle = Math.PI * 2.0 * index / pointCount;
                var xScale = headNo % 2 == 0 ? 1.20 : 1.00;
                var yScale = headNo % 4 == 0 ? 0.82 : 1.00;

                return new ST_PATH_POINT(
                    centerX + Math.Cos(angle) * radius * xScale,
                    centerY + Math.Sin(angle) * radius * yScale);
            })
            .ToArray();
    }

    private static IReadOnlyList<ST_HEAD_PATH_DATA> SetHeadStatus(
        IReadOnlyList<ST_HEAD_PATH_DATA> preview,
        EN_PROCESS_STEP processStep)
    {
        return preview
            .Select(head =>
            {
                var status = processStep switch
                {
                    EN_PROCESS_STEP.Running => head.HeadNo == 1
                        ? EN_HEAD_PROCESS_STATUS.Running
                        : EN_HEAD_PROCESS_STATUS.Ready,
                    EN_PROCESS_STEP.Completed => EN_HEAD_PROCESS_STATUS.Completed,
                    EN_PROCESS_STEP.Stopped => EN_HEAD_PROCESS_STATUS.Ready,
                    EN_PROCESS_STEP.Error => EN_HEAD_PROCESS_STATUS.Error,
                    _ => head.Status
                };

                return head with { Status = status };
            })
            .ToArray();
    }

    private static IReadOnlyList<ST_PROCESS_DISPLAY_ITEM> BuildProcessSequence(
        EN_PROCESS_STEP processStep)
    {
        return
        [
            new("1", "IDLE", SequenceState(processStep, 1)),
            new("2", "CHECK", SequenceState(processStep, 2)),
            new("3", "PROCESS", SequenceState(processStep, 3)),
            new("4", "WAIT PROCESS DONE", SequenceState(processStep, 4)),
            new("5", "COMPLETE", SequenceState(processStep, 5))
        ];
    }

    private IReadOnlyList<ST_PROCESS_DISPLAY_ITEM> BuildCurrentStepDetails(
        ST_PROCESS_PLAN? processPlan,
        EN_SCRIPT_STATUS scriptStatus,
        EN_PROCESS_STEP processStep)
    {
        if (processPlan is null)
        {
            return AutoStepInfos
                .Select(step => new ST_PROCESS_DISPLAY_ITEM(step.DisplayName, AutoStepWait))
                .ToArray();
        }

        return AutoStepInfos
            .Select(step => new ST_PROCESS_DISPLAY_ITEM(
                step.DisplayName,
                ReadAutoStepState(step.Key, scriptStatus, processStep)))
            .ToArray();
    }

    private static IReadOnlyList<ST_PROCESS_DISPLAY_ITEM> BuildProcessSummary(
        IReadOnlyList<ST_HEAD_PATH_DATA> preview,
        ST_PROCESS_STATISTICS statistics)
    {
        var currentHead = preview.FirstOrDefault(head => head.Status == EN_HEAD_PROCESS_STATUS.Running)
            ?? preview.FirstOrDefault(head => head.Status == EN_HEAD_PROCESS_STATUS.Ready)
            ?? preview.FirstOrDefault();
        var doneCount = (int)Math.Round(statistics.TotalPoints * statistics.ProgressPercent / 100.0);

        return
        [
            new("Current Head", currentHead is null ? "-" : $"H{currentHead.HeadNo:00}"),
            new("Process Index", $"{doneCount:000} / {statistics.TotalPoints:000}"),
            new("Tact Time (Est.)", FormatDuration(statistics.EstimatedTime)),
            new("Cycle Time", FormatDuration(statistics.ElapsedTime))
        ];
    }

    private IReadOnlyList<ST_PROCESS_DISPLAY_ITEM> BuildScriptStatusItems(
        ST_PROCESS_PLAN? processPlan,
        EN_SCRIPT_STATUS scriptStatus,
        ST_PROCESS_RESULT? result)
    {
        return
        [
            new("Script Build", FormatScriptStatus(scriptStatus)),
            new("Script File", processPlan is null ? "-" : _lastScript?.FileName ?? _automationScriptFile.ScriptFileName, _lastScript?.FilePath ?? ""),
            new("Task No", processPlan is null ? "-" : "1"),
            new("Execute State", FormatExecuteState(scriptStatus, result)),
            new("Created Time", FormatDateTime(_scriptCreatedAt)),
            new("Started Time", FormatDateTime(_scriptStartedAt)),
            new("Completed Time", FormatDateTime(_scriptCompletedAt)),
            new("Result", result is null ? "In Progress" : result.IsSuccess ? "OK" : "NG"),
            new("Error Code", result is { IsSuccess: false } ? "1" : "0")
        ];
    }

    private static IReadOnlyList<ST_PROCESS_DISPLAY_ITEM> BuildScriptLifecycleItems(
        EN_SCRIPT_STATUS scriptStatus,
        EN_PROCESS_STEP processStep)
    {
        return
        [
            new("Not Created", LifecycleState(scriptStatus, processStep, 0)),
            new("Created", LifecycleState(scriptStatus, processStep, 1)),
            new("Started", LifecycleState(scriptStatus, processStep, 2)),
            new("Running", LifecycleState(scriptStatus, processStep, 3)),
            new("Completed", LifecycleState(scriptStatus, processStep, 4))
        ];
    }

    private static ST_PROCESS_STATISTICS BuildStatistics(
        IReadOnlyList<ST_HEAD_PATH_DATA> preview,
        double progressPercent,
        TimeSpan elapsedTime)
    {
        var totalPoints = preview.Sum(head => head.Points.Count);

        return new ST_PROCESS_STATISTICS(
            totalPoints,
            (int)Math.Round(totalPoints * 0.60),
            (int)Math.Round(totalPoints * 0.40),
            totalPoints == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(80),
            elapsedTime,
            Math.Clamp(progressPercent, 0.0, 100.0));
    }

    private static ST_PROCESS_STATISTICS EmptyStatistics()
    {
        return new ST_PROCESS_STATISTICS(0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, 0.0);
    }

    private void RefreshSnapshot()
    {
        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            _snapshot.HeadPreviews,
            _snapshot.ScriptStatus,
            _snapshot.ProcessStep,
            _snapshot.Result);
    }

    private void ResetAutoStepStates()
    {
        _autoStepStates.Clear();

        foreach (var step in AutoStepInfos)
        {
            _autoStepStates[step.Key] = AutoStepWait;
        }
    }

    private void SetAutoStepState(string stepKey, string state)
    {
        _autoStepStates[stepKey] = state;
    }

    private void MarkRunningAutoSteps(string state)
    {
        foreach (var step in AutoStepInfos)
        {
            if (_autoStepStates.TryGetValue(step.Key, out var currentState) &&
                currentState == AutoStepRunning)
            {
                _autoStepStates[step.Key] = state;
            }
        }
    }

    private string ReadAutoStepState(
        string stepKey,
        EN_SCRIPT_STATUS scriptStatus,
        EN_PROCESS_STEP processStep)
    {
        if (_autoStepStates.TryGetValue(stepKey, out var state))
        {
            return state;
        }

        return (stepKey, scriptStatus, processStep) switch
        {
            (AutoStepScript, EN_SCRIPT_STATUS.Created or EN_SCRIPT_STATUS.Running or EN_SCRIPT_STATUS.Completed, _) => AutoStepOk,
            (AutoStepTask, EN_SCRIPT_STATUS.Running, EN_PROCESS_STEP.Running) => AutoStepRunning,
            (AutoStepComplete, EN_SCRIPT_STATUS.Completed, EN_PROCESS_STEP.Completed) => AutoStepDone,
            _ => AutoStepWait
        };
    }

    private static string GetAutoStepName(string stepKey)
    {
        return AutoStepInfos.FirstOrDefault(step => step.Key == stepKey)?.DisplayName ?? stepKey;
    }

    private static Dictionary<string, string> CreateAutoStepStateMap()
    {
        return AutoStepInfos.ToDictionary(
            step => step.Key,
            _ => AutoStepWait,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string SequenceState(EN_PROCESS_STEP processStep, int sequenceNo)
    {
        return processStep switch
        {
            EN_PROCESS_STEP.Idle => sequenceNo == 1 ? "ACTIVE" : "WAIT",
            EN_PROCESS_STEP.ProcessPlanned => sequenceNo == 1 ? "DONE" : sequenceNo == 2 ? "WAIT" : "WAIT",
            EN_PROCESS_STEP.ReadyToRun => sequenceNo <= 2 ? "DONE" : sequenceNo == 3 ? "READY" : "WAIT",
            EN_PROCESS_STEP.Running => sequenceNo <= 2 ? "DONE" : sequenceNo == 3 ? "ACTIVE" : sequenceNo == 4 ? "WAIT" : "WAIT",
            EN_PROCESS_STEP.Completed => "DONE",
            EN_PROCESS_STEP.Stopped => sequenceNo == 3 ? "STOP" : sequenceNo < 3 ? "DONE" : "WAIT",
            EN_PROCESS_STEP.Error => sequenceNo == 3 ? "ALARM" : sequenceNo < 3 ? "DONE" : "WAIT",
            _ => "WAIT"
        };
    }

    private static string LifecycleState(
        EN_SCRIPT_STATUS scriptStatus,
        EN_PROCESS_STEP processStep,
        int stepNo)
    {
        return (scriptStatus, processStep, stepNo) switch
        {
            (EN_SCRIPT_STATUS.NotCreated, _, 0) => "ACTIVE",
            (EN_SCRIPT_STATUS.Created, _, 0) => "DONE",
            (EN_SCRIPT_STATUS.Created, _, 1) => "ACTIVE",
            (EN_SCRIPT_STATUS.Running, _, <= 2) => "DONE",
            (EN_SCRIPT_STATUS.Running, _, 3) => "ACTIVE",
            (EN_SCRIPT_STATUS.Completed, _, _) => stepNo < 4 ? "DONE" : "ACTIVE",
            (EN_SCRIPT_STATUS.Error, _, _) => stepNo == 3 ? "ERROR" : "-",
            _ => "-"
        };
    }

    private static double ReadDouble(
        IReadOnlyDictionary<string, string> parameters,
        string key,
        double defaultValue)
    {
        return parameters.TryGetValue(key, out var value) &&
            double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
    }

    private static int ReadInt(
        IReadOnlyDictionary<string, string> parameters,
        string key,
        int defaultValue)
    {
        return parameters.TryGetValue(key, out var value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
    }

    private static double ReadDoubleAny(
        IReadOnlyDictionary<string, string> parameters,
        double defaultValue,
        params string[] keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (parameters.TryGetValue(key, out var value) &&
                double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    private static int ReadIntAny(
        IReadOnlyDictionary<string, string> parameters,
        int defaultValue,
        params string[] keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (!parameters.TryGetValue(key, out var value))
            {
                continue;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
            {
                return intResult;
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleResult))
            {
                return (int)Math.Round(doubleResult);
            }
        }

        return defaultValue;
    }

    private static bool ReadBool(
        IReadOnlyDictionary<string, string> parameters,
        string key,
        bool defaultValue)
    {
        if (!parameters.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "1" or "Y" or "YES" or "TRUE" or "ON" or "USE" => true,
            "0" or "N" or "NO" or "FALSE" or "OFF" or "SKIP" => false,
            _ => defaultValue
        };
    }

    private static bool ReadBoolAny(
        IReadOnlyDictionary<string, string> parameters,
        bool defaultValue,
        params string[] keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (parameters.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim().ToUpperInvariant() switch
                {
                    "1" or "Y" or "YES" or "TRUE" or "ON" or "USE" => true,
                    "0" or "N" or "NO" or "FALSE" or "OFF" or "SKIP" => false,
                    _ => defaultValue
                };
            }
        }

        return defaultValue;
    }

    private static string ReadText(
        IReadOnlyDictionary<string, string> parameters,
        string key,
        string defaultValue)
    {
        return parameters.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : defaultValue;
    }

    private static string ReadTextAny(
        IReadOnlyDictionary<string, string> parameters,
        string defaultValue,
        params string[] keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (parameters.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return defaultValue;
    }

    private static string[] CreateHeadKeys(int headNo, params string[] names)
    {
        var headNoText = headNo.ToString(CultureInfo.InvariantCulture);
        var paddedHeadNoText = headNo.ToString("00", CultureInfo.InvariantCulture);
        var prefixes = new[]
        {
            $"H{paddedHeadNoText}",
            $"HEAD{headNoText}",
            $"HEAD{paddedHeadNoText}"
        };

        return prefixes
            .SelectMany(prefix => names.Select(name => $"{prefix}_{name}"))
            .ToArray();
    }

    private static string ReadParameter(
        ST_PROCESS_PLAN? processPlan,
        string key,
        string defaultValue)
    {
        return processPlan?.Parameters.TryGetValue(key, out var value) == true
            ? value
            : defaultValue;
    }

    private static string ReadAnyParameter(
        ST_PROCESS_PLAN processPlan,
        string defaultValue,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (processPlan.Parameters.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return defaultValue;
    }

    private ST_PROCESS_MODEL GetProcessModel()
    {
        return _processModel
            ?? throw new InvalidOperationException("Process Model is not built.");
    }

    private async Task<ST_STATION_PROCESS_STATUS> SetAlarm(
        string message,
        CancellationToken cancellationToken)
    {
        AddProcessLog("ERROR", "STATION", message);
        MarkRunningAutoSteps(AutoStepError);
        await SetProductError(message, cancellationToken);
        var result = new ST_PROCESS_RESULT(false, message, DateTimeOffset.Now);
        await ReportProcessResult(result, "ALARM", cancellationToken);

        _snapshot = CreateSnapshot(
            _snapshot.ProcessPlan,
            SetHeadStatus(_snapshot.HeadPreviews, EN_PROCESS_STEP.Error),
            EN_SCRIPT_STATUS.Error,
            EN_PROCESS_STEP.Error,
            result);

        SetStationState(
            EN_STATION_STATE.Alarm,
            EN_PROCESS_STEP.Error,
            EN_SCRIPT_STATUS.Error,
            message);

        return _snapshot;
    }

    private void SetStationState(
        EN_STATION_STATE state,
        EN_PROCESS_STEP processStep,
        EN_SCRIPT_STATUS scriptStatus,
        string message)
    {
        _stationStatus = _stationStatus with
        {
            State = state,
            ProcessStep = processStep,
            ScriptStatus = scriptStatus,
            LastMessage = message,
            ChangedAt = DateTimeOffset.Now
        };

        _logManager?.WriteStationState(
            _stationStatus.StationName,
            state.ToString().ToUpperInvariant(),
            processStep.ToString().ToUpperInvariant(),
            message);
    }

    private void AddProcessLog(
        string level,
        string source,
        string message)
    {
        _processLogs.Add(new ST_PROCESS_LOG_ITEM(DateTimeOffset.Now, level, source, message));
    }

    private static string FormatInterLockBlockedMessage(ST_INTERLOCK_SUMMARY interLock)
    {
        var item = interLock.Items.FirstOrDefault(x => x.Level != EN_INTERLOCK_LEVEL.Ok);
        return item is null
            ? "InterLock is not ready."
            : $"InterLock is not ready. {item.Signal}: {item.Detail}";
    }

    private static string FormatScriptStatus(EN_SCRIPT_STATUS status)
    {
        return status switch
        {
            EN_SCRIPT_STATUS.NotCreated => "Not Created",
            _ => status.ToString()
        };
    }

    private static string FormatExecuteState(
        EN_SCRIPT_STATUS scriptStatus,
        ST_PROCESS_RESULT? result)
    {
        if (result is not null)
        {
            return result.IsSuccess ? "Completed" : "Failed";
        }

        return scriptStatus switch
        {
            EN_SCRIPT_STATUS.Running => "Running",
            EN_SCRIPT_STATUS.Created => "Ready",
            EN_SCRIPT_STATUS.Error => "Error",
            _ => "-"
        };
    }

    private static string FormatDateTime(DateTimeOffset? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) ?? "-";
    }

    private static string FormatProcessResultDetail(
        ST_PROCESS_RESULT result,
        string processId,
        string recipeId,
        string productId)
    {
        var state = result.IsSuccess ? "OK" : "NG";
        return string.Join(
            ", ",
            $"Process={processId}",
            $"Recipe={recipeId}",
            $"Product={productId}",
            $"Result={state}",
            $"Message={result.Message}",
            $"Time={result.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
    }

    private static string FormatDuration(TimeSpan value)
    {
        return value == TimeSpan.Zero
            ? "00:00:00"
            : value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private sealed record ST_AUTO_STEP_INFO(string Key, string DisplayName);
}


