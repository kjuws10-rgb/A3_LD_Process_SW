using System.Globalization;
using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;
using Drilling.File.Parser;

namespace Drilling.File.JHMI;

public sealed class CManualScanFile(string configRoot) : IManualScanFile
{
    private const string DefaultSettingName = "CIRCLE_TEST.scan";

    private static readonly IReadOnlyList<string> ValueHeaders =
    [
        "NAME",
        "VALUE"
    ];

    private readonly string _manualDirectory = Path.Combine(configRoot, "Manual");

    public Task<IReadOnlyList<string>> List(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(_manualDirectory))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var settingNames = Directory
            .EnumerateFiles(_manualDirectory, "*.scan")
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(settingNames);
    }

    public Task<ST_MANUAL_SCAN_PARAM> Load(CancellationToken cancellationToken = default)
    {
        return Load(GetDefaultSettingName(), cancellationToken);
    }

    public Task<ST_MANUAL_SCAN_PARAM> Load(
        string settingName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var formItems = LoadFormItems();
        var values = CCsvParser.Read(GetSettingPath(settingName))
            .Where(row => !string.IsNullOrWhiteSpace(CCsvParser.Get(row, "NAME")))
            .ToDictionary(
                row => CCsvParser.Get(row, "NAME"),
                row => CCsvParser.Get(row, "VALUE"),
                StringComparer.OrdinalIgnoreCase);

        var settings = new ST_MANUAL_SCAN_PARAM(
            ReadDouble(values, formItems, "ShapeSize", 0.350),
            ReadDouble(values, formItems, "OffsetX", 0.000),
            ReadDouble(values, formItems, "OffsetY", 0.000),
            ReadString(values, formItems, "Direction", "CW"),
            ReadString(values, formItems, "ShapeName", "Circle"));

        return Task.FromResult(settings);
    }

    public Task Save(ST_MANUAL_SCAN_PARAM settings, CancellationToken cancellationToken = default)
    {
        return Save(GetDefaultSettingName(), settings, cancellationToken);
    }

    public Task Save(
        string settingName,
        ST_MANUAL_SCAN_PARAM settings,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedName = NormalizeSettingName(settingName);
        var formItems = LoadFormItems();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ShapeSize"] = settings.ShapeSize.ToString("F3", CultureInfo.InvariantCulture),
            ["OffsetX"] = settings.OffsetX.ToString("F3", CultureInfo.InvariantCulture),
            ["OffsetY"] = settings.OffsetY.ToString("F3", CultureInfo.InvariantCulture),
            ["Direction"] = settings.Direction,
            ["ShapeName"] = settings.ShapeName
        };

        ValidateValues(formItems, values);

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows =
            formItems
                .Where(item => item.Use)
                .OrderBy(item => item.DisplayOrder)
                .Select(item => new Dictionary<string, string>
                {
                    ["NAME"] = item.Name,
                    ["VALUE"] = values.TryGetValue(item.Name, out var value) ? value : item.DefaultValue
                })
                .ToArray();

        CCsvParser.Write(GetSettingPath(normalizedName), ValueHeaders, rows);
        ValidateSavedSetting(normalizedName, values);
        return Task.CompletedTask;
    }

    public Task Rename(
        string oldSettingName,
        string newSettingName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var oldPath = GetSettingPath(oldSettingName);
        var newPath = GetSettingPath(newSettingName);

        if (!System.IO.File.Exists(oldPath))
        {
            throw new FileNotFoundException($"Manual setting file was not found: {NormalizeSettingName(oldSettingName)}");
        }

        if (System.IO.File.Exists(newPath))
        {
            throw new IOException($"Manual setting file already exists: {NormalizeSettingName(newSettingName)}");
        }

        Directory.CreateDirectory(_manualDirectory);
        System.IO.File.Move(oldPath, newPath);
        return Task.CompletedTask;
    }

    public Task Delete(string settingName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetSettingPath(settingName);

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetDefaultSettingName()
    {
        if (System.IO.File.Exists(GetSettingPath(DefaultSettingName)))
        {
            return DefaultSettingName;
        }

        return Directory.Exists(_manualDirectory)
            ? Directory
                .EnumerateFiles(_manualDirectory, "*.scan")
                .Select(Path.GetFileName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                ?? DefaultSettingName
            : DefaultSettingName;
    }

    private IReadOnlyList<ST_MANUAL_FORM_ITEM> LoadFormItems()
    {
        var formItems = CCsvParser.Read(GetFormPath())
            .Select((row, index) => new ST_MANUAL_FORM_ITEM(
                CCsvParser.Get(row, "NAME"),
                GetOrDefault(CCsvParser.Get(row, "DISPLAY NAME"), CCsvParser.Get(row, "NAME")),
                ReadDataType(CCsvParser.Get(row, "DATA TYPE")),
                CCsvParser.Get(row, "UNIT"),
                ReadBool(CCsvParser.Get(row, "SHOW"), true),
                ReadBool(CCsvParser.Get(row, "USE"), true),
                CCsvParser.Get(row, "VALUE"),
                ReadDoubleValue(CCsvParser.Get(row, "MIN"), 0.0),
                ReadDoubleValue(CCsvParser.Get(row, "MAX"), 0.0),
                CCsvParser.Get(row, "DESCRIPTION"),
                ReadInt(CCsvParser.Get(row, "ORDER"), index + 1)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .ToArray();

        return formItems.Length > 0
            ? formItems
            : CreateFallbackFormItems();
    }

    private void ValidateSavedSetting(
        string settingName,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var actualValues = CCsvParser.Read(GetSettingPath(settingName))
            .Where(row => !string.IsNullOrWhiteSpace(CCsvParser.Get(row, "NAME")))
            .ToDictionary(
                row => CCsvParser.Get(row, "NAME"),
                row => CCsvParser.Get(row, "VALUE"),
                StringComparer.OrdinalIgnoreCase);

        foreach (var (name, expectedValue) in values)
        {
            if (!actualValues.TryGetValue(name, out var actualValue))
            {
                throw new InvalidDataException($"Manual setting CSV validation failed. Missing parameter: {name}");
            }

            if (!actualValue.Equals(expectedValue, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Manual setting CSV validation failed. {name}: expected '{expectedValue}', actual '{actualValue}'");
            }
        }
    }

    private static void ValidateValues(
        IReadOnlyList<ST_MANUAL_FORM_ITEM> formItems,
        IReadOnlyDictionary<string, string> values)
    {
        var formItemsByName = formItems
            .Where(item => item.Use)
            .ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in values)
        {
            if (!formItemsByName.TryGetValue(name, out var formItem))
            {
                throw new InvalidDataException($"Manual setting save blocked. Unknown parameter: {name}");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException($"Manual setting save blocked. {formItem.DisplayName} cannot be empty.");
            }

            var validationMessage = formItem.DataType switch
            {
                EN_RECIPE_DATA_TYPE.Int => ValidateIntParameter(formItem, value),
                EN_RECIPE_DATA_TYPE.Double => ValidateDoubleParameter(formItem, value),
                EN_RECIPE_DATA_TYPE.Bool => ValidateBoolParameter(formItem, value),
                _ => ""
            };

            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                throw new InvalidDataException(validationMessage);
            }
        }
    }

    private static string ValidateIntParameter(
        ST_MANUAL_FORM_ITEM formItem,
        string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return $"Manual setting save blocked. {formItem.DisplayName} must be an integer.";
        }

        return ValidateNumericRange(formItem, parsed);
    }

    private static string ValidateDoubleParameter(
        ST_MANUAL_FORM_ITEM formItem,
        string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return $"Manual setting save blocked. {formItem.DisplayName} must be numeric.";
        }

        return ValidateNumericRange(formItem, parsed);
    }

    private static string ValidateBoolParameter(
        ST_MANUAL_FORM_ITEM formItem,
        string value)
    {
        var normalized = value.Trim().ToUpperInvariant();

        return normalized is "ON" or "OFF" or "TRUE" or "FALSE" or "1" or "0" or "YES" or "NO"
            ? ""
            : $"Manual setting save blocked. {formItem.DisplayName} must be ON/OFF or TRUE/FALSE.";
    }

    private static string ValidateNumericRange(
        ST_MANUAL_FORM_ITEM formItem,
        double value)
    {
        if (!formItem.Min.Equals(formItem.Max) &&
            (value < formItem.Min || value > formItem.Max))
        {
            return $"Manual setting save blocked. {formItem.DisplayName} must be between {formItem.Min:0.###} and {formItem.Max:0.###}.";
        }

        return "";
    }

    private double ReadDouble(
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<ST_MANUAL_FORM_ITEM> formItems,
        string key,
        double defaultValue)
    {
        var value = ReadString(values, formItems, key, defaultValue.ToString(CultureInfo.InvariantCulture));

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    private static double ReadDoubleValue(string value, double defaultValue)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    private string ReadString(
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<ST_MANUAL_FORM_ITEM> formItems,
        string key,
        string defaultValue)
    {
        return values.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value)
            ? value
            : formItems.FirstOrDefault(item => item.Name.Equals(key, StringComparison.OrdinalIgnoreCase))?.DefaultValue
                ?? defaultValue;
    }

    private string GetFormPath()
    {
        return Path.Combine(configRoot, "JHMI_MANUAL_SCAN.csv");
    }

    private string GetSettingPath(string settingName)
    {
        return Path.Combine(_manualDirectory, NormalizeSettingName(settingName));
    }

    private static string NormalizeSettingName(string settingName)
    {
        var normalized = Path.GetFileName(settingName.Trim());

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = DefaultSettingName;
        }

        if (!normalized.EndsWith(".scan", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"{normalized}.scan";
        }

        return normalized;
    }

    private static EN_RECIPE_DATA_TYPE ReadDataType(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "INT" => EN_RECIPE_DATA_TYPE.Int,
            "DOUBLE" => EN_RECIPE_DATA_TYPE.Double,
            "BOOL" => EN_RECIPE_DATA_TYPE.Bool,
            _ => EN_RECIPE_DATA_TYPE.String
        };
    }

    private static bool ReadBool(string value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
            || value.Equals("USE", StringComparison.OrdinalIgnoreCase)
            || value.Equals("ON", StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadInt(string value, int defaultValue)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    private static string GetOrDefault(string value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static IReadOnlyList<ST_MANUAL_FORM_ITEM> CreateFallbackFormItems()
    {
        return
        [
            new("ShapeSize", "Shape Size", EN_RECIPE_DATA_TYPE.Double, "mm", true, true, "0.350", 0.001, 100.000, "Shape scan size", 10),
            new("OffsetX", "Offset X", EN_RECIPE_DATA_TYPE.Double, "mm", true, true, "0.000", -500.000, 500.000, "Shape scan offset X", 20),
            new("OffsetY", "Offset Y", EN_RECIPE_DATA_TYPE.Double, "mm", true, true, "0.000", -500.000, 500.000, "Shape scan offset Y", 30),
            new("Direction", "Direction", EN_RECIPE_DATA_TYPE.String, "", true, true, "CW", 0.0, 0.0, "Scan direction", 40),
            new("ShapeName", "Shape Name", EN_RECIPE_DATA_TYPE.String, "", true, true, "Circle", 0.0, 0.0, "Shape type", 50)
        ];
    }

    private sealed record ST_MANUAL_FORM_ITEM(
        string Name,
        string DisplayName,
        EN_RECIPE_DATA_TYPE DataType,
        string Unit,
        bool Show,
        bool Use,
        string DefaultValue,
        double Min,
        double Max,
        string Description,
        int DisplayOrder);
}





