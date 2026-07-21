using System.IO;
using System.Reflection;

namespace MofCoordinateDemo.Automation1;

/// <summary>
/// Loads the official Automation1 .NET API at runtime so the sample still builds
/// on a client PC where Automation1-MDK is not installed.
/// </summary>
public sealed class Automation1ReflectionRuntime : IAutomation1Runtime
{
    private readonly string? _controllerHost;
    private readonly string? _assemblyPath;

    public Automation1ReflectionRuntime(string? controllerHost, string? assemblyPath)
    {
        _controllerHost = string.IsNullOrWhiteSpace(controllerHost) ? null : controllerHost;
        _assemblyPath = string.IsNullOrWhiteSpace(assemblyPath) ? null : assemblyPath;
    }

    public Task<string> CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dynamic? controller = null;
        try
        {
            var assemblyPath = ResolveAssemblyPath();
            var assembly = Assembly.LoadFrom(assemblyPath);
            var controllerType = assembly.GetType("Aerotech.Automation1.DotNet.Controller", throwOnError: true)!;
            controller = Connect(controllerType);

            var isRunning = (bool)controller.IsRunning;
            var host = Convert.ToString(controller.Information.Host) ?? _controllerHost ?? "local";
            var port = Convert.ToString(controller.Information.Port) ?? "unknown";
            var taskSummary = isRunning
                ? $", Tasks={Convert.ToString(controller.Runtime.Tasks.Count)}"
                : ", Controller는 연결되었지만 정지 상태이며 실행 시 Start()를 호출합니다";
            return Task.FromResult(
                $"Automation1 runtime ready. Controller={host}:{port}, IsRunning={isRunning}{taskSummary}, DLL={assemblyPath}");
        }
        finally
        {
            if (controller is not null)
            {
                try
                {
                    controller.Disconnect();
                }
                catch
                {
                    // Health result is based on the successful connection and query.
                }
            }
        }
    }

    public async Task ExecuteAsync(
        AeroScriptPackage package,
        Func<ScriptJobState, string, ValueTask> reportStatus,
        CancellationToken cancellationToken)
    {
        dynamic? controller = null;
        try
        {
            var assembly = Assembly.LoadFrom(ResolveAssemblyPath());
            var controllerType = assembly.GetType("Aerotech.Automation1.DotNet.Controller", throwOnError: true)!;
            controller = Connect(controllerType);

            if (!(bool)controller.IsRunning)
            {
                controller.Start();
            }

            await reportStatus(ScriptJobState.TransferringToController,
                $"Controller.Files.WriteText: {package.ControllerFileName}");
            controller.Files.WriteText(package.ControllerFileName, package.ScriptText);

            dynamic task = controller.Runtime.Tasks[package.TaskIndex];
            task.Program.Run(package.ControllerFileName);
            await reportStatus(ScriptJobState.Running,
                $"Task {package.TaskIndex} Program.Run: {package.ControllerFileName}");

            string? previousTaskState = null;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The Automation1 documentation recommends reading Status once per poll
                // and then using that snapshot to avoid state races.
                dynamic status = task.Status;
                var taskState = status.TaskState.ToString() as string ?? "Unknown";
                if (!taskState.Equals(previousTaskState, StringComparison.OrdinalIgnoreCase))
                {
                    var sourceFile = Convert.ToString(status.AeroScriptSourceFileName) ?? package.ControllerFileName;
                    await reportStatus(
                        ScriptJobState.Running,
                        $"Task {package.TaskIndex} TaskState={taskState}, Source={sourceFile}");
                    previousTaskState = taskState;
                }

                if (taskState.Equals("ProgramComplete", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (taskState.Equals("Error", StringComparison.OrdinalIgnoreCase))
                {
                    var error = status.Error?.ToString() as string ?? "Automation1 task error";
                    throw new InvalidOperationException(error);
                }

                if (!IsActiveTaskState(taskState))
                {
                    throw new InvalidOperationException(
                        $"Task {package.TaskIndex}가 완료 전에 예상하지 못한 상태 {taskState}로 전환되었습니다.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (controller is not null)
        {
            TryStopTask(controller, package.TaskIndex);
            throw;
        }
        finally
        {
            if (controller is not null)
            {
                try
                {
                    controller.Disconnect();
                }
                catch
                {
                    // The execution result is more important than a disconnect error.
                }
            }
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private dynamic Connect(Type controllerType)
    {
        var parameterTypes = _controllerHost is null ? Type.EmptyTypes : new[] { typeof(string) };
        var connect = controllerType.GetMethod(
            "Connect",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: parameterTypes,
            modifiers: null)
            ?? throw new MissingMethodException(controllerType.FullName, "Connect");

        return connect.Invoke(null, _controllerHost is null ? null : new object?[] { _controllerHost })
               ?? throw new InvalidOperationException("Automation1 Controller.Connect returned null.");
    }

    private static void TryStopTask(dynamic controller, int taskIndex)
    {
        try
        {
            controller.Runtime.Tasks[taskIndex].Program.Stop(5000);
        }
        catch
        {
            // Cancellation must continue even when the controller cannot stop the task.
        }
    }

    private static bool IsActiveTaskState(string taskState) =>
        taskState.Equals("ProgramRunning", StringComparison.OrdinalIgnoreCase) ||
        taskState.Equals("ProgramFeedhold", StringComparison.OrdinalIgnoreCase) ||
        taskState.Equals("ProgramPaused", StringComparison.OrdinalIgnoreCase);

    private string ResolveAssemblyPath()
    {
        var configured = _assemblyPath ?? Environment.GetEnvironmentVariable("AUTOMATION1_DOTNET_DLL");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return Path.GetFullPath(configured);
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Aerotech",
            "Automation1-MDK",
            "APIs",
            "DotNet");

        if (Directory.Exists(root))
        {
            var candidate = Directory
                .EnumerateFiles(root, "Aerotech.Automation1.DotNet.dll", SearchOption.AllDirectories)
                .OrderByDescending(path => path.Contains("netstandard", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (candidate is not null)
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "Aerotech.Automation1.DotNet.dll을 찾을 수 없습니다. --dll 또는 AUTOMATION1_DOTNET_DLL을 지정하세요.");
    }
}
