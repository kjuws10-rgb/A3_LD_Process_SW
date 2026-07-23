using System.Globalization;
using Drilling.Common.Interface;
using Drilling.File.Parser;

namespace Drilling.File.JHMI;

public sealed class CMelsecMapFile(string configRoot) : IMelsecMapFile
{
    private const string TableName = "JHMI_MELSEC_MAP";

    private static readonly IReadOnlyList<string> Headers =
    [
        "ID",
        "USE",
        "GROUP",
        "NAME",
        "DEVICE NO",
        "ADDRESS",
        "DATA TYPE",
        "DIRECTION",
        "ACCESS",
        "SCALE",
        "LENGTH",
        "POLL_MS",
        "DESCRIPTION"
    ];

    private static readonly IReadOnlyList<IReadOnlyList<string>> RequiredHeaderGroups =
    [
        ["ID"],
        ["USE"],
        ["GROUP"],
        ["NAME"],
        ["DEVICE NO", "DEV NO", "NUMBER"],
        ["ADDRESS"],
        ["DATA TYPE", "DATATYPE", "TYPE"],
        ["DIRECTION", "DIR"],
        ["ACCESS"],
        ["SCALE"],
        ["LENGTH", "SIZE"],
        ["POLL_MS", "POLL MS", "POLL"]
    ];

    public Task<IReadOnlyList<ST_MELSEC_MAP_DATA>> LoadAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureFile();
        CCsvParser.ValidateRequiredHeaders(GetMapPath(), TableName, RequiredHeaderGroups);

