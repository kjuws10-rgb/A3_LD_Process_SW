using System.Globalization;
using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;
using Drilling.File.Parser;

namespace Drilling.File.JHMI;

public sealed class CMotorFile(string configRoot) : IMotorFile
{
    private static readonly IReadOnlyList<string> Headers =
    [
        "NAME",
        "USE",
        "AXIS",
        "VIRTURE AXIS",
        "DEV TYPE",
        "DEV NO",
        "COORDINATE NO",
        "MOTOR TYPE",
        "SCALE",
        "SYSTEM",
        "STATION NAME",
        "SUBORDINATE",
        "DISPLAYNAME",
        "AXIS_DIR",
        "ALIGN_REVERSE",
        "PROCESS_REVERSE",
        "DIR",
        "PRODUCT INDEX",
        "AXIS_COLOR",
        "REVERSE_DIR",
        "COR_ANGLE",
        "OFFSET_X",
        "OFFSET_Y",
        "OFFSET_Z",
        "OFFSET_XT",
        "OFFSET_YT",
        "OFFSET_ZT",
        "UNIT",
        "MAXVEL",
        "INTERLOCK MAXVEL",
        "MAXACC",
        "MIN",
        "MAX",
        "HOMEPLC",
        "HOMETIMEOUT",
        "HOMEPLC FLAG",
        "DES",
        "LOAD_ALARM_VAL",
        "PRE_CHECK_IO"
    ];

    private static readonly IReadOnlyList<IReadOnlyList<string>> RequiredHeaderGroups =
    [
        ["NAME", "MOTOR NAME", "AXIS NAME"],
        ["USE"],
        ["AXIS"],
        ["DEV TYPE", "DEVICE TYPE"],
        ["DEV NO", "DEVICE NO"],
        ["SCALE"],
        ["MAXVEL"],
        ["MAXACC"],
        ["MIN"],
        ["MAX"]
    ];

    public Task<IReadOnlyList<ST_MOTOR_DATA>> LoadAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureFile();
        CCsvParser.ValidateRequiredHeaders(GetMotorPath(), "JHMI_MOTOR", RequiredHeaderGroups);

        var rows = CCsvParser.Read(GetMotorPath())
            .Select((row, index) => Parse(row, index + 2))
            .Where(axis => !string.IsNullOrWhiteSpace(axis.Name))
            .OrderBy(axis => axis.Axis)
            .ThenBy(axis => axis.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Validate(rows);
        return Task.FromResult<IReadOnlyList<ST_MOTOR_DATA>>(rows);
    }

    private ST_MOTOR_DATA Parse(
        IReadOnlyDictionary<string, string> row,
        int rowNo)
    {
        var name = ReadFirst(row, "NAME", "MOTOR NAME", "AXIS NAME");
        var displayName = ReadFirst(row, "DISPLAYNAME", "DISPLAY NAME");

        if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(displayName))
        {
            name = displayName;
        }

