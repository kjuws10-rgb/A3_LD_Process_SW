using System.Globalization;
using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;
using Drilling.File.Parser;

namespace Drilling.File.IPS;

public sealed class CBETFile(string configRoot) : IBETFile
{
    private static readonly IReadOnlyList<string> Headers =
    [
        "INDEX",
        "USE",
        "DESCRIPTION",
        "DIV",
        "MAG",
        "SPOTSIZE",
        "ROWBEAMSIZE"
    ];

    private readonly string _betDirectory = Path.Combine(configRoot, "BET");

    public Task<IReadOnlyList<ST_BET_TABLE_DATA>> Load(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureFiles();

        var table = ReadTable(GetValuePath());

        if (table.Count == 0)
        {
            table = ReadTable(GetFormPath());
        }

        return Task.FromResult<IReadOnlyList<ST_BET_TABLE_DATA>>(table);
    }

    public Task Save(
        IReadOnlyList<ST_BET_TABLE_DATA> table,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_betDirectory);
        WriteTable(GetValuePath(), table);
        return Task.CompletedTask;
    }

    private void EnsureFiles()
    {
        Directory.CreateDirectory(_betDirectory);

        var defaults = CreateDefaultTable();

        if (!System.IO.File.Exists(GetFormPath()))
        {
            WriteTable(GetFormPath(), defaults);
        }

        if (!System.IO.File.Exists(GetValuePath()))
        {
            WriteTable(GetValuePath(), defaults);
        }
    }

    private List<ST_BET_TABLE_DATA> ReadTable(string path)
    {
        return CCsvParser.Read(path)
            .Where(row => !string.IsNullOrWhiteSpace(CCsvParser.Get(row, "INDEX")))
            .Select((row, order) => new ST_BET_TABLE_DATA(
                ReadInt(CCsvParser.Get(row, "INDEX"), order),
                ReadBool(CCsvParser.Get(row, "USE"), true),
                ReadDouble(CCsvParser.Get(row, "MAG"), 0.0),
                ReadDouble(CCsvParser.Get(row, "DIV"), 0.0),
                ReadDouble(CCsvParser.Get(row, "ROWBEAMSIZE"), 32.64),
                ReadDouble(CCsvParser.Get(row, "SPOTSIZE"), 0.0),
                CCsvParser.Get(row, "DESCRIPTION")))
            .OrderBy(row => row.Index)
            .ToList();
    }

    private static void WriteTable(
        string path,
        IReadOnlyList<ST_BET_TABLE_DATA> table)
    {
        var rows = table
            .OrderBy(row => row.Index)
            .Select(row => new Dictionary<string, string>
            {
                ["INDEX"] = row.Index.ToString(CultureInfo.InvariantCulture),
                ["USE"] = row.Use ? "1" : "0",
                ["DESCRIPTION"] = row.Description,
                ["DIV"] = row.Divergence.ToString("F3", CultureInfo.InvariantCulture),
                ["MAG"] = row.Magnification.ToString("F3", CultureInfo.InvariantCulture),
                ["SPOTSIZE"] = row.SpotSizeOffset.ToString("F3", CultureInfo.InvariantCulture),
                ["ROWBEAMSIZE"] = row.RowBeamSize.ToString("F3", CultureInfo.InvariantCulture)
            });

        CCsvParser.Write(path, Headers, rows);
    }

    private static IReadOnlyList<ST_BET_TABLE_DATA> CreateDefaultTable()
    {
        return
        [
            new(0, true, 0.850, 1.120, 32.64, 0.000, "BET_SET_01"),
            new(1, true, 0.900, 1.080, 32.64, 0.000, "BET_SET_02"),
            new(2, true, 0.950, 1.040, 32.64, 0.000, "BET_SET_03"),
            new(3, true, 1.000, 1.000, 32.64, 0.000, "BET_SET_04"),
            new(4, true, 1.080, 0.960, 32.64, 0.000, "BET_SET_05"),
            new(5, true, 1.160, 0.920, 32.64, 0.000, "BET_SET_06"),
            new(6, true, 1.250, 0.880, 32.64, 0.000, "BET_SET_07"),
            new(7, false, 1.400, 0.820, 32.64, 0.000, "BET_SET_08")
        ];
    }

    private string GetFormPath()
    {
        return Path.Combine(configRoot, "IPS_BET.csv");
    }

    private string GetValuePath()
    {
        return Path.Combine(_betDirectory, "BET.csv");
    }

    private static bool ReadBool(string value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
            value.Trim().Equals("USE", StringComparison.OrdinalIgnoreCase) ||
            value.Trim().Equals("ON", StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadInt(string value, int defaultValue)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    private static double ReadDouble(string value, double defaultValue)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }
}





