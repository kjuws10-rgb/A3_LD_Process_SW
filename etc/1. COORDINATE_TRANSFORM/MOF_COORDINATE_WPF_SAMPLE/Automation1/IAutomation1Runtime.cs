namespace MofCoordinateDemo.Automation1;

public interface IAutomation1Runtime : IAsyncDisposable
{
    Task<string> CheckHealthAsync(CancellationToken cancellationToken);

    Task ExecuteAsync(
        AeroScriptPackage package,
        Func<ScriptJobState, string, ValueTask> reportStatus,
        CancellationToken cancellationToken);
}
