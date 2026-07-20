using System.Security.Cryptography;
using System.Text;

namespace MofCoordinateDemo.Automation1;

public enum ScriptRequestType
{
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

public sealed record AeroScriptPackage(
    string JobId,
    string ControllerFileName,
    string ScriptText,
    string Sha256,
    int TaskIndex,
    DateTimeOffset CreatedAtUtc)
{
    public static AeroScriptPackage Create(string controllerFileName, string scriptText, int taskIndex)
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
    public const int CurrentProtocolVersion = 1;

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

public sealed record AeroScriptGenerationOptions(
    string AxisXTemplate,
    string AxisYTemplate,
    double CoordinatedSpeed,
    bool EnableAxes,
    bool DisableAxesAtEnd);

public sealed record ScriptServerOptions(
    string BindAddress,
    int Port,
    string ApiKey,
    string SpoolDirectory,
    int MaxScriptBytes,
    TimeSpan ExecutionTimeout);

