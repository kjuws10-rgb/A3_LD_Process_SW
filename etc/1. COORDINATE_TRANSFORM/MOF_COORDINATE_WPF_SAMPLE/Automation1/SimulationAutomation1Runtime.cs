namespace MofCoordinateDemo.Automation1;

public sealed class SimulationAutomation1Runtime : IAutomation1Runtime
{
    private readonly TimeSpan _runDelay;

    public SimulationAutomation1Runtime(TimeSpan? runDelay = null)
    {
        _runDelay = runDelay ?? TimeSpan.FromSeconds(1.5);
    }

    public async Task ExecuteAsync(
        AeroScriptPackage package,
        Func<ScriptJobState, string, ValueTask> reportStatus,
        CancellationToken cancellationToken)
    {
        await reportStatus(ScriptJobState.TransferringToController, "SIM: Controller.Files.WriteText 완료");
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        if (package.ScriptText.Contains("SIMULATE_ERROR", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SIMULATE_ERROR 토큰으로 의도된 실행 오류가 발생했습니다.");
        }

        await reportStatus(ScriptJobState.Running, $"SIM: Task {package.TaskIndex} Program.Run 실행 중");
        await Task.Delay(_runDelay, cancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

