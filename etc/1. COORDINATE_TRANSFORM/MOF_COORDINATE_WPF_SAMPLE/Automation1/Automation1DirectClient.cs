using System.Text.Json;
using Aerotech.Automation1.DotNet;

namespace MofCoordinateDemo.Automation1;

/// <summary>
/// Uses the official Automation1 .NET API to connect directly to a remote controller.
/// No custom TCP gateway or server-side helper application is involved.
/// </summary>
public sealed class Automation1DirectClient : IAsyncDisposable
{
    private readonly Automation1ConnectionOptions _options;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly Dictionary<string, Automation1JobRuntime> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private Controller? _controller;
    private bool _disposed;

    public Automation1ConnectionInfo? LastConnectionInfo { get; private set; }

    public Automation1DirectClient(Automation1ConnectionOptions options)
    {
        _options = options;
        ValidateOptions(options);
    }

    public Task<Automation1ConnectionInfo> ConnectAsync(CancellationToken cancellationToken) =>
        ExecuteLockedAsync(ConnectCore, cancellationToken);

    public Task<Automation1ConnectionInfo> HealthCheckAsync(CancellationToken cancellationToken) =>
        ConnectAsync(cancellationToken);

    public Task<Automation1DirectStatus> UploadAsync(
        AeroScriptPackage package,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);
        return ExecuteLockedAsync(() => UploadCore(package), cancellationToken);
    }

    public Task<Automation1DirectStatus> CompileAsync(string jobId, CancellationToken cancellationToken) =>
        ExecuteLockedAsync(() => CompileCore(jobId), cancellationToken);

    public Task<Automation1DirectStatus> RunAsync(
        string jobId,
        Automation1HardwareReadiness hardwareReadiness,
        CancellationToken cancellationToken) =>
        ExecuteLockedAsync(() => RunCore(jobId, hardwareReadiness), cancellationToken);

    public Task<Automation1DirectStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken) =>
        ExecuteLockedAsync(() => GetStatusCore(jobId), cancellationToken);

    public Task<Automation1DirectStatus> StopAsync(string jobId, CancellationToken cancellationToken) =>
        ExecuteLockedAsync(() => StopCore(jobId), cancellationToken);

    private async Task<T> ExecuteLockedAsync<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(operation, cancellationToken);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private Automation1ConnectionInfo ConnectCore()
    {
        ThrowIfDisposed();
        DisconnectCore();
        _controller = _options.Mode switch
        {
            Automation1ConnectionMode.NoAuthentication =>
                Controller.Connect(_options.Host, _options.Port),
            Automation1ConnectionMode.UserPassword =>
                Controller.Connect(_options.Host, _options.Port, _options.UserName, _options.Password),
            Automation1ConnectionMode.SecureCertificate =>
                Controller.ConnectSecure(_options.Host, _options.ExpectedCertificate),
            Automation1ConnectionMode.SecureUserPassword =>
                Controller.ConnectSecure(
                    _options.Host,
                    _options.UserName,
                    _options.Password,
                    _options.ExpectedCertificate),
            _ => throw new ArgumentOutOfRangeException(nameof(_options.Mode))
        };

        if (!_controller.IsRunning && _options.StartControllerIfStopped)
        {
            _controller.Start();
        }

        var isRunning = _controller.IsRunning;
        var taskCount = isRunning ? _controller.Runtime.Tasks.Count : 0;
        var axisSummary = isRunning
            ? string.Join(", ", _controller.Runtime.Axes.Select(axis =>
                $"{axis.AxisName}({(axis.HyperWireDevice is null ? "Virtual" : "Physical")})"))
            : "Controller stopped";
        var host = _controller.Information.Host ?? _options.Host;
        var port = _controller.Information.Port;
        var encrypted = _controller.Information.IsConnectionEncrypted;
        var apiVersion = typeof(Controller).Assembly.GetName().Version?.ToString() ?? "unknown";
        LastConnectionInfo = new Automation1ConnectionInfo(
            host,
            port,
            isRunning,
            encrypted,
            taskCount,
            axisSummary,
            apiVersion,
            $"Direct Automation1 connection ready: {host}:{port}, Running={isRunning}, " +
            $"Encrypted={encrypted}, Tasks={taskCount}, Axes=[{axisSummary}], API={apiVersion}");
        return LastConnectionInfo;
    }

    private Automation1DirectStatus UploadCore(AeroScriptPackage package)
    {
        var controller = EnsureRunningController();
        ValidateTask(controller, package.TaskIndex);
        controller.Files.WriteText(package.ControllerFileName, package.ScriptText);

        var controllerDirectory = package.ControllerFileName.LastIndexOf('/') is var slashIndex && slashIndex >= 0
            ? package.ControllerFileName[..(slashIndex + 1)]
            : "";
        var auditFile =
            $"{controllerDirectory}mof_job_{package.CreatedAtUtc:yyyyMMdd_HHmmss}_{package.JobId[..8]}.json";
        var runtime = new Automation1JobRuntime(
            package,
            Automation1JobAudit.Create(package, _options.Host, _options.Port, auditFile));
        _jobs[package.JobId] = runtime;
        runtime.LastStatus = CreateStatus(
            package,
            Automation1DirectState.Uploaded,
            "ProgramReady",
            $"Controller.Files.WriteText completed: {package.ControllerFileName}",
            "",
            auditFile);
        AppendAuditEvent(runtime, runtime.LastStatus);
        return runtime.LastStatus;
    }

    private Automation1DirectStatus CompileCore(string jobId)
    {
        var runtime = EnsureJob(jobId);
        var package = runtime.Package;
        var controller = EnsureRunningController();
        ValidateTask(controller, package.TaskIndex);
        ValidateExecutionEnvironment(controller, package);

        try
        {
            runtime.CompiledAeroScript = controller.Compiler.CompileControllerFile(
                package.ControllerFileName,
                compileWithDebugInformation: true);
            var compiledControllerFileName = GetCompiledControllerFileName(package.ControllerFileName);
            controller.Files.WriteBytes(compiledControllerFileName, runtime.CompiledAeroScript.CompiledBytes);
            var warnings = runtime.CompiledAeroScript.CompilerWarnings.ToArray();
            var warningText = warnings.Length == 0
                ? "No compiler warnings."
                : $"Compiler warnings: {FormatCompilerResults(warnings)}";
            var taskState = controller.Runtime.Tasks[package.TaskIndex].Status.TaskState.ToString();
            var compiled = CreateStatus(
                package,
                Automation1DirectState.Compiled,
                taskState,
                $"AeroScript compile succeeded and wrote {compiledControllerFileName}. {warningText}",
                "",
                runtime.Audit.ControllerAuditFileName);
            UpdateStatusAndAudit(runtime, compiled);
            return compiled;
        }
        catch (CompileException ex)
        {
            runtime.CompiledAeroScript = null;
            var details = FormatCompilerResults(ex.CompilerErrors);
            var failed = CreateStatus(
                package,
                Automation1DirectState.Failed,
                "CompileError",
                "AeroScript compile failed. Review the file, line, column, and message below.",
                details,
                runtime.Audit.ControllerAuditFileName);
            UpdateStatusAndAudit(runtime, failed);
            throw new InvalidOperationException(
                $"AeroScript compile failed:{Environment.NewLine}{details}",
                ex);
        }
    }

    private Automation1DirectStatus RunCore(
        string jobId,
        Automation1HardwareReadiness hardwareReadiness)
    {
        var runtime = EnsureJob(jobId);
        var package = runtime.Package;
        var controller = EnsureRunningController();
        ValidateExecutionEnvironment(controller, package);
        ValidateHardwareReadiness(package, hardwareReadiness);
        var task = controller.Runtime.Tasks[package.TaskIndex];
        var before = task.Status;
        if (IsBusyTaskState(before.TaskState))
        {
            throw new InvalidOperationException(
                $"Task {package.TaskIndex} is already busy: {before.TaskState}.");
        }

        var compiledProgram = runtime.CompiledAeroScript;
        if (compiledProgram is null)
        {
            CompileCore(jobId);
            compiledProgram = runtime.CompiledAeroScript
                              ?? throw new InvalidOperationException("AeroScript compilation did not produce a program.");
        }

        runtime.RunRequestedAtUtc = DateTimeOffset.UtcNow;
        try
        {
            task.Program.Run(compiledProgram);
        }
        catch (Exception ex)
        {
            runtime.RunRequestedAtUtc = null;
            var failed = CreateStatus(
                package,
                Automation1DirectState.Failed,
                task.Status.TaskState.ToString(),
                $"Task {package.TaskIndex} failed to start the compiled AeroScript program.",
                ex.Message,
                runtime.Audit.ControllerAuditFileName);
            UpdateStatusAndAudit(runtime, failed);
            throw;
        }

        return RefreshTaskStatus(controller, runtime);
    }

    private Automation1DirectStatus GetStatusCore(string jobId)
    {
        var runtime = EnsureJob(jobId);
        if (runtime.RunRequestedAtUtc is null)
        {
            return runtime.LastStatus
                   ?? throw new InvalidOperationException("No uploaded script status is available.");
        }

        return RefreshTaskStatus(EnsureRunningController(), runtime);
    }

    private Automation1DirectStatus StopCore(string jobId)
    {
        var runtime = EnsureJob(jobId);
        var package = runtime.Package;
        var controller = EnsureRunningController();
        var task = controller.Runtime.Tasks[package.TaskIndex];
        if (IsBusyTaskState(task.Status.TaskState))
        {
            task.Program.Stop(5000);
        }

        runtime.RunRequestedAtUtc = null;
        var stopped = CreateStatus(
            package,
            Automation1DirectState.Stopped,
            task.Status.TaskState.ToString(),
            $"Task {package.TaskIndex} Program.Stop completed.",
            "",
            runtime.Audit.ControllerAuditFileName);
        UpdateStatusAndAudit(runtime, stopped);
        return stopped;
    }

    private Automation1DirectStatus RefreshTaskStatus(Controller controller, Automation1JobRuntime runtime)
    {
        var package = runtime.Package;
        var snapshot = controller.Runtime.Tasks[package.TaskIndex].Status;
        var monitor = ReadProcessMonitor(controller);
        var taskState = snapshot.TaskState.ToString();
        var error = snapshot.Error?.ToString() ?? "";
        var inRunTransition = runtime.RunRequestedAtUtc is not null &&
                              DateTimeOffset.UtcNow - runtime.RunRequestedAtUtc.Value < TimeSpan.FromSeconds(2);
        var state = snapshot.TaskState switch
        {
            TaskState.ProgramComplete => Automation1DirectState.Completed,
            TaskState.Error => Automation1DirectState.Failed,
            TaskState.ProgramRunning or TaskState.ProgramFeedhold or TaskState.ProgramPaused =>
                Automation1DirectState.Running,
            _ when inRunTransition => Automation1DirectState.Running,
            _ => Automation1DirectState.Failed
        };
        var message = state switch
        {
            Automation1DirectState.Completed =>
                $"Task {package.TaskIndex} reached ProgramComplete.",
            Automation1DirectState.Failed when snapshot.TaskState == TaskState.Error =>
                $"Task {package.TaskIndex} error: {error}",
            Automation1DirectState.Failed =>
                $"Task {package.TaskIndex} changed to unexpected state {taskState} before completion.",
            _ =>
                $"Task {package.TaskIndex} state={taskState}, source={snapshot.AeroScriptSourceFileName}"
        };
        var status = CreateStatus(
            package,
            state,
            taskState,
            message,
            error,
            runtime.Audit.ControllerAuditFileName,
            monitor.StagePosition,
            monitor.CurrentSequence,
            monitor.TotalTargets,
            monitor.LaserState,
            monitor.LaserPulseCount);
        UpdateStatusAndAudit(runtime, status);
        return status;
    }

    private void UpdateStatusAndAudit(Automation1JobRuntime runtime, Automation1DirectStatus status)
    {
        var changed = runtime.LastStatus is null ||
                      runtime.LastStatus.State != status.State ||
                      !runtime.LastStatus.TaskState.Equals(status.TaskState, StringComparison.Ordinal) ||
                      !runtime.LastStatus.Message.Equals(status.Message, StringComparison.Ordinal);
        runtime.LastStatus = status;
        if (changed)
        {
            AppendAuditEvent(runtime, status);
        }
    }

    private void AppendAuditEvent(Automation1JobRuntime runtime, Automation1DirectStatus status)
    {
        var controller = _controller ?? throw new InvalidOperationException("Automation1 is not connected.");
        var audit = runtime.Audit;
        audit.UpdatedAtUtc = status.UpdatedAtUtc;
        audit.FinalState = status.State.ToString();
        audit.FinalTaskState = status.TaskState;
        audit.FinalMessage = status.Message;
        audit.FinalError = status.Error;
        audit.Events.Add(new Automation1AuditEvent(
            status.UpdatedAtUtc,
            status.State.ToString(),
            status.TaskState,
            status.Message,
            status.Error));
        controller.Files.WriteText(
            audit.ControllerAuditFileName,
            JsonSerializer.Serialize(audit, AuditJsonOptions));
    }

    private Controller EnsureRunningController()
    {
        ThrowIfDisposed();
        var controller = _controller
                         ?? throw new InvalidOperationException("Connect directly to the Automation1 controller first.");
        if (!controller.IsRunning)
        {
            throw new InvalidOperationException("The Automation1 controller is not running.");
        }

        return controller;
    }

    private Automation1JobRuntime EnsureJob(string jobId)
    {
        var runtime = _jobs.GetValueOrDefault(jobId)
                      ?? throw new InvalidOperationException("Write a script to the Controller File System first.");
        return runtime;
    }

    private static void ValidateTask(Controller controller, int taskIndex)
    {
        var count = controller.Runtime.Tasks.Count;
        if (taskIndex <= 0 || taskIndex > count)
        {
            throw new InvalidOperationException(
                $"Task index {taskIndex} is outside the controller task range 1..{count}.");
        }
    }

    private static bool IsBusyTaskState(TaskState state) =>
        state is TaskState.ProgramRunning or
            TaskState.ProgramFeedhold or
            TaskState.ProgramPaused or
            TaskState.QueueRunning or
            TaskState.QueuePaused;

    private static void ValidateExecutionEnvironment(Controller controller, AeroScriptPackage package)
    {
        var configuredAxes = controller.Runtime.Axes
            .GroupBy(axis => axis.AxisName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var missingAxes = package.RequiredAxisNames
            .Where(axisName => !configuredAxes.ContainsKey(axisName))
            .ToArray();
        if (missingAxes.Length > 0)
        {
            throw new InvalidOperationException(
                $"The controller MCD does not contain required axes: {string.Join(", ", missingAxes)}. " +
                $"Configured axes: {string.Join(", ", configuredAxes.Keys.OrderBy(name => name))}.");
        }

        if (package.ExecutionEnvironment != Automation1ExecutionEnvironment.Simulation)
        {
            return;
        }

        var physicalAxes = package.RequiredAxisNames
            .Where(axisName => configuredAxes[axisName].HyperWireDevice is not null)
            .ToArray();
        if (physicalAxes.Length > 0)
        {
            throw new InvalidOperationException(
                "Simulation mode was selected, but these required axes are backed by physical HyperWire devices: " +
                $"{string.Join(", ", physicalAxes)}. Use virtual axes or explicitly select Equipment mode.");
        }
    }

    private static void ValidateHardwareReadiness(
        AeroScriptPackage package,
        Automation1HardwareReadiness readiness)
    {
        if (package.ExecutionEnvironment == Automation1ExecutionEnvironment.Simulation)
        {
            return;
        }

        if (!readiness.IsReady)
        {
            var missing = new List<string>();
            if (!readiness.MotionAxesReady) missing.Add("motion axes ready");
            if (!readiness.SafetyInterlocksReady) missing.Add("safety interlocks ready");
            if (!readiness.LaserAndBeamPathReady) missing.Add("laser/beam path ready");
            if (!readiness.OperatorConfirmed) missing.Add("operator final confirmation");
            throw new InvalidOperationException(
                $"Equipment mode execution is blocked. Confirm: {string.Join(", ", missing)}.");
        }
    }

    private static string FormatCompilerResults(IEnumerable<CompilerResult> results)
    {
        var lines = results.Select(result =>
            $"{result.AeroScriptSourceFileName ?? "<memory>"}" +
            $"({result.StartingLine},{result.StartingColumn})-" +
            $"({result.EndingLine},{result.EndingColumn}): {result.Message}").ToArray();
        return lines.Length == 0 ? "No compiler diagnostic details were returned." : string.Join(Environment.NewLine, lines);
    }

    private static string GetCompiledControllerFileName(string sourceControllerFileName)
    {
        var extensionIndex = sourceControllerFileName.LastIndexOf('.');
        var slashIndex = sourceControllerFileName.LastIndexOf('/');
        return extensionIndex > slashIndex
            ? sourceControllerFileName[..extensionIndex] + ".a1exe"
            : sourceControllerFileName + ".a1exe";
    }

    private static Automation1DirectStatus CreateStatus(
        AeroScriptPackage package,
        Automation1DirectState state,
        string taskState,
        string message,
        string error,
        string auditFile,
        double virtualStagePosition = 0,
        long currentMofSequence = 0,
        long totalMofTargets = 0,
        long simulatedLaserState = 0,
        long simulatedLaserPulseCount = 0) =>
        new(
            package.JobId,
            state,
            taskState,
            message,
            error,
            package.ControllerFileName,
            auditFile,
            package.TaskIndex,
            DateTimeOffset.UtcNow,
            virtualStagePosition,
            currentMofSequence,
            totalMofTargets,
            simulatedLaserState,
            simulatedLaserPulseCount);

    private static ProcessMonitorSnapshot ReadProcessMonitor(Controller controller)
    {
        try
        {
            var globals = controller.Runtime.Variables.Global;
            return new ProcessMonitorSnapshot(
                globals.GetReal(AeroScriptGenerator.MonitorStagePositionGlobalRealIndex),
                globals.GetInteger(AeroScriptGenerator.MonitorCurrentSequenceGlobalIntegerIndex),
                globals.GetInteger(AeroScriptGenerator.MonitorTotalTargetsGlobalIntegerIndex),
                globals.GetInteger(AeroScriptGenerator.MonitorLaserStateGlobalIntegerIndex),
                globals.GetInteger(AeroScriptGenerator.MonitorLaserPulseCountGlobalIntegerIndex));
        }
        catch
        {
            return new ProcessMonitorSnapshot(0, 0, 0, 0, 0);
        }
    }

    private sealed record ProcessMonitorSnapshot(
        double StagePosition,
        long CurrentSequence,
        long TotalTargets,
        long LaserState,
        long LaserPulseCount);

    private static void ValidateOptions(Automation1ConnectionOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Host);
        if (options.Port is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Port));
        }

        if ((options.Mode is Automation1ConnectionMode.UserPassword or
             Automation1ConnectionMode.SecureUserPassword) &&
            (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrEmpty(options.Password)))
        {
            throw new InvalidOperationException("User name and password are required for authenticated connections.");
        }

        if ((options.Mode is Automation1ConnectionMode.SecureCertificate or
             Automation1ConnectionMode.SecureUserPassword) &&
            string.IsNullOrWhiteSpace(options.ExpectedCertificate))
        {
            throw new InvalidOperationException("The expected controller certificate is required for secure connections.");
        }
    }

    private void DisconnectCore()
    {
        if (_controller is null)
        {
            return;
        }

        try
        {
            _controller.Disconnect();
        }
        finally
        {
            _controller = null;
            _jobs.Clear();
            LastConnectionInfo = null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _operationGate.WaitAsync();
        try
        {
            DisconnectCore();
            _disposed = true;
        }
        finally
        {
            _operationGate.Release();
            _operationGate.Dispose();
        }
    }

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        WriteIndented = true
    };

    private sealed class Automation1JobRuntime
    {
        public Automation1JobRuntime(AeroScriptPackage package, Automation1JobAudit audit)
        {
            Package = package;
            Audit = audit;
        }

        public AeroScriptPackage Package { get; }
        public Automation1JobAudit Audit { get; }
        public CompiledAeroScript? CompiledAeroScript { get; set; }
        public Automation1DirectStatus? LastStatus { get; set; }
        public DateTimeOffset? RunRequestedAtUtc { get; set; }
    }
}

