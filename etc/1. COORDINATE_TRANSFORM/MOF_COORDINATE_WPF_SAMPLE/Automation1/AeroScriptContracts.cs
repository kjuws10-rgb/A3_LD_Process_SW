using System.Security.Cryptography;
using System.Text;

namespace MofCoordinateDemo.Automation1;

public enum ScriptRequestType
{
    HealthCheck,
    UploadScript,
    RunScript,
    GetStatus
}

public enum ScriptJobState
{
    Unknown,
    Uploaded,
    Queued,
    TransferringToController,
    Running,
    Completed,
    Failed,
    Rejected
}

public enum AeroScriptGenerationMode
{
    VirtualWaitSimulation,
    HardwareCoordinateProgram
}

public enum AeroScriptModePolicy
{
    Any,
    VirtualOnly,
    HardwareOnly
}

public sealed record AeroScriptPackage(
    string JobId,
    string ControllerFileName,
    string ScriptText,
    string Sha256,
    int TaskIndex,
    AeroScriptGenerationMode GenerationMode,
    DateTimeOffset CreatedAtUtc)
{
    public static AeroScriptPackage Create(
        string controllerFileName,
        string scriptText,
        int taskIndex,
        AeroScriptGenerationMode generationMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controllerFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptText);

        var scriptBytes = Encoding.UTF8.GetBytes(scriptText);
        return new AeroScriptPackage(
            Guid.NewGuid().ToString("N"),
            controllerFileName,
            scriptText,
            Convert.ToHexString(SHA256.HashData(scriptBytes)),
            taskIndex,
            generationMode,
            DateTimeOffset.UtcNow);
    }
}

public sealed record ScriptServerRequest(
    int ProtocolVersion,
    ScriptRequestType RequestType,
    string RequestId,
    string ApiKey,
    string? JobId,
    AeroScriptPackage? Package)
{
    public const int CurrentProtocolVersion = 3;

    public static ScriptServerRequest Health(string apiKey) =>
        new(CurrentProtocolVersion, ScriptRequestType.HealthCheck, NewRequestId(), apiKey, null, null);

    public static ScriptServerRequest Upload(string apiKey, AeroScriptPackage package) =>
        new(CurrentProtocolVersion, ScriptRequestType.UploadScript, NewRequestId(), apiKey, package.JobId, package);

    public static ScriptServerRequest Run(string apiKey, string jobId) =>
        new(CurrentProtocolVersion, ScriptRequestType.RunScript, NewRequestId(), apiKey, jobId, null);

    public static ScriptServerRequest Status(string apiKey, string jobId) =>
        new(CurrentProtocolVersion, ScriptRequestType.GetStatus, NewRequestId(), apiKey, jobId, null);

    private static string NewRequestId() => Guid.NewGuid().ToString("N");
}

public sealed record ScriptJobStatus(
    string JobId,
    ScriptJobState State,
    string Message,
    string ControllerFileName,
    int TaskIndex,
    AeroScriptGenerationMode GenerationMode,
    string Sha256,
    DateTimeOffset UpdatedAtUtc);

public sealed record ScriptServerResponse(
    int ProtocolVersion,
    string RequestId,
    bool Success,
    string ErrorCode,
    string Message,
    ScriptJobStatus? Job)
{
    public static ScriptServerResponse Ok(ScriptServerRequest request, string message, ScriptJobStatus? job) =>
        new(ScriptServerRequest.CurrentProtocolVersion, request.RequestId, true, "", message, job);

    public static ScriptServerResponse Error(ScriptServerRequest request, string code, string message, ScriptJobStatus? job = null) =>
        new(ScriptServerRequest.CurrentProtocolVersion, request.RequestId, false, code, message, job);
}

public sealed record AeroScriptGenerationOptions
{
    public AeroScriptGenerationMode Mode { get; init; } = AeroScriptGenerationMode.VirtualWaitSimulation;
    public string StageAxisName { get; init; } = "Y";
    public string AxisXTemplate { get; init; } = "GX";
    public string AxisYTemplate { get; init; } = "GY";
    public double StartYPosition { get; init; } = 500;
    public double StageTravelDistance { get; init; } = 40;
    public double StageSpeed { get; init; } = 20;
    public double ScannerRapidSpeed { get; init; } = 1000;
    public double CoordinatedSpeed { get; init; } = 100;
    public double RampRate { get; init; } = 3_000_000;
    public double TrajectoryFirFilter { get; init; } = 3;
    public double MotionUpdateRateKhz { get; init; } = 100;
    public int ExecuteNumLines { get; init; } = 110;
    public double SetupDwellSeconds { get; init; } = 0.2;
    public double MoveDelayMilliseconds { get; init; } = 0.1;
    public double WaitStepY { get; init; } = 10;
    public double SoftwareLimitLow { get; init; } = -10_000;
    public double SoftwareLimitHigh { get; init; } = 10_000;
    public bool EnableAxes { get; init; }
    public bool DisableAxesAtEnd { get; init; }
    public bool IncludeLaserLibraryImport { get; init; }
    public string LaserLibraryFileName { get; init; } = "LaserOnLibrary.a1lib";
}

public sealed record ScriptServerOptions(
    string BindAddress,
    int Port,
    string ApiKey,
    string SpoolDirectory,
    int MaxScriptBytes,
    TimeSpan ExecutionTimeout,
    AeroScriptModePolicy ModePolicy);
