using System.Net;
using System.Net.Sockets;
using MofCoordinateDemo.Automation1;
using MofCoordinateDemo.Models;
using MofCoordinateDemo.Services;

var arguments = ParseArguments(args);
if (arguments.ContainsKey("self-test"))
{
    await RunSelfTestAsync();
    return;
}

var bind = Get(arguments, "bind", "0.0.0.0");
var port = GetInt(arguments, "port", 46100);
var apiKey = Get(arguments, "api-key", Environment.GetEnvironmentVariable("A1_SCRIPT_API_KEY") ?? "change-this-key");
var spool = Path.GetFullPath(Get(arguments, "spool", Path.Combine(AppContext.BaseDirectory, "script-spool")));
var runtimeName = Get(arguments, "runtime", "simulation");

IAutomation1Runtime runtime = runtimeName.Equals("automation1", StringComparison.OrdinalIgnoreCase)
    ? new Automation1ReflectionRuntime(Get(arguments, "controller", ""), Get(arguments, "dll", ""))
    : new SimulationAutomation1Runtime();

var options = new ScriptServerOptions(
    bind,
    port,
    apiKey,
    spool,
    24 * 1024 * 1024,
    TimeSpan.FromMinutes(GetInt(arguments, "timeout-minutes", 30)));

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

Console.WriteLine($"Automation1 Script Server: {bind}:{port}");
Console.WriteLine($"Runtime: {runtimeName}, Spool: {spool}");
Console.WriteLine("Client가 생성한 AeroScript만 수신하며, 서버에서는 좌표/script를 생성하지 않습니다.");
Console.WriteLine("종료: Ctrl+C");

await using var server = new AeroScriptServer(options, runtime);
await server.RunAsync(shutdown.Token);

static Dictionary<string, string> ParseArguments(string[] args)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var argument in args)
    {
        if (!argument.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var pair = argument[2..].Split('=', 2);
        values[pair[0]] = pair.Length == 2 ? pair[1] : "true";
    }

    return values;
}

static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
    values.TryGetValue(key, out var value) ? value : fallback;

static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback) =>
    values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

static async Task RunSelfTestAsync()
{
    var port = GetFreeTcpPort();
    var apiKey = "self-test-key";
    var spool = Path.Combine(Path.GetTempPath(), "MofCoordinateDemo", "server-self-test", Guid.NewGuid().ToString("N"));
    var options = new ScriptServerOptions(
        "127.0.0.1",
        port,
        apiKey,
        spool,
        1024 * 1024,
        TimeSpan.FromSeconds(10));

    using var shutdown = new CancellationTokenSource();
    await using var server = new AeroScriptServer(options, new SimulationAutomation1Runtime(TimeSpan.FromMilliseconds(150)));
    var serverTask = server.RunAsync(shutdown.Token);

    try
    {
        await Task.Delay(100);
        var coordinateInput = new CoordinateInput();
        var coordinateResult = new CoordinateTransformService().Generate(coordinateInput);
        var generatedSource = new AeroScriptGenerator().Generate(
            coordinateInput,
            coordinateResult.MofExecutionCommands.Take(5).ToArray(),
            new AeroScriptGenerationOptions("Gx{0}", "Gy{0}", 100, true, false));
        if (!generatedSource.Contains("MoveLinear", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AeroScript generator did not create motion commands.");
        }

        var client = new AeroScriptClient("127.0.0.1", port, apiKey);
        var package = AeroScriptPackage.Create("programs/self-test.ascript", generatedSource, 1);

        EnsureSuccess(await client.UploadAsync(package, CancellationToken.None), "upload");
        EnsureSuccess(await client.RunAsync(package.JobId, CancellationToken.None), "run");

        ScriptServerResponse response;
        do
        {
            await Task.Delay(50);
            response = await client.GetStatusAsync(package.JobId, CancellationToken.None);
            EnsureSuccess(response, "status");
        }
        while (response.Job?.State is not ScriptJobState.Completed and not ScriptJobState.Failed);

        if (response.Job?.State != ScriptJobState.Completed)
        {
            throw new InvalidOperationException($"Self-test failed: {response.Job?.Message}");
        }

        Console.WriteLine("SELF-TEST PASS: client generation -> upload -> run -> completion verified.");
    }
    finally
    {
        shutdown.Cancel();
        await serverTask;
        if (Directory.Exists(spool))
        {
            Directory.Delete(spool, recursive: true);
        }
    }
}

static void EnsureSuccess(ScriptServerResponse response, string operation)
{
    if (!response.Success)
    {
        throw new InvalidOperationException($"{operation}: {response.ErrorCode} {response.Message}");
    }
}

static int GetFreeTcpPort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
