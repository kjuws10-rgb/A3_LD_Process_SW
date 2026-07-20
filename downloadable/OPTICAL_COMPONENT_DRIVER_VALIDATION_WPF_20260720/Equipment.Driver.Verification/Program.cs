using Equipment.Driver;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("Six manual component profiles", VerifyProfiles),
    ("Manual command formatting and limits", VerifyFormatting),
    ("All component read-only simulator validation", VerifyAllSimulators),
    ("Attenuator and beam expander motion state", VerifyOpticalMotion),
    ("XPS API result and position", VerifyXps),
    ("Picomotor CmdLib lifecycle", VerifyPicomotor),
    ("Hardware write safety lock", VerifySafetyLock),
    ("Timeout fault propagation", VerifyTimeout)
};

var failed = 0;
foreach (var test in tests)
{
    try { await test.Run(); Console.WriteLine($"PASS | {test.Name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL | {test.Name} | {ex.Message}"); }
}
Console.WriteLine($"RESULT | PASS={tests.Count - failed} FAIL={failed}");
return failed == 0 ? 0 : 1;

static Task VerifyProfiles()
{
    Equal(6, CEquipmentCatalog.AllProfiles.Count);
    Equal(921600, CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.ConexAgpAttenuator).DefaultBaudRate);
    Equal(5001, CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.XpsController).DefaultTcpPort);
    return Task.CompletedTask;
}

static Task VerifyFormatting()
{
    var conex = Find(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "move-abs");
    Equal("1PA12.345", CEquipmentProtocol.Build(conex, 12.345));
    var bet = Find(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "move1");
    Equal("#1:1600", CEquipmentProtocol.Build(bet, 1600));
    Throws<ArgumentOutOfRangeException>(() => CEquipmentProtocol.Build(bet, 4501));
    var wave = Find(EN_EQUIPMENT_TYPE.PowerMaxMeter, "set-wavelength");
    Equal("wv 3.55E-7", CEquipmentProtocol.Build(wave, 355e-9));
    return Task.CompletedTask;
}

static async Task VerifyAllSimulators()
{
    foreach (var profile in CEquipmentCatalog.AllProfiles)
    {
        await using var driver = new CEquipmentDriver(profile, new CSimulatorEquipmentTransport(profile));
        await driver.Connect();
        var result = await driver.RunReadOnlyValidation();
        Equal(true, result.Count > 0);
        Equal(true, result.All(item => item.Passed));
    }
}

static async Task VerifyOpticalMotion()
{
    var safety = new ST_EQUIPMENT_SAFETY(true, true, true, true);
    var profile = CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.ConexAgpAttenuator);
    await using var driver = new CEquipmentDriver(profile, new CSimulatorEquipmentTransport(profile));
    await driver.Connect();
    await driver.Execute(Find(profile.Type, "move-abs"), 20.5, safety);
    Equal("1TP20.500", await driver.Execute(Find(profile.Type, "position")));

    var betProfile = CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.MotorizedBeamExpander);
    await using var bet = new CEquipmentDriver(betProfile, new CSimulatorEquipmentTransport(betProfile));
    await bet.Connect();
    await bet.Execute(Find(betProfile.Type, "move1"), 2100, safety);
    Equal("$7:2100", await bet.Execute(Find(betProfile.Type, "motor1")));
}

static async Task VerifyXps()
{
    var profile = CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.XpsController);
    await using var driver = new CEquipmentDriver(profile, new CSimulatorEquipmentTransport(profile));
    await driver.Connect();
    var safety = new ST_EQUIPMENT_SAFETY(true, true, true, true);
    await driver.Execute(Find(profile.Type, "move-abs"), 42.125, safety);
    Equal("0,42.125000,EndOfAPI", await driver.Execute(Find(profile.Type, "position")));
}

static async Task VerifyPicomotor()
{
    var profile = CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.Picomotor);
    await using var driver = new CEquipmentDriver(profile, new CSimulatorEquipmentTransport(profile));
    await driver.Connect();
    var result = await driver.RunReadOnlyValidation();
    Equal(true, result.Any(item => item.Item == "장비 검색" && item.Passed));
    Equal(true, result.Any(item => item.Item == "Motion 완료" && item.Actual == "1"));
}

static async Task VerifySafetyLock()
{
    var profile = CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.XpsController);
    await using var driver = new CEquipmentDriver(profile, new CSimulatorEquipmentTransport(profile));
    await driver.Connect();
    await ThrowsAsync<InvalidOperationException>(() => driver.Execute(Find(profile.Type, "move-rel"), 1));
}

static async Task VerifyTimeout()
{
    var profile = CEquipmentCatalog.GetProfile(EN_EQUIPMENT_TYPE.PowerMaxMeter);
    var simulator = new CSimulatorEquipmentTransport(profile) { InjectTimeoutOnce = true };
    await using var driver = new CEquipmentDriver(profile, simulator);
    await driver.Connect();
    await ThrowsAsync<TimeoutException>(() => driver.Execute(Find(profile.Type, "power")));
}

static ST_EQUIPMENT_COMMAND_SPEC Find(EN_EQUIPMENT_TYPE type, string id) => CEquipmentCatalog.GetCommands(type).Single(item => item.Id == id);
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'."); }
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
