using System.Windows.Media;
using Drilling.UI.Menu;

namespace Drilling.UI.Menu.Menus;

public sealed class CMenuCorrection : IMenu
{
    private static readonly string[] CorrectionTabs =
    [
        "REVIEW DATA",
        "ALIGN COMP",
        "OFFSET COMP",
        "APC / ICR",
        "ZERO DEFENSE",
        "OUTPUT / HISTORY"
    ];

    private readonly Action<string> _statusReporter;
    private readonly Func<Task> _refreshCurrentScreen;
    private string _selectedTab = "REVIEW DATA";

    public CMenuCorrection(
        Action<string> statusReporter,
        Func<Task> refreshCurrentScreen)
    {
        _statusReporter = statusReporter;
        _refreshCurrentScreen = refreshCurrentScreen;

        SelectTabCommand = new CButtonCommand(SelectTab);
        ExecuteCommand = new CButtonCommand(Execute);
    }

    public EN_MENU Menu => EN_MENU.Correction;

    public string Title => $"CORRECTION / {_selectedTab}";

    public string Subtitle => GetSubtitle(_selectedTab);

    public string SelectedTab => _selectedTab;

    public IReadOnlyList<ST_CORRECTION_TAB> Tabs { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> SummaryItems { get; private set; } = [];

    public IReadOnlyList<ST_CORRECTION_SOURCE_ROW> SourceRows { get; private set; } = [];

    public IReadOnlyList<ST_CORRECTION_VALUE_ROW> CandidateRows { get; private set; } = [];

    public IReadOnlyList<ST_CORRECTION_VALUE_ROW> ApplyRows { get; private set; } = [];

    public IReadOnlyList<ST_CORRECTION_HISTORY_ROW> HistoryRows { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> DetailItems { get; private set; } = [];

    public CButtonCommand SelectTabCommand { get; }

    public CButtonCommand ExecuteCommand { get; }

    public Task<CScreenViewModel> Build(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplyTabData(_selectedTab);

        var screen = new CScreenViewModel(
            EN_MENU.Correction,
            Title,
            Subtitle,
            [
                new("Selected", _selectedTab),
                new("Source", GetSourceName(_selectedTab)),
                new("State", GetStateName(_selectedTab))
            ],
            [
                new("Correction Source", []),
                new("Correction Candidate", []),
                new("Apply Preview", [])
            ],
            correction: this);

        return Task.FromResult(screen);
    }

    private void SelectTab(object? parameter)
    {
        var tabName = parameter?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(tabName))
        {
            return;
        }

        var normalizedTab = CorrectionTabs.FirstOrDefault(tab =>
            tab.Equals(tabName, StringComparison.OrdinalIgnoreCase));

        if (normalizedTab is null)
        {
            return;
        }

        _selectedTab = normalizedTab;
        _statusReporter($"Correction tab selected: {_selectedTab}.");
        _ = _refreshCurrentScreen();
    }

    private void Execute(object? parameter)
    {
        var command = parameter?.ToString()?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        _statusReporter($"Correction {command} requested. Tab={_selectedTab}.");
    }

    private void ApplyTabData(string selectedTab)
    {
        Tabs = CorrectionTabs
            .Select(tab => new ST_CORRECTION_TAB(tab, tab.Equals(selectedTab, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        (SummaryItems, SourceRows, CandidateRows, ApplyRows, DetailItems, HistoryRows) = selectedTab switch
        {
            "ALIGN COMP" => CreateAlignCompData(),
            "OFFSET COMP" => CreateOffsetCompData(),
            "APC / ICR" => CreateApcIcrData(),
            "ZERO DEFENSE" => CreateZeroDefenseData(),
            "OUTPUT / HISTORY" => CreateOutputHistoryData(),
            _ => CreateReviewData()
        };
    }

    private static (
        IReadOnlyList<ST_DISPLAY_ITEM> Summary,
        IReadOnlyList<ST_CORRECTION_SOURCE_ROW> Source,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Candidate,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Apply,
        IReadOnlyList<ST_DISPLAY_ITEM> Detail,
        IReadOnlyList<ST_CORRECTION_HISTORY_ROW> History) CreateReviewData()
    {
        return (
            [
                new("Review Result", "ReviewResult_DRILL_A01_115248.csv"),
                new("Point Count", "80"),
                new("NG Count", "3", "WARN"),
                new("Tolerance", "X/Y +/-0.030 mm")
            ],
            [
                new("Review", "ALL POINT", "80", "Loaded", "2026-07-14 11:52:48"),
                new("Review", "Sample Rule", "ALL_POINT.csv", "Ready", "Recipe"),
                new("Recipe", "DRILL_A01", "8 Head / 10 Cell", "Ready", "Config\\RECIPE")
            ],
            [
                new("H01-CELL01", "Error X", "+0.011", "mm", "OK"),
                new("H03-CELL04", "Error Y", "-0.034", "mm", "NG"),
                new("H06-CELL08", "Error X", "+0.041", "mm", "NG"),
                new("H08-CELL10", "Error Y", "-0.009", "mm", "OK")
            ],
            [
                new("Average Offset X", "Recipe", "+0.006", "mm", "Ready"),
                new("Average Offset Y", "Recipe", "-0.004", "mm", "Ready"),
                new("Exclude NG", "Review Data", "3", "point", "Pending")
            ],
            [
                new("Target", "Review result based offset"),
                new("Apply To", "Recipe / Correction Table"),
                new("Rule", "OK point average, NG point review")
            ],
            CreateCommonHistory());
    }

    private static (
        IReadOnlyList<ST_DISPLAY_ITEM> Summary,
        IReadOnlyList<ST_CORRECTION_SOURCE_ROW> Source,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Candidate,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Apply,
        IReadOnlyList<ST_DISPLAY_ITEM> Detail,
        IReadOnlyList<ST_CORRECTION_HISTORY_ROW> History) CreateAlignCompData()
    {
        return (
            [
                new("Align Result", "Ready"),
                new("Distortion Key", "6"),
                new("Theta", "+0.002 deg"),
                new("Weight", "X 0.80 / Y 0.80")
            ],
            [
                new("Align", "Front Key", "X +0.012 / Y -0.006", "OK", "Vision PC"),
                new("Align", "Rear Key", "X +0.009 / Y -0.004", "OK", "Vision PC"),
                new("Distortion", "KEY 01-06", "6 point", "Ready", "Vision Result")
            ],
            [
                new("Align X", "Weighted", "+0.010", "mm", "Ready"),
                new("Align Y", "Weighted", "-0.005", "mm", "Ready"),
                new("Align Theta", "Rotation", "+0.002", "deg", "Ready"),
                new("Distortion", "Max DA", "42.0", "um", "OK")
            ],
            [
                new("X Start Weight", "Recipe", "0.80", "-", "Ready"),
                new("X End Weight", "Recipe", "0.80", "-", "Ready"),
                new("Y Start Weight", "Recipe", "0.80", "-", "Ready"),
                new("Y End Weight", "Recipe", "0.80", "-", "Ready")
            ],
            [
                new("Target", "Align result compensation"),
                new("Apply To", "Process coordinate transform"),
                new("Distortion Key", "6 key display / result storage")
            ],
            CreateCommonHistory());
    }

    private static (
        IReadOnlyList<ST_DISPLAY_ITEM> Summary,
        IReadOnlyList<ST_CORRECTION_SOURCE_ROW> Source,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Candidate,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Apply,
        IReadOnlyList<ST_DISPLAY_ITEM> Detail,
        IReadOnlyList<ST_CORRECTION_HISTORY_ROW> History) CreateOffsetCompData()
    {
        return (
            [
                new("Mode", "Recipe Offset"),
                new("Head Offset", "8"),
                new("Cell Shift", "50"),
                new("Default Offset", "Ready")
            ],
            [
                new("Recipe", "Head Offset", "H01-H08", "Loaded", "JHMI_RCP"),
                new("Recipe", "Cell Shift", "CELL01-CELL50", "Loaded", "JHMI_RCP"),
                new("Setup", "Scanner Default Offset", "8 scanner", "Ready", "Default Offset")
            ],
            [
                new("H01_OFFSET_X", "Head", "+0.003", "mm", "Ready"),
                new("H07_OFFSET_Y", "Head", "+0.002", "mm", "Ready"),
                new("CELL12_ALIGN_X", "Cell", "-0.010", "mm", "Ready"),
                new("SCANNER_04_DEFAULT_X", "Default", "+0.006", "mm", "Ready")
            ],
            [
                new("Simple Offset", "Key-in", "Recipe value edit", "-", "Ready"),
                new("Rough Offset", "Cell 1 Point", "Cell batch apply", "-", "Pending"),
                new("Default Offset", "Scanner interval", "Head default apply", "-", "Ready")
            ],
            [
                new("Target", "Recipe offset parameter"),
                new("Simple Offset", "Direct key-in value"),
                new("Excluded", "Scanner Field Correction / Scan Comp")
            ],
            CreateCommonHistory());
    }

    private static (
        IReadOnlyList<ST_DISPLAY_ITEM> Summary,
        IReadOnlyList<ST_CORRECTION_SOURCE_ROW> Source,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Candidate,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Apply,
        IReadOnlyList<ST_DISPLAY_ITEM> Detail,
        IReadOnlyList<ST_CORRECTION_HISTORY_ROW> History) CreateApcIcrData()
    {
        return (
            [
                new("APC", "OFF", "WAIT"),
                new("ICR", "OFF", "WAIT"),
                new("Precision", "+/-13 um"),
                new("Source", "CIM Share")
            ],
            [
                new("APC", "APC File", "Not Loaded", "WAIT", "CIM PC Share"),
                new("ICR", "ICR File", "Not Loaded", "WAIT", "CIM PC Share"),
                new("Setting", "OFFSET_PRECISION", "+/-13", "Ready", "EC_LD")
            ],
            [
                new("APC_USE", "External", "0", "-", "WAIT"),
                new("ICR_USE", "External", "0", "-", "WAIT"),
                new("OFFSET_PRECISION", "Limit", "13", "um", "Ready")
            ],
            [
                new("APC Apply", "Correction Table", "Standby", "-", "WAIT"),
                new("ICR Apply", "Correction Table", "Standby", "-", "WAIT"),
                new("Source Backup", "History", "Required", "-", "Ready")
            ],
            [
                new("Target", "External precision compensation"),
                new("Input", "CIM/shared folder file"),
                new("Apply To", "Correction table / process model")
            ],
            CreateCommonHistory());
    }

    private static (
        IReadOnlyList<ST_DISPLAY_ITEM> Summary,
        IReadOnlyList<ST_CORRECTION_SOURCE_ROW> Source,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Candidate,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Apply,
        IReadOnlyList<ST_DISPLAY_ITEM> Detail,
        IReadOnlyList<ST_CORRECTION_HISTORY_ROW> History) CreateZeroDefenseData()
    {
        return (
            [
                new("0-Line Point", "5"),
                new("Mode", "Review"),
                new("Judge", "Ready"),
                new("Apply", "Standby")
            ],
            [
                new("Recipe", "ZERO_DEFENCE_REVIEW_POINT", "5", "Loaded", "JHMI_RCP"),
                new("Review", "0-Line Result", "5 point", "Ready", "Review Result"),
                new("Stage", "Line Move", "Stage PC", "Ready", "Melsec / Stage PC")
            ],
            [
                new("Line Error X", "Average", "+0.008", "mm", "OK"),
                new("Line Error Y", "Average", "-0.006", "mm", "OK"),
                new("Max Deviation", "0-Line", "0.018", "mm", "OK")
            ],
            [
                new("0-Line Offset X", "Recipe", "+0.008", "mm", "Ready"),
                new("0-Line Offset Y", "Recipe", "-0.006", "mm", "Ready"),
                new("Defense Result", "Interlock", "OK", "-", "Ready")
            ],
            [
                new("Target", "0-line defense review"),
                new("Review Point", "Recipe ZERO_DEFENCE_REVIEW_POINT"),
                new("Apply To", "Recipe offset / process model")
            ],
            CreateCommonHistory());
    }

    private static (
        IReadOnlyList<ST_DISPLAY_ITEM> Summary,
        IReadOnlyList<ST_CORRECTION_SOURCE_ROW> Source,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Candidate,
        IReadOnlyList<ST_CORRECTION_VALUE_ROW> Apply,
        IReadOnlyList<ST_DISPLAY_ITEM> Detail,
        IReadOnlyList<ST_CORRECTION_HISTORY_ROW> History) CreateOutputHistoryData()
    {
        return (
            [
                new("Output", "Correction Table"),
                new("Recipe", "DRILL_A01"),
                new("Last Apply", "10:24:36"),
                new("State", "Modified", "WARN")
            ],
            [
                new("Output", "Recipe", "DRILL_A01.csv", "Modified", "Config\\RECIPE"),
                new("Output", "Correction Table", "Current", "Ready", "Config\\Correction"),
                new("Log", "Correction History", "Today", "Ready", "Data\\Log")
            ],
            [
                new("Review Offset", "Recipe", "+0.006 / -0.004", "mm", "Ready"),
                new("Align Offset", "Transform", "+0.010 / -0.005", "mm", "Ready"),
                new("Zero Defense", "Recipe", "+0.008 / -0.006", "mm", "Ready")
            ],
            [
                new("Recipe Save", "DRILL_A01.csv", "Required", "-", "Pending"),
                new("Script Build", "Process Model", "Correction applied", "-", "Ready"),
                new("History Write", "Correction Log", "Required", "-", "Ready")
            ],
            [
                new("Target", "Final correction output"),
                new("Apply Order", "Review / Align / Offset / APC-ICR / Zero"),
                new("History", "Before / After / User / Time")
            ],
            [
                new("10:24:36", "ENG1", "ZERO DEFENSE", "Line Offset", "0.000 / 0.000", "+0.008 / -0.006", "Pending"),
                new("10:18:11", "ENG1", "OFFSET COMP", "H01_OFFSET_X", "0.000", "+0.003", "Saved"),
                new("10:12:40", "ENG1", "ALIGN COMP", "Theta", "0.000", "+0.002", "Saved"),
                new("10:04:29", "ENG1", "REVIEW DATA", "Review Result", "-", "Loaded", "OK")
            ]);
    }

    private static IReadOnlyList<ST_CORRECTION_HISTORY_ROW> CreateCommonHistory()
    {
        return
        [
            new("10:24:36", "ENG1", "REVIEW DATA", "Review Result", "-", "Loaded", "OK"),
            new("10:18:11", "ENG1", "OFFSET COMP", "H01_OFFSET_X", "0.000", "+0.003", "Saved"),
            new("10:12:40", "ENG1", "ALIGN COMP", "Theta", "0.000", "+0.002", "Saved"),
            new("10:05:09", "ENG1", "APC / ICR", "APC File", "-", "Standby", "WAIT")
        ];
    }

    private static string GetSubtitle(string selectedTab)
    {
        return selectedTab switch
        {
            "REVIEW DATA" => "Review result load, measurement error review and compensation point selection",
            "ALIGN COMP" => "Align X/Y/Theta and distortion key compensation",
            "OFFSET COMP" => "Recipe offset, head offset, cell shift and scanner default offset",
            "APC / ICR" => "External APC / ICR precision correction file apply",
            "ZERO DEFENSE" => "0-line review point check and defense offset apply",
            "OUTPUT / HISTORY" => "Final correction output preview and apply history",
            _ => "Correction operation"
        };
    }

    private static string GetSourceName(string selectedTab)
    {
        return selectedTab switch
        {
            "APC / ICR" => "External",
            "ALIGN COMP" => "Vision",
            "OFFSET COMP" => "Recipe",
            "ZERO DEFENSE" => "Review",
            "OUTPUT / HISTORY" => "Mixed",
            _ => "Review"
        };
    }

    private static string GetStateName(string selectedTab)
    {
        return selectedTab is "APC / ICR"
            ? "WAIT"
            : "Ready";
    }
}

public sealed record ST_CORRECTION_TAB(
    string Name,
    bool IsSelected);

public sealed record ST_CORRECTION_SOURCE_ROW(
    string Type,
    string Name,
    string Value,
    string State,
    string Source)
{
    public Brush StateBrush => CStatusBrush.ForDisplayState(State);
}

public sealed record ST_CORRECTION_VALUE_ROW(
    string Item,
    string Target,
    string Value,
    string Unit,
    string State)
{
    public Brush ValueBrush => CStatusBrush.ForDisplayState(State);

    public Brush StateBrush => CStatusBrush.ForDisplayState(State);
}

public sealed record ST_CORRECTION_HISTORY_ROW(
    string Time,
    string User,
    string Tab,
    string Item,
    string Before,
    string After,
    string Result)
{
    public Brush ResultBrush => CStatusBrush.ForDisplayState(Result);
}
