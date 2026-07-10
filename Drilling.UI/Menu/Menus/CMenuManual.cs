using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;
using System.IO;
using System.Windows;
using Drilling.UI.Popup;
using System.Windows.Media;

namespace Drilling.UI.Menu.Menus;

public sealed class CMenuManual : IMenu
{
    private readonly IManualScanFile _scanFile;
    private readonly Func<int> _selectedHeadNoProvider;
    private readonly Func<string> _selectedSettingNameProvider;
    private readonly Action<string> _selectedSettingNameSetter;
    private readonly Action<string> _setStatusMessage;
    private readonly Action _refreshShellStatus;
    private readonly Func<Task> _refreshCurrentScreen;

    public CMenuManual(
        IManualScanFile scanFile,
        Func<int> selectedHeadNoProvider,
        Func<string> selectedSettingNameProvider,
        Action<string> selectedSettingNameSetter,
        CButtonCommand selectHeadCommand,
        Action<string> setStatusMessage,
        Action refreshShellStatus,
        Func<Task> refreshCurrentScreen)
    {
        _scanFile = scanFile;
        _selectedHeadNoProvider = selectedHeadNoProvider;
        _selectedSettingNameProvider = selectedSettingNameProvider;
        _selectedSettingNameSetter = selectedSettingNameSetter;
        _setStatusMessage = setStatusMessage;
        _refreshShellStatus = refreshShellStatus;
        _refreshCurrentScreen = refreshCurrentScreen;

        SelectHeadCommand = selectHeadCommand;
        SelectSettingCommand = new CButtonCommand(async parameter => await SelectSetting(parameter));
        CreateCommand = new CButtonCommand(async _ => await Create());
        DeleteCommand = new CButtonCommand(async _ => await Delete());
        RenameCommand = new CButtonCommand(async _ => await Rename());
        SaveCommand = new CButtonCommand(async _ => await Save());
    }

    public EN_MENU Menu => EN_MENU.Manual;

