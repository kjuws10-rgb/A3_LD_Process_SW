namespace MofCoordinateDemo.Automation1;

public interface IAutomation1Runtime : IAsyncDisposable
{
    Task ExecuteAsync(
        AeroScriptPackage package,
        Func<ScriptJobState, string, ValueTask> reportStatus,
        CancellationToken cancellationToken);
}