        var rows = CCsvParser.Read(GetMapPath())
            .Select((row, index) => Parse(row, index + 2))
            .Where(data => !string.IsNullOrWhiteSpace(data.Id))
            .OrderBy(data => data.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(data => data.DeviceNo)
            .ThenBy(data => data.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Validate(rows);
        return Task.FromResult<IReadOnlyList<ST_MELSEC_MAP_DATA>>(rows);
    }

    private ST_MELSEC_MAP_DATA Parse(
        IReadOnlyDictionary<string, string> row,
        int rowNo)
    {
        return new ST_MELSEC_MAP_DATA(
            NormalizeId(RequireText(row, rowNo, "ID")),
            ReadBool(ReadFirst(row, "USE"), true),
            NormalizeGroup(RequireText(row, rowNo, "GROUP")),
            ReadFirst(row, "NAME", "DISPLAY NAME"),
            ReadInt(ReadFirst(row, "DEVICE NO", "DEV NO", "NUMBER"), rowNo, "DEVICE NO", 0),
            NormalizeAddress(RequireText(row, rowNo, "ADDRESS")),
            ReadDataType(RequireText(row, rowNo, "DATA TYPE", "DATATYPE", "TYPE"), rowNo),
            ReadDirection(RequireText(row, rowNo, "DIRECTION", "DIR"), rowNo),
            ReadAccess(RequireText(row, rowNo, "ACCESS"), rowNo),
            ReadDouble(ReadFirst(row, "SCALE"), rowNo, "SCALE", 1.0),
            ReadInt(ReadFirst(row, "LENGTH", "SIZE"), rowNo, "LENGTH", 1),
            ReadInt(ReadFirst(row, "POLL_MS", "POLL MS", "POLL"), rowNo, "POLL_MS", 0),
            ReadFirst(row, "DESCRIPTION", "DESC"));
    }

    private void EnsureFile()
    {
        var path = GetMapPath();

        if (System.IO.File.Exists(path))
        {
            return;
        }

        CCsvParser.Write(path, Headers, CreateDefaultRows());
    }

    private string GetMapPath()
    {
        return Path.Combine(configRoot, "JHMI_MELSEC_MAP.csv");
    }

    private static void Validate(IReadOnlyList<ST_MELSEC_MAP_DATA> rows)
    {
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Where(row => row.Use))
        {
            if (string.IsNullOrWhiteSpace(row.Id))
            {
                throw new InvalidDataException($"{TableName} validation failed. ID cannot be empty.");
            }

            if (!usedIds.Add(row.Id))
            {
                throw new InvalidDataException($"{TableName} validation failed. Duplicated ID: {row.Id}");
            }

            if (row.DeviceNo < 0)
            {
                throw new InvalidDataException($"{TableName} validation failed. DEVICE NO cannot be negative: {row.Id}");
            }

            if (string.IsNullOrWhiteSpace(row.Address))
            {
                throw new InvalidDataException($"{TableName} validation failed. ADDRESS cannot be empty: {row.Id}");
            }

            if (row.Length <= 0)
            {
                throw new InvalidDataException($"{TableName} validation failed. LENGTH must be positive: {row.Id}");
            }

            if (row.PollMs < 0)
            {
                throw new InvalidDataException($"{TableName} validation failed. POLL_MS cannot be negative: {row.Id}");
            }

            if (row.DataType == EN_MELSEC_DATA_TYPE.Bit && row.Length != 1)
            {
                throw new InvalidDataException($"{TableName} validation failed. BIT LENGTH must be 1: {row.Id}");
            }
        }
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> CreateDefaultRows()
    {
        return
        [
            Row("PROCESS_ALIVE", "PROCESS", "Process Alive", 0, "W30000.0", "BIT", "OUT", "RW", "1", "1", "100", "Process PC alive bit"),
            Row("PROCESS_ALARM_OCCUR", "PROCESS", "Process Alarm Occur", 0, "W30000.1", "BIT", "OUT", "RW", "1", "1", "100", "Process alarm state"),
            Row("PROCESS_AUTO_MODE", "PROCESS", "Process Auto Mode", 0, "W30000.2", "BIT", "OUT", "RW", "1", "1", "100", "Process auto mode state"),
            Row("PROCESS_COMPLETE", "PROCESS", "Process Complete", 0, "W30000.3", "BIT", "OUT", "RW", "1", "1", "100", "Process complete signal"),
            Row("STAGE_Y_TARGET_POS", "STAGE", "Stage Y Target Position", 0, "W30100", "DOUBLE", "OUT", "W", "0.001", "2", "0", "Stage Y target position"),
            Row("STAGE_Y_TARGET_SPEED", "STAGE", "Stage Y Target Speed", 0, "W30102", "DOUBLE", "OUT", "W", "0.001", "2", "0", "Stage Y target speed"),
            Row("REVIEW_X_TARGET_POS", "STAGE", "Review X Target Position", 0, "W30104", "DOUBLE", "OUT", "W", "0.001", "2", "0", "Review camera X target position"),
            Row("REVIEW_X_TARGET_SPEED", "STAGE", "Review X Target Speed", 0, "W30106", "DOUBLE", "OUT", "W", "0.001", "2", "0", "Review camera X target speed"),
            Row("AXIS_MOVE_START_REQ", "STAGE", "Axis Move Start Request", 0, "W30120.0", "BIT", "OUT", "W", "1", "1", "0", "Stage/review axis move start request"),
            Row("AXIS_MOVE_STOP_REQ", "STAGE", "Axis Move Stop Request", 0, "W30120.1", "BIT", "OUT", "W", "1", "1", "0", "Stage/review axis move stop request"),
            Row("AXIS_MOVE_COMPLETE", "STAGE", "Axis Move Complete", 0, "W30121.0", "BIT", "IN", "R", "1", "1", "50", "Stage/review axis move complete"),
            Row("AXIS_MOVE_ERROR", "STAGE", "Axis Move Error", 0, "W30121.1", "BIT", "IN", "R", "1", "1", "50", "Stage/review axis move error"),
            Row("REVIEW_TYPE", "REVIEW", "Review Type", 0, "W30200", "WORD", "OUT", "W", "1", "1", "0", "Review inspection type"),
            Row("REVIEW_TRIGGER_COUNT", "REVIEW", "Review Trigger Count", 0, "W30201", "WORD", "OUT", "W", "1", "1", "0", "Review IOF trigger count"),
            Row("REVIEW_START_REQ", "REVIEW", "Review Start Request", 0, "W30210.0", "BIT", "OUT", "W", "1", "1", "0", "Review sequence start request"),
            Row("REVIEW_SEQUENCE_COMPLETE", "REVIEW", "Review Sequence Complete", 0, "W30211.0", "BIT", "IN", "R", "1", "1", "50", "Review sequence complete signal"),
            Row("GLASS_ID", "PRODUCT", "Glass ID", 0, "W31000", "STRING", "IN", "R", "1", "20", "500", "Current glass id"),
            Row("PPID", "PRODUCT", "PPID", 0, "W31020", "STRING", "IN", "R", "1", "20", "500", "Current PPID"),
            Row("EQ_RECIPE", "PRODUCT", "EQ Recipe", 0, "W31040", "STRING", "IN", "R", "1", "20", "500", "Current equipment recipe"),
            Row("OPTIC_STATUS_ALL", "OPTIC", "Optic Status All", 0, "W32000", "WORD", "IN", "R", "1", "1", "500", "Optic total status check")
        ];
    }

    private static IReadOnlyDictionary<string, string> Row(
        string id,
        string group,
        string name,
        int deviceNo,
        string address,
        string dataType,
        string direction,
        string access,
        string scale,
        string length,
        string pollMs,
        string description)
    {
        return new Dictionary<string, string>
        {
            ["ID"] = id,
            ["USE"] = "1",
            ["GROUP"] = group,
            ["NAME"] = name,
            ["DEVICE NO"] = deviceNo.ToString(CultureInfo.InvariantCulture),
            ["ADDRESS"] = address,
            ["DATA TYPE"] = dataType,
            ["DIRECTION"] = direction,
            ["ACCESS"] = access,
            ["SCALE"] = scale,
            ["LENGTH"] = length,
            ["POLL_MS"] = pollMs,
            ["DESCRIPTION"] = description
        };
    }

    private static EN_MELSEC_DATA_TYPE ReadDataType(string value, int rowNo)
    {
        return NormalizeText(value) switch
        {
            "BIT" => EN_MELSEC_DATA_TYPE.Bit,
            "WORD" => EN_MELSEC_DATA_TYPE.Word,
            "DWORD" or "D_WORD" or "DOUBLEWORD" => EN_MELSEC_DATA_TYPE.DWord,
            "DOUBLE" or "REAL64" => EN_MELSEC_DATA_TYPE.Double,
            "FLOAT" or "REAL32" => EN_MELSEC_DATA_TYPE.Float,
            "STRING" or "TEXT" => EN_MELSEC_DATA_TYPE.String,
            _ => throw new InvalidDataException($"{TableName} validation failed. Row {rowNo} / DATA TYPE is invalid: {value}")
        };
    }

    private static EN_MELSEC_DIRECTION ReadDirection(string value, int rowNo)
    {
        return NormalizeText(value) switch
        {
            "IN" or "INPUT" => EN_MELSEC_DIRECTION.In,
            "OUT" or "OUTPUT" => EN_MELSEC_DIRECTION.Out,
            "INOUT" or "IN_OUT" or "BOTH" => EN_MELSEC_DIRECTION.InOut,
            _ => throw new InvalidDataException($"{TableName} validation failed. Row {rowNo} / DIRECTION is invalid: {value}")
        };
    }

    private static EN_MELSEC_ACCESS ReadAccess(string value, int rowNo)
    {
        return NormalizeText(value) switch
        {
            "R" or "READ" => EN_MELSEC_ACCESS.Read,
            "W" or "WRITE" => EN_MELSEC_ACCESS.Write,
            "RW" or "R/W" or "READWRITE" or "READ_WRITE" => EN_MELSEC_ACCESS.ReadWrite,
            _ => throw new InvalidDataException($"{TableName} validation failed. Row {rowNo} / ACCESS is invalid: {value}")
        };
    }

    private static string RequireText(
        IReadOnlyDictionary<string, string> row,
        int rowNo,
        params string[] names)
    {
        return CCsvParser.RequireText(row, TableName, rowNo, names);
    }

    private static string ReadFirst(
        IReadOnlyDictionary<string, string> row,
        params string[] names)
    {
        return CCsvParser.GetFirst(row, names);
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
        return CCsvParser.ReadInt(value, TableName, rowNo, fieldName, defaultValue);
    }

    private static double ReadDouble(
        string value,
        int rowNo,
        string fieldName,
        double defaultValue)
    {
        return CCsvParser.ReadDouble(value, TableName, rowNo, fieldName, defaultValue);
    }

    private static string NormalizeId(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeGroup(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeAddress(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeText(string value)
    {
        return value.Trim()
            .ToUpperInvariant()
            .Replace(" ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-", "_", StringComparison.OrdinalIgnoreCase);
    }
}