    public IReadOnlyList<ST_DISPLAY_ITEM> ManualSettings { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> SelectedHeadItems { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> PositionMoveItems { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> ShapeScanItems { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> CommandStateItems { get; private set; } = [];

    public string SelectedHead { get; private set; } = "";

    public string LoadedSettingName { get; private set; } = "";

    public string LoadedSettingPath { get; private set; } = "";

    public IReadOnlyList<ST_MANUAL_HEAD_CARD> HeadCards { get; private set; } = [];

    public IReadOnlyList<ST_MANUAL_SETTING_FILE> SettingFiles { get; private set; } = [];

    public IReadOnlyList<ST_MANUAL_PARAMETER> SettingParameters { get; private set; } = [];

    public IReadOnlyList<ST_MANUAL_COMMAND_STATE> CommandStateRows { get; private set; } = [];

    public CButtonCommand SelectHeadCommand { get; }

    public CButtonCommand SelectSettingCommand { get; }

    public CButtonCommand CreateCommand { get; }

    public CButtonCommand DeleteCommand { get; }

    public CButtonCommand RenameCommand { get; }

    public CButtonCommand SaveCommand { get; }

    public async Task<CScreenViewModel> Build(CancellationToken cancellationToken = default)
    {
        var settingNames = await _scanFile.List(cancellationToken);
        var selectedSettingName = ResolveSelectedSettingName(settingNames, _selectedSettingNameProvider());
        var settings = await _scanFile.Load(selectedSettingName, cancellationToken);
        var selectedHeadNo = Math.Clamp(_selectedHeadNoProvider(), 1, 12);
        var headCards = BuildHeadCards(selectedHeadNo);
        var selectedHead = headCards.First(head => head.IsSelected);
        var selectedHeadItems = new ST_DISPLAY_ITEM[]
        {
            new("Head", selectedHead.HeadName),
            new("GX Position", selectedHead.Gx, "mm"),
            new("GY Position", selectedHead.Gy, "mm"),
            new("Servo", "ON"),
            new("Motion", selectedHead.State)
        };

        Apply(
            [
                new("Selected Head", selectedHead.HeadName),
                new("Shape Size", settings.ShapeSize.ToString("F3"), "mm"),
                new("Offset X", settings.OffsetX.ToString("F3"), "mm"),
                new("Offset Y", settings.OffsetY.ToString("F3"), "mm"),
                new("Direction", settings.Direction),
                new("Shape", settings.ShapeName)
            ],
            selectedHeadItems,
            [
                new("Center Move", "Ready"),
                new("Position Move", "GX/GY Target"),
                new("Move Stop", "Ready"),
                new("GX Target", selectedHead.Gx, "mm"),
                new("GY Target", selectedHead.Gy, "mm")
            ],
            [
                new("Shape Size", settings.ShapeSize.ToString("F3"), "mm"),
                new("Offset X", settings.OffsetX.ToString("F3"), "mm"),
                new("Offset Y", settings.OffsetY.ToString("F3"), "mm"),
                new("Direction", settings.Direction),
                new("Shape", settings.ShapeName),
                new("Start", "Ready"),
                new("Stop", "Ready")
            ],
            [
                new("Laser", "OFF"),
                new("CENTER", "OFF"),
                new("Last Command", "-"),
                new("Result", "Ready")
            ],
            selectedHead.HeadName,
            selectedSettingName,
            $@"Config\Manual\{selectedSettingName}",
            headCards,
            BuildSettingFiles(settingNames, selectedSettingName),
            BuildSettingParameters(settings),
            BuildCommandStateRows(selectedHead, settings));

        return new CScreenViewModel(
            EN_MENU.Manual,
            "MANUAL / SCANNER CONTROL",
            "Single head scanner move, shape scan, manual parameter save/load.",
            [
                new("Selected Head", selectedHead.HeadName),
                new("Mode", "Manual"),
                new("Laser", "OFF")
            ],
            [
                new("Manual Setting", [
                    new("Shape Size", settings.ShapeSize.ToString("F3"), "mm"),
                    new("Offset X", settings.OffsetX.ToString("F3"), "mm"),
                    new("Offset Y", settings.OffsetY.ToString("F3"), "mm"),
                    new("Direction", settings.Direction),
                    new("Shape", settings.ShapeName)
                ]),
                new("Position Move", [
                    new("Center Move", "Ready"),
                    new("Position Move", "GX/GY target input"),
                    new("Move Stop", "Ready")
                ])
            ],
            manual: this);
    }

    private async Task SelectSetting(object? parameter)
    {
        var settingName = GetManualSettingNameFromParameter(parameter);

        if (string.IsNullOrWhiteSpace(settingName))
        {
            return;
        }

        _selectedSettingNameSetter(settingName);
        _setStatusMessage($"Manual setting {settingName} selected.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task Create()
    {
        var settingNames = await _scanFile.List();
        var newSettingName = ShowManualSettingNameDialog(
            "Create Manual Setting",
            "Enter the new manual scan setting name.",
            "",
            value => ValidateManualSettingName(NormalizeManualSettingNameInput(value), settingNames));

        if (newSettingName is null)
        {
            _setStatusMessage("Manual setting create canceled.");
            return;
        }

        var validationMessage = ValidateManualSettingName(newSettingName, settingNames);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            _setStatusMessage(validationMessage);
            return;
        }

        await _scanFile.Save(newSettingName, CreateManualScanParamFromScreen(this));

        _selectedSettingNameSetter(newSettingName);
        _setStatusMessage($"Manual setting {newSettingName} created and CSV verified.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(LoadedSettingName))
        {
            _setStatusMessage("Manual setting save skipped. No manual setting is selected.");
            return;
        }

        try
        {
            await _scanFile.Save(
                LoadedSettingName,
                CreateManualScanParamFromScreen(this));
        }
        catch (InvalidDataException exception)
        {
            _setStatusMessage(exception.Message);
            return;
        }
        catch (IOException exception)
        {
            _setStatusMessage($"Manual setting save blocked. {exception.Message}");
            return;
        }

        _selectedSettingNameSetter(LoadedSettingName);
        _setStatusMessage($"Manual setting {LoadedSettingName} saved and CSV verified.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task Rename()
    {
        var oldSettingName = GetManualSettingNameFromParameter(LoadedSettingName);

        if (string.IsNullOrWhiteSpace(oldSettingName))
        {
            _setStatusMessage("Manual setting rename skipped. No manual setting is selected.");
            return;
        }

        var settingNames = await _scanFile.List();
        var newSettingName = ShowManualSettingNameDialog(
            "Rename Manual Setting",
            "Enter the new manual scan setting name.",
            Path.GetFileNameWithoutExtension(oldSettingName),
            value => ValidateManualSettingName(NormalizeManualSettingNameInput(value), settingNames, oldSettingName));

        if (newSettingName is null)
        {
            _setStatusMessage("Manual setting rename canceled.");
            return;
        }

        if (newSettingName.Equals(oldSettingName, StringComparison.OrdinalIgnoreCase))
        {
            _setStatusMessage("Manual setting rename skipped. Name was not changed.");
            return;
        }

        var validationMessage = ValidateManualSettingName(newSettingName, settingNames, oldSettingName);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            _setStatusMessage(validationMessage);
            return;
        }

        await _scanFile.Save(oldSettingName, CreateManualScanParamFromScreen(this));
        await _scanFile.Rename(oldSettingName, newSettingName);

        _selectedSettingNameSetter(newSettingName);
        _setStatusMessage($"Manual setting {oldSettingName} renamed to {newSettingName}.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task Delete()
    {
        var settingName = GetManualSettingNameFromParameter(LoadedSettingName);

        if (string.IsNullOrWhiteSpace(settingName))
        {
            _setStatusMessage("Manual setting delete skipped. No manual setting is selected.");
            return;
        }

        if (!ConfirmManualSettingDelete(settingName))
        {
            _setStatusMessage($"Manual setting {settingName} delete canceled.");
            return;
        }

        await _scanFile.Delete(settingName);

        var remainingSettings = await _scanFile.List();
        _selectedSettingNameSetter(remainingSettings.FirstOrDefault() ?? "CIRCLE_TEST.scan");
        _setStatusMessage($"Manual setting {settingName} deleted.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private void Apply(
        IReadOnlyList<ST_DISPLAY_ITEM> manualSettings,
        IReadOnlyList<ST_DISPLAY_ITEM> selectedHeadItems,
        IReadOnlyList<ST_DISPLAY_ITEM> positionMoveItems,
        IReadOnlyList<ST_DISPLAY_ITEM> shapeScanItems,
        IReadOnlyList<ST_DISPLAY_ITEM> commandStateItems,
        string selectedHead,
        string loadedSettingName,
        string loadedSettingPath,
        IReadOnlyList<ST_MANUAL_HEAD_CARD> headCards,
        IReadOnlyList<ST_MANUAL_SETTING_FILE> settingFiles,
        IReadOnlyList<ST_MANUAL_PARAMETER> settingParameters,
        IReadOnlyList<ST_MANUAL_COMMAND_STATE> commandStateRows)
    {
        ManualSettings = manualSettings;
        SelectedHeadItems = selectedHeadItems;
        PositionMoveItems = positionMoveItems;
        ShapeScanItems = shapeScanItems;
        CommandStateItems = commandStateItems;
        SelectedHead = selectedHead;
        LoadedSettingName = loadedSettingName;
        LoadedSettingPath = loadedSettingPath;
        HeadCards = headCards;
        SettingFiles = settingFiles;
        SettingParameters = settingParameters;
        CommandStateRows = commandStateRows;
    }

    private static IReadOnlyList<ST_MANUAL_HEAD_CARD> BuildHeadCards(int selectedHeadNo)
    {
        return
        [
            Head(1, "-12.345", "-23.450", "Ready", selectedHeadNo),
            Head(2, "15.230", "-10.125", "Ready", selectedHeadNo),
            Head(3, "-5.678", "-50.880", "Idle", selectedHeadNo),
            Head(4, "12.340", "8.960", "Ready", selectedHeadNo),
            Head(5, "-25.100", "30.250", "Idle", selectedHeadNo),
            Head(6, "-22.010", "11.250", "Ready", selectedHeadNo),
            Head(7, "18.750", "-18.430", "Ready", selectedHeadNo),
            Head(8, "-15.630", "15.400", "Idle", selectedHeadNo),
            Head(9, "-15.620", "5.400", "Ready", selectedHeadNo),
            Head(10, "-30.220", "41.210", "Idle", selectedHeadNo),
            Head(11, "3.120", "-32.770", "Ready", selectedHeadNo),
            Head(12, "0.000", "0.000", "Idle", selectedHeadNo)
        ];
    }

    private static ST_MANUAL_HEAD_CARD Head(
        int headNo,
        string gx,
        string gy,
        string state,
        int selectedHeadNo)
    {
        return new ST_MANUAL_HEAD_CARD(
            headNo,
            $"H{headNo:00}",
            gx,
            gy,
            state,
            headNo == selectedHeadNo);
    }

    private static string ResolveSelectedSettingName(
        IReadOnlyList<string> settingNames,
        string selectedSettingName)
    {
        if (settingNames.Count == 0)
        {
            return "CIRCLE_TEST.scan";
        }

        var normalizedSelectedName = NormalizeSettingName(selectedSettingName);

        return settingNames.FirstOrDefault(name =>
                name.Equals(normalizedSelectedName, StringComparison.OrdinalIgnoreCase))
            ?? settingNames.FirstOrDefault(name =>
                name.Equals("CIRCLE_TEST.scan", StringComparison.OrdinalIgnoreCase))
            ?? settingNames[0];
    }

    private static IReadOnlyList<ST_MANUAL_SETTING_FILE> BuildSettingFiles(
        IReadOnlyList<string> settingNames,
        string selectedSettingName)
    {
        return settingNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new ST_MANUAL_SETTING_FILE(
                name,
                name.Equals(selectedSettingName, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static IReadOnlyList<ST_MANUAL_PARAMETER> BuildSettingParameters(ST_MANUAL_SCAN_PARAM settings)
    {
        return
        [
            new("Laser Power", "1.00", "W"),
            new("Jump Speed", "1500", "mm/sec"),
            new("Mark Speed", "900", "mm/sec"),
            new("Attenuator Position", "23.50", "%"),
            new("Laser Frequency", "20.0", "kHz"),
            new("Laser On Delay", "8", "usec"),
            new("Laser Off Delay", "12", "usec"),
            new("Time", "10", "ms"),
            new("Count", "48000", "count"),
            new("Shape Size", settings.ShapeSize.ToString("F3"), "mm"),
            new("Shape Count", "1", "count"),
            new("Offset X", settings.OffsetX.ToString("F3"), "mm"),
            new("Offset Y", settings.OffsetY.ToString("F3"), "mm"),
            new("Direction", settings.Direction, "-")
        ];
    }

    private static string NormalizeSettingName(string settingName)
    {
        var normalized = Path.GetFileName(settingName.Trim());

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "CIRCLE_TEST.scan";
        }

        return normalized.EndsWith(".scan", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{normalized}.scan";
    }

    private static IReadOnlyList<ST_MANUAL_COMMAND_STATE> BuildCommandStateRows(
        ST_MANUAL_HEAD_CARD selectedHead,
        ST_MANUAL_SCAN_PARAM settings)
    {
        return
        [
            new("Selected Head", selectedHead.HeadName),
            new("Mode", "TIME"),
            new("Laser Power", "1.00", "W"),
            new("Frequency", "20.0", "kHz"),
            new("Time", "10", "ms"),
            new("Count", "48000", "count"),
            new("Attenuator Position", "23.50", "%"),
            new("Command State", selectedHead.State)
        ];
    }

    private static ST_MANUAL_SCAN_PARAM CreateManualScanParamFromScreen(CMenuManual manualScreen)
    {
        return new ST_MANUAL_SCAN_PARAM(
            ReadManualDouble(manualScreen, "Shape Size", 0.350),
            ReadManualDouble(manualScreen, "Offset X", 0.000),
            ReadManualDouble(manualScreen, "Offset Y", 0.000),
            ReadManualValue(manualScreen, "Direction", "CW"),
            ReadManualValue(manualScreen, "Shape Name", ReadManualValue(manualScreen, "Shape", "Circle")));
    }

    private static double ReadManualDouble(
        CMenuManual manualScreen,
        string parameterName,
        double defaultValue)
    {
        return double.TryParse(
            ReadManualValue(manualScreen, parameterName, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value)
            ? value
            : defaultValue;
    }

    private static string ReadManualValue(
        CMenuManual manualScreen,
        string parameterName,
        string defaultValue)
    {
        var row = manualScreen.SettingParameters.FirstOrDefault(parameter =>
            parameter.Parameter.Equals(parameterName, StringComparison.OrdinalIgnoreCase));

        return row is null || string.IsNullOrWhiteSpace(row.Value)
            ? defaultValue
            : row.Value.Trim();
    }

    private static string GetManualSettingNameFromParameter(object? parameter)
    {
        var value = parameter switch
        {
            ST_MANUAL_SETTING_FILE settingFile => settingFile.Name,
            string text => text,
            _ => ""
        };

        return NormalizeManualSettingNameInput(value);
    }

    private static string? ShowManualSettingNameDialog(
        string title,
        string message,
        string initialValue,
        Func<string, string>? validate = null)
    {
        var dialog = new CRecipeNameDialog(title, message, initialValue, validate)
        {
            Owner = GetActiveWindow()
        };

        return dialog.ShowDialog() == true
            ? NormalizeManualSettingNameInput(dialog.RecipeName)
            : null;
    }

    private static bool ConfirmManualSettingDelete(string settingName)
    {
        var dialog = new CRecipeConfirmDialog(
            "Delete Manual Setting",
            $"Delete {settingName}?\nThis operation removes the manual scan setting file from the Manual folder.",
            "DELETE")
        {
            Owner = GetActiveWindow()
        };

        return dialog.ShowDialog() == true;
    }

    private static Window? GetActiveWindow()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive);
    }

    private static string NormalizeManualSettingNameInput(string value)
    {
        var normalized = value.Trim();

        if (normalized.EndsWith(".scan", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^5];
        }

        normalized = normalized.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? ""
            : $"{normalized}.scan";
    }

    private static string ValidateManualSettingName(
        string settingName,
        IReadOnlyList<string> settingNames,
        string currentSettingName = "")
    {
        if (string.IsNullOrWhiteSpace(settingName))
        {
            return "Manual setting name is required.";
        }

        var settingId = Path.GetFileNameWithoutExtension(settingName.Trim());

        foreach (var character in settingId)
        {
            if (Path.GetInvalidFileNameChars().Contains(character))
            {
                return $"Manual setting name cannot contain '{character}'.";
            }
        }

        if (settingId is "." or ".." || settingId.EndsWith(".", StringComparison.Ordinal))
        {
            return "Manual setting name is not valid as a file name.";
        }

        var reservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        if (reservedNames.Contains(settingId))
        {
            return "Manual setting name is reserved by Windows.";
        }

        var exists = settingNames.Any(name =>
            name.Equals(settingName, StringComparison.OrdinalIgnoreCase) &&
            !name.Equals(currentSettingName, StringComparison.OrdinalIgnoreCase));

        return exists
            ? $"Manual setting {settingName} already exists."
            : "";
    }
}

public sealed record ST_MANUAL_HEAD_CARD(
    int HeadNo,
    string HeadName,
    string Gx,
    string Gy,
    string State,
    bool IsSelected)
{
    public Brush StateBrush => CStatusBrush.ForHeadStatus(State);
}

public sealed record ST_MANUAL_SETTING_FILE(
    string Name,
    bool IsSelected);

public sealed class ST_MANUAL_PARAMETER : CBindingBase
{
    private string _value;

    public ST_MANUAL_PARAMETER(
        string parameter,
        string value,
        string unit)
    {
        Parameter = parameter;
        _value = value;
        Unit = unit;
    }

    public string Parameter { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public string Unit { get; }
}

public sealed record ST_MANUAL_COMMAND_STATE(
    string Name,
    string Value,
    string Unit = "");