        return new ST_MOTOR_DATA(
            name.Trim(),
            ReadBool(ReadFirst(row, "USE"), true),
            ReadInt(ReadFirst(row, "AXIS"), rowNo, "AXIS", rowNo - 2),
            ReadInt(ReadFirst(row, "VIRTURE AXIS", "VIRTUAL AXIS"), rowNo, "VIRTURE AXIS", -1),
            ReadController(ReadFirst(row, "DEV TYPE", "DEVICE TYPE")),
            ReadInt(ReadFirst(row, "DEV NO", "DEVICE NO"), rowNo, "DEV NO", 0),
            ReadInt(ReadFirst(row, "COORDINATE NO"), rowNo, "COORDINATE NO", 0),
            ReadInt(ReadFirst(row, "MOTOR TYPE"), rowNo, "MOTOR TYPE", 0),
            ReadDouble(ReadFirst(row, "SCALE"), rowNo, "SCALE", 1.0),
            ReadFirst(row, "SYSTEM"),
            ReadFirst(row, "STATION NAME", "STATION"),
            ReadFirst(row, "SUBORDINATE"),
            string.IsNullOrWhiteSpace(displayName) ? name.Trim() : displayName.Trim(),
            ReadFirst(row, "AXIS_DIR", "AXIS DIR"),
            ReadBool(ReadFirst(row, "ALIGN_REVERSE"), false),
            ReadBool(ReadFirst(row, "PROCESS_REVERSE"), false),
            ReadFirst(row, "DIR"),
            ReadFirst(row, "PRODUCT INDEX"),
            ReadFirst(row, "AXIS_COLOR", "AXIS COLOR"),
            ReadBool(ReadFirst(row, "REVERSE_DIR"), false),
            ReadDouble(ReadFirst(row, "COR_ANGLE"), rowNo, "COR_ANGLE", 0.0),
            ReadDouble(ReadFirst(row, "OFFSET_X"), rowNo, "OFFSET_X", 0.0),
            ReadDouble(ReadFirst(row, "OFFSET_Y"), rowNo, "OFFSET_Y", 0.0),
            ReadDouble(ReadFirst(row, "OFFSET_Z"), rowNo, "OFFSET_Z", 0.0),
            ReadDouble(ReadFirst(row, "OFFSET_XT"), rowNo, "OFFSET_XT", 0.0),
            ReadDouble(ReadFirst(row, "OFFSET_YT"), rowNo, "OFFSET_YT", 0.0),
            ReadDouble(ReadFirst(row, "OFFSET_ZT"), rowNo, "OFFSET_ZT", 0.0),
            ReadFirst(row, "UNIT"),
            ReadDouble(ReadFirst(row, "MAXVEL"), rowNo, "MAXVEL", 0.0),
            ReadDouble(ReadFirst(row, "INTERLOCK MAXVEL"), rowNo, "INTERLOCK MAXVEL", 0.0),
            ReadDouble(ReadFirst(row, "MAXACC"), rowNo, "MAXACC", 0.0),
            ReadDouble(ReadFirst(row, "MIN"), rowNo, "MIN", 0.0),
            ReadDouble(ReadFirst(row, "MAX"), rowNo, "MAX", 0.0),
            ReadInt(ReadFirst(row, "HOMEPLC"), rowNo, "HOMEPLC", 0),
            ReadInt(ReadFirst(row, "HOMETIMEOUT"), rowNo, "HOMETIMEOUT", 0),
            ReadFirst(row, "HOMEPLC FLAG"),
            ReadFirst(row, "DES", "DESCRIPTION"),
            ReadDouble(ReadFirst(row, "LOAD_ALARM_VAL"), rowNo, "LOAD_ALARM_VAL", 0.0),
            ReadFirst(row, "PRE_CHECK_IO"));
    }

    private void EnsureFile()
    {
        var path = GetMotorPath();

        if (System.IO.File.Exists(path))
        {
            return;
        }

        CCsvParser.Write(path, Headers, CreateDefaultRows());
    }

    private string GetMotorPath()
    {
        return Path.Combine(configRoot, "JHMI_MOTOR.csv");
    }

    private static void Validate(IReadOnlyList<ST_MOTOR_DATA> motors)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedControllerAxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var axis in motors.Where(axis => axis.Use))
        {
            if (string.IsNullOrWhiteSpace(axis.Name))
            {
                throw new InvalidDataException("JHMI_MOTOR validation failed. NAME cannot be empty.");
            }

            if (!usedNames.Add(axis.Name))
            {
                throw new InvalidDataException($"JHMI_MOTOR validation failed. Duplicated NAME: {axis.Name}");
            }

            if (axis.Axis < 0)
            {
                throw new InvalidDataException($"JHMI_MOTOR validation failed. AXIS cannot be negative: {axis.Name}");
            }

            if (axis.DevNo < 0)
            {
                throw new InvalidDataException($"JHMI_MOTOR validation failed. DEV NO cannot be negative: {axis.Name}");
            }

            if (string.IsNullOrWhiteSpace(axis.DevType))
            {
                throw new InvalidDataException($"JHMI_MOTOR validation failed. DEV TYPE cannot be empty: {axis.Name}");
            }

            var controllerAxisKey = $"{axis.DevType}:{axis.DevNo}:{axis.Axis}";
            if (!usedControllerAxes.Add(controllerAxisKey))
            {
                throw new InvalidDataException(
                    $"JHMI_MOTOR validation failed. Duplicated controller axis: {axis.DevType}[{axis.DevNo}] AXIS {axis.Axis}");
            }

            if (axis.Scale <= 0.0)
            {
                throw new InvalidDataException($"JHMI_MOTOR validation failed. SCALE must be positive: {axis.Name}");
            }
        }
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> CreateDefaultRows()
    {
        return
        [
            Row("GX", 0, "Galvo X", "mm", -120, 120, 300, 500),
            Row("GY", 1, "Galvo Y", "mm", -120, 120, 300, 500),
            Row("X", 2, "Stage X", "mm", -500, 500, 300, 500),
            Row("Y", 3, "Stage Y", "mm", -500, 500, 300, 500),
            Row("Z", 4, "Stage Z", "mm", -100, 100, 100, 300),
            Row("THETA", 5, "Theta Rotation", "deg", -180, 180, 30, 100),
            Row("ATTENUATOR", 6, "Attenuator", "deg", -120, 360, 50, 100),
            Row("BET_MAG", 7, "Beam Expander Mag", "x", 0, 10, 10, 100),
            Row("BET_DIV", 8, "Beam Expander Div", "x", 0, 10, 10, 100),
            Row("SCANNER_01_GX", 9, "Scanner 01 GX", "mm", -120, 120, 300, 500),
            Row("SCANNER_01_GY", 10, "Scanner 01 GY", "mm", -120, 120, 300, 500),
            Row("SCANNER_02_GX", 11, "Scanner 02 GX", "mm", -120, 120, 300, 500),
            Row("SCANNER_02_GY", 12, "Scanner 02 GY", "mm", -120, 120, 300, 500)
        ];
    }

    private static IReadOnlyDictionary<string, string> Row(
        string name,
        int axis,
        string displayName,
        string unit,
        double min,
        double max,
        double maxVel,
        double maxAcc)
    {
        return new Dictionary<string, string>
        {
            ["NAME"] = name,
            ["USE"] = "1",
            ["AXIS"] = axis.ToString(CultureInfo.InvariantCulture),
            ["VIRTURE AXIS"] = "-1",
            ["DEV TYPE"] = "AUTOMATION1",
            ["DEV NO"] = "0",
            ["COORDINATE NO"] = "0",
            ["MOTOR TYPE"] = "0",
            ["SCALE"] = "1000",
            ["SYSTEM"] = "MOTION",
            ["STATION NAME"] = "DRILLING",
            ["SUBORDINATE"] = "",
            ["DISPLAYNAME"] = displayName,
            ["AXIS_DIR"] = "",
            ["ALIGN_REVERSE"] = "0",
            ["PROCESS_REVERSE"] = "0",
            ["DIR"] = "",
            ["PRODUCT INDEX"] = "",
            ["AXIS_COLOR"] = "CYAN",
            ["REVERSE_DIR"] = "0",
            ["COR_ANGLE"] = "0",
            ["OFFSET_X"] = "0",
            ["OFFSET_Y"] = "0",
            ["OFFSET_Z"] = "0",
            ["OFFSET_XT"] = "0",
            ["OFFSET_YT"] = "0",
            ["OFFSET_ZT"] = "0",
            ["UNIT"] = unit,
            ["MAXVEL"] = maxVel.ToString("F3", CultureInfo.InvariantCulture),
            ["INTERLOCK MAXVEL"] = maxVel.ToString("F3", CultureInfo.InvariantCulture),
            ["MAXACC"] = maxAcc.ToString("F3", CultureInfo.InvariantCulture),
            ["MIN"] = min.ToString("F3", CultureInfo.InvariantCulture),
            ["MAX"] = max.ToString("F3", CultureInfo.InvariantCulture),
            ["HOMEPLC"] = "0",
            ["HOMETIMEOUT"] = "30000",
            ["HOMEPLC FLAG"] = "",
            ["DES"] = displayName,
            ["LOAD_ALARM_VAL"] = "0",
            ["PRE_CHECK_IO"] = ""
        };
    }

    private static string ReadFirst(
        IReadOnlyDictionary<string, string> row,
        params string[] names)
    {
        return CCsvParser.GetFirst(row, names);
    }

    private static string ReadController(string value)
    {
        var controller = Normalize(value);
        return string.IsNullOrWhiteSpace(controller) ? "AUTOMATION1" : controller;
    }

    private static bool ReadBool(string value, bool defaultValue)
    {
        return CCsvParser.ReadBool(value, defaultValue);
    }

    private static int ReadInt(
        string value,
        int rowNo,
        string fieldName,
        int defaultValue)
    {
        return CCsvParser.ReadInt(value, "JHMI_MOTOR", rowNo, fieldName, defaultValue);
    }

    private static double ReadDouble(
        string value,
        int rowNo,
        string fieldName,
        double defaultValue)
    {
        return CCsvParser.ReadDouble(value, "JHMI_MOTOR", rowNo, fieldName, defaultValue);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant().Replace(" ", "_", StringComparison.OrdinalIgnoreCase);
    }
}





