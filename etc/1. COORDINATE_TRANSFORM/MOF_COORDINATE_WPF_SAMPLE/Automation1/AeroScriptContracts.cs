using System.Security.Cryptography;
using System.Text;

namespace MofCoordinateDemo.Automation1;

public enum AeroScriptGenerationMode
{
    VirtualWaitSimulation,
    HardwareCoordinateProgram
}

public enum Automation1ExecutionEnvironment
{
    Simulation,
    Equipment
}

public enum Automation1ConnectionMode
{
    NoAuthentication,
    UserPassword,
    SecureCertificate,
    SecureUserPassword
}

public enum Automation1DirectState
{
    Disconnected,
    Connected,
    Uploaded,
    Compiled,
    Running,
    Completed,
    Failed,
    Stopped
}

public sealed record Automation1ConnectionOptions(
    string Host,
    int Port,
    Automation1ConnectionMode Mode,
    string UserName,
    string Password,
    string ExpectedCertificate,
    bool StartControllerIfStopped)
{
    public const int DefaultControllerPort = 12200;
}

public sealed record Automation1ConnectionInfo(
    string Host,
    int Port,
    bool IsRunning,
    bool IsEncrypted,
    int TaskCount,
    string AxisSummary,
    string ApiVersion,
    string Message);

public sealed record Automation1HardwareReadiness(
    bool MotionAxesReady,
    bool SafetyInterlocksReady,
    bool LaserAndBeamPathReady,
    bool OperatorConfirmed)
{
    public static Automation1HardwareReadiness Simulation { get; } = new(true, true, true, true);

    public bool IsReady =>
        MotionAxesReady && SafetyInterlocksReady && LaserAndBeamPathReady && OperatorConfirmed;
}

public sealed record Automation1DirectStatus(
    string JobId,
    Automation1DirectState State,
    string TaskState,
    string Message,
    string Error,
    string ControllerFileName,
    string ControllerAuditFileName,
    int TaskIndex,
    DateTimeOffset UpdatedAtUtc);

public sealed record AeroScriptPackage(
    string JobId,
    string ControllerFileName,
    string ScriptText,
    string Sha256,
    int TaskIndex,
    int TargetCount,
    AeroScriptGenerationMode GenerationMode,
    Automation1ExecutionEnvironment ExecutionEnvironment,
    IReadOnlyList<string> RequiredAxisNames,
    DateTimeOffset CreatedAtUtc)
{
    public static AeroScriptPackage Create(
        string controllerFileName,
        string scriptText,
        int taskIndex,
        int targetCount,
        AeroScriptGenerationMode generationMode,
        IReadOnlyList<string> requiredAxisNames,
        bool preserveJobFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controllerFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptText);
        ArgumentNullException.ThrowIfNull(requiredAxisNames);
        if (taskIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taskIndex), "Automation1 task index must be 1 or greater.");
        }

        var jobId = Guid.NewGuid().ToString("N");
        var createdAt = DateTimeOffset.UtcNow;
        var resolvedControllerFile = preserveJobFile
            ? AddJobSuffix(controllerFileName, createdAt, jobId)
            : controllerFileName;
        var scriptBytes = Encoding.UTF8.GetBytes(scriptText);
        return new AeroScriptPackage(
            jobId,
            resolvedControllerFile,
            scriptText,
            Convert.ToHexString(SHA256.HashData(scriptBytes)),
            taskIndex,
            targetCount,
            generationMode,
            generationMode == AeroScriptGenerationMode.VirtualWaitSimulation
                ? Automation1ExecutionEnvironment.Simulation
                : Automation1ExecutionEnvironment.Equipment,
            requiredAxisNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            createdAt);
    }

    private static string AddJobSuffix(string controllerFileName, DateTimeOffset createdAt, string jobId)
    {
        var extensionIndex = controllerFileName.LastIndexOf('.');
        var slashIndex = controllerFileName.LastIndexOf('/');
        var suffix = $"_{createdAt:yyyyMMdd_HHmmss}_{jobId[..8]}";
        return extensionIndex > slashIndex
            ? controllerFileName.Insert(extensionIndex, suffix)
            : controllerFileName + suffix + ".ascript";
    }
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