public sealed class Automation1JobAudit
{
    public required string JobId { get; init; }
    public required string ControllerHost { get; init; }
    public required int ControllerPort { get; init; }
    public required string ControllerFileName { get; init; }
    public required string ControllerAuditFileName { get; init; }
    public required int TaskIndex { get; init; }
    public required int TargetCount { get; init; }
    public required string GenerationMode { get; init; }
    public required string ExecutionEnvironment { get; init; }
    public required IReadOnlyList<string> RequiredAxisNames { get; init; }
    public required string Sha256 { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string FinalState { get; set; } = "";
    public string FinalTaskState { get; set; } = "";
    public string FinalMessage { get; set; } = "";
    public string FinalError { get; set; } = "";
    public List<Automation1AuditEvent> Events { get; } = new();

    public static Automation1JobAudit Create(
        AeroScriptPackage package,
        string controllerHost,
        int controllerPort,
        string auditFile) =>
        new()
        {
            JobId = package.JobId,
            ControllerHost = controllerHost,
            ControllerPort = controllerPort,
            ControllerFileName = package.ControllerFileName,
            ControllerAuditFileName = auditFile,
            TaskIndex = package.TaskIndex,
            TargetCount = package.TargetCount,
            GenerationMode = package.GenerationMode.ToString(),
            ExecutionEnvironment = package.ExecutionEnvironment.ToString(),
            RequiredAxisNames = package.RequiredAxisNames,
            Sha256 = package.Sha256,
            CreatedAtUtc = package.CreatedAtUtc,
            UpdatedAtUtc = package.CreatedAtUtc
        };
}

public sealed record Automation1AuditEvent(
    DateTimeOffset TimestampUtc,
    string State,
    string TaskState,
    string Message,
    string Error);
