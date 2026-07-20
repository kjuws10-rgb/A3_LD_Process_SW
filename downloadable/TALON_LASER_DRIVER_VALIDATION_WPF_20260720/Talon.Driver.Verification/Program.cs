using Talon.Driver;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("Manual command formatting", VerifyCommandFormatting),
    ("Parameter range protection", VerifyParameterRange),
    ("CR/LF and echo normalization", VerifyResponseNormalization),
    ("Status history and status bits", VerifyStatusParsing),
    ("Simulator read-only validation", VerifySimulatorValidation),
    ("Hardware output safety lock", VerifyHardwareSafetyLock)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS | {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL | {test.Name} | {ex.Message}");
    }
}

Console.WriteLine($"RESULT | PASS={tests.Count - failures} FAIL={failures}");
return failures == 0 ? 0 : 1;

static Task VerifyCommandFormatting()
{
    Equal("?C1", CTalonProtocol.Build(EN_TALON_COMMAND.QueryDiodeCurrent));
    Equal("C1:5.80", CTalonProtocol.Build(EN_TALON_COMMAND.SetDiodeCurrent, 5.8));
    Equal("Q:100000", CTalonProtocol.Build(EN_TALON_COMMAND.SetRepetitionRate, 100000));
    Equal("QMODE?", CTalonProtocol.Build(EN_TALON_COMMAND.QueryQMode));
    Equal("?FH", CTalonProtocol.Build(EN_TALON_COMMAND.QueryStatusHistory));
    Equal("*STB?", CTalonProtocol.Build(EN_TALON_COMMAND.QueryStatusByte));
    Equal("MTR:TSPOT:15", CTalonProtocol.Build(EN_TALON_COMMAND.SetThgSpot, 15));
    return Task.CompletedTask;
}

static Task VerifyParameterRange()
{
    Throws<ArgumentOutOfRangeException>(() => CTalonProtocol.Build(EN_TALON_COMMAND.SetQMode, 3));
    Throws<ArgumentOutOfRangeException>(() => CTalonProtocol.Build(EN_TALON_COMMAND.SetThgSpot, 0));
    Throws<ArgumentOutOfRangeException>(() => CTalonProtocol.Build(EN_TALON_COMMAND.SetBaudRate, 4800));
    return Task.CompletedTask;
}

static Task VerifyResponseNormalization()
{
    Equal("23.25", CTalonProtocol.NormalizeResponse("?T1", "?T1\r\n23.25\r\n"));
    Equal("SYSTEM READY", CTalonProtocol.NormalizeResponse("?F", "SYSTEM READY\r"));
    Equal(23.25, CTalonProtocol.ReadDouble("23.25 C"));
    Equal(true, CTalonProtocol.ReadBoolean("OPEN"));
    return Task.CompletedTask;
}

static Task VerifyStatusParsing()
{
    var history = CTalonProtocol.ReadStatusHistory("000;013;011;000");
    Equal(4, history.Count);
    Equal(13, history[1]);

    var bits = ST_TALON_STATUS_BITS.FromRaw((1 << 0) | (1 << 2) | (1 << 5) | (1 << 9));
    Equal(true, bits.Emission);
    Equal(true, bits.GateOpen);
    Equal(true, bits.SystemFault);
    Equal(true, bits.MotorMoving);
    Equal(false, bits.ShutterOpen);
    return Task.CompletedTask;
}

static async Task VerifySimulatorValidation()
{
    await using var driver = new CTalonDriver(new CTalonSimulatorTransport());
    await driver.Connect();
    var result = await driver.RunReadOnlyValidation();
    Equal(14, result.Count);
    Equal(true, result.All(item => item.Passed));
    Equal(16, driver.Status.StatusHistory.Count);
    Equal("SYSTEM READY", driver.Status.SystemStatus);
}

static async Task VerifyHardwareSafetyLock()
{
    await using var transport = new CRecordingTransport();
    await using var driver = new CTalonDriver(transport);
    await driver.Connect();
    await driver.Execute(EN_TALON_COMMAND.QuerySystemStatus);

    await ThrowsAsync<InvalidOperationException>(() => driver.Execute(EN_TALON_COMMAND.TurnEmissionOn));
    Equal(false, transport.Commands.Contains("ON"));

    var safe = new ST_TALON_SAFETY_CONTEXT(true, true, true, true);
    await driver.Execute(EN_TALON_COMMAND.TurnEmissionOn, safety: safe);
    Equal(true, transport.Commands.Contains("ON"));

    // Output-reducing commands remain available even if the safety unlock is not set.
    await driver.Execute(EN_TALON_COMMAND.TurnEmissionOff);
    Equal(true, transport.Commands.Contains("OFF"));
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
    }
}

static void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException($"Expected exception {typeof(T).Name}.");
}

static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
{
    try
    {
        await action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException($"Expected exception {typeof(T).Name}.");
}

sealed class CRecordingTransport : ITalonTransport
{
    public List<string> Commands { get; } = [];
    public EN_TALON_CONNECTION_STATE ConnectionState { get; private set; }
    public string Endpoint => "TEST:RECORDING";

    public Task Connect(CancellationToken cancellationToken = default)
    {
        ConnectionState = EN_TALON_CONNECTION_STATE.Online;
        return Task.CompletedTask;
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        ConnectionState = EN_TALON_CONNECTION_STATE.Offline;
        return Task.CompletedTask;
    }

    public Task<string> Exchange(string command, bool expectResponse, CancellationToken cancellationToken = default)
    {
        Commands.Add(command);
        return Task.FromResult(command == "?F" ? "SYSTEM READY" : expectResponse ? "0" : "SENT");
    }

    public ValueTask DisposeAsync()
    {
        ConnectionState = EN_TALON_CONNECTION_STATE.Offline;
        return ValueTask.CompletedTask;
    }
}
