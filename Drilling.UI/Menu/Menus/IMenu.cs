using Drilling.Common.Alarm;
using Drilling.Common.Interface;
using Drilling.Common.InterLock;
using Drilling.Common.Managers;
using Drilling.Common.Motion;
using Drilling.Common.Station;
using Drilling.UI.Menu;
using System.Windows.Media;

namespace Drilling.UI.Menu.Menus;

public enum EN_MENU
{
    Main,
    Manual,
    Recipe,
    Setting,
    Alarm,
    Monitor,
    Pm,
    Exit
}

public interface IMenu
{
    EN_MENU Menu { get; }

    Task<CScreenViewModel> Build(CancellationToken cancellationToken = default);
}

public sealed record ST_DISPLAY_ITEM(
    string Name,
    string Value,
    string Detail = "")
{
    public Brush ValueBrush => CStatusBrush.ForDisplayState(string.IsNullOrWhiteSpace(Detail) ? Value : Detail);

    public Brush StateBrush => CStatusBrush.ForDisplayState(Value);
}

internal static class CStatusBrush
{
    public static readonly Brush PrimaryText = Frozen(0xF3, 0xF6, 0xFA);
    public static readonly Brush Recipe = Frozen(0xFF, 0x2F, 0x6D);
    public static readonly Brush Online = Frozen(0x12, 0xD4, 0x66);
    public static readonly Brush Simul = Frozen(0xFF, 0xC4, 0x00);
    public static readonly Brush Offline = Frozen(0xFF, 0x3B, 0x5F);
    public static readonly Brush Wait = Frozen(0xFF, 0xC4, 0x00);
    public static readonly Brush Active = Frozen(0x2F, 0x81, 0xF7);
    public static readonly Brush Muted = Frozen(0x6F, 0x7B, 0x8A);
    public static readonly Brush CommandBlue = Frozen(0x07, 0x59, 0xC8);
    public static readonly Brush CommandBlueBorder = Frozen(0x2F, 0x81, 0xF7);
    public static readonly Brush CommandGreen = Frozen(0x0A, 0x8F, 0x4A);
    public static readonly Brush CommandGreenBorder = Frozen(0x12, 0xD4, 0x66);
    public static readonly Brush CommandRed = Frozen(0xC8, 0x14, 0x3B);
    public static readonly Brush CommandRedBorder = Frozen(0xFF, 0x2F, 0x6D);
    public static readonly Brush CommandDark = Frozen(0x10, 0x19, 0x23);
    public static readonly Brush CommandDarkBorder = Frozen(0x34, 0x42, 0x51);

    public static Brush ForHeaderState(string state)
    {
        return Normalize(state) switch
        {
            "RECIPE" => Recipe,
            "ONLINE" or "CLEAR" or "READY" or "OK" or "RUN" or "SAFE" or "DONE" => Online,
            "SIM" or "SIMUL" or "SIMULATION" => Simul,
            "OFFLINE" or "OCCUR" or "ERROR" or "NG" or "FAILED" or "FAIL" or "ALARM" or "STOP" or "STOPPED" => Offline,
            "WAIT" or "WAITING" or "WARN" or "WARNING" or "PENDING" => Wait,
            "ACTIVE" or "RUNNING" => Active,
            _ => Muted
        };
    }

    public static Brush ForDisplayState(string state)
    {
        return Normalize(state) switch
        {
            "OK" or "DONE" or "COMPLETE" or "COMPLETED" or "CLEAR" or "READY" => Online,
            "RUN" or "RUNNING" or "ACTIVE" => Active,
            "WAIT" or "WAITING" or "PENDING" or "WARN" or "WARNING" => Wait,
            "ERROR" or "NG" or "FAILED" or "FAIL" or "ALARM" or "STOP" or "STOPPED" => Offline,
            "SIM" or "SIMUL" or "SIMULATION" => Simul,
            _ => Muted
        };
    }

    public static Brush ForHeadStatus(string status)
    {
        return Normalize(status) switch
        {
            "RUN" or "RUNNING" or "ACTIVE" => Active,
            "WAIT" or "WAITING" or "IDLE" => Wait,
            "SKIP" or "SKIPPED" => Muted,
            "ERROR" or "ALARM" => Offline,
            _ => Online
        };
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    public static SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}

public sealed record ST_HEADER_STATUS_ITEM(
    string Name,
    string Value,
    string State,
    bool CanNavigate = false,
    string PageText = "",
    CButtonCommand? PreviousCommand = null,
    CButtonCommand? NextCommand = null,
    CButtonCommand? OpenCommand = null)
{
    public Brush AccentBrush => CStatusBrush.ForHeaderState(State);

    public Brush ValueBrush => CStatusBrush.ForHeaderState(State);

    public CButtonCommand Previous => PreviousCommand ?? CButtonCommand.NoOp;

    public CButtonCommand Next => NextCommand ?? CButtonCommand.NoOp;

    public CButtonCommand Open => OpenCommand ?? CButtonCommand.NoOp;
}

public sealed record ST_SCREEN_SECTION(
    string Title,
    IReadOnlyList<ST_DISPLAY_ITEM> Items);

public sealed class CScreenViewModel(
    EN_MENU menu,
    string title,
    string subtitle,
    IReadOnlyList<ST_DISPLAY_ITEM> metrics,
    IReadOnlyList<ST_SCREEN_SECTION> sections,
    bool showCycleControls = false,
    CMenuMain? mainOperating = null,
    CMenuManual? manual = null,
    CMenuRecipe? recipe = null,
    CMenuSetting? setting = null,
    CMenuAlarm? alarm = null,
    CMenuMonitor? monitor = null,
    CMenuPm? pm = null)
{
    public EN_MENU Menu { get; } = menu;

    public string Title { get; } = title;

    public string Subtitle { get; } = subtitle;

    public IReadOnlyList<ST_DISPLAY_ITEM> Metrics { get; } = metrics;

    public IReadOnlyList<ST_SCREEN_SECTION> Sections { get; } = sections;

    public bool ShowCycleControls { get; } = showCycleControls;

    public CMenuMain? MainOperating { get; } = mainOperating;

    public CMenuManual? Manual { get; } = manual;

    public CMenuRecipe? Recipe { get; } = recipe;

    public CMenuSetting? Setting { get; } = setting;

    public CMenuAlarm? Alarm { get; } = alarm;

    public CMenuMonitor? Monitor { get; } = monitor;

    public CMenuPm? Pm { get; } = pm;

    public bool IsMainLayout => Menu == EN_MENU.Main;

    public bool IsManualLayout => Menu == EN_MENU.Manual;

    public bool IsRecipeLayout => Menu == EN_MENU.Recipe;

    public bool IsSettingLayout => Menu == EN_MENU.Setting;

    public bool IsAlarmLayout => Menu == EN_MENU.Alarm;

    public bool IsMonitorLayout => Menu == EN_MENU.Monitor;

    public bool IsPmLayout => Menu == EN_MENU.Pm;

    public bool IsGenericLayout =>
        !IsMainLayout &&
        !IsManualLayout &&
        !IsRecipeLayout &&
        !IsSettingLayout &&
        !IsAlarmLayout &&
        !IsMonitorLayout &&
        !IsPmLayout;
}

public sealed class CMenuItem(EN_MENU menu, string name)
{
    public EN_MENU Menu { get; } = menu;

    public string Name { get; } = name;

    public Geometry IconGeometry => CMenuIcon.Get(Menu);

    public string Description => Menu switch
    {
        EN_MENU.Main => "Auto",
        EN_MENU.Manual => "Manual",
        EN_MENU.Recipe => "Recipe",
        EN_MENU.Setting => "Setting",
        EN_MENU.Alarm => "Alarm",
        EN_MENU.Monitor => "Monitor",
        EN_MENU.Pm => "PM Lock",
        EN_MENU.Exit => "Exit",
        _ => string.Empty
    };
}

internal static class CMenuIcon
{
    private static readonly IReadOnlyDictionary<EN_MENU, Geometry> Icons =
        new Dictionary<EN_MENU, Geometry>
        {
            [EN_MENU.Main] = Icon("M3,11 L12,4 L21,11 M5,10 V21 H10 V15 H14 V21 H19 V10"),
            [EN_MENU.Manual] = Icon("M7,12 V6 M10,12 V4 M13,12 V5 M16,13 V7 M7,12 L5,10 C4,9 3,10 4,12 L6,17 C7,20 9,22 12,22 H14 C17,22 19,19 19,16 V10"),
            [EN_MENU.Recipe] = Icon("M6,4 H18 V20 H6 Z M9,8 H15 M9,12 H15 M9,16 H13"),
            [EN_MENU.Setting] = Icon("M12,8 C14.2,8 16,9.8 16,12 C16,14.2 14.2,16 12,16 C9.8,16 8,14.2 8,12 C8,9.8 9.8,8 12,8 M12,2 V5 M12,19 V22 M2,12 H5 M19,12 H22 M4.9,4.9 L7,7 M17,17 L19.1,19.1 M19.1,4.9 L17,7 M7,17 L4.9,19.1"),
            [EN_MENU.Alarm] = Icon("M8,18 H16 M10,20 H14 M6,17 H18 L16,15 V10 C16,7.8 14.2,6 12,6 C9.8,6 8,7.8 8,10 V15 Z M10,6 C10,4.9 10.9,4 12,4 C13.1,4 14,4.9 14,6"),
            [EN_MENU.Monitor] = Icon("M4,5 H20 V16 H4 Z M9,20 H15 M12,16 V20 M7,11 H10 L11.5,8 L14,14 L15,11 H17"),
            [EN_MENU.Pm] = Icon("M15,4 C17,3 19,4 20,5 L17,8 L19,10 L16,13 L14,11 L7,18 C6,19 5,19 4,18 C3,17 3,16 4,15 L11,8 L9,6 L12,3 Z"),
            [EN_MENU.Exit] = Icon("M10,5 H5 V19 H10 M13,8 L18,12 L13,16 M18,12 H8")
        };

    public static Geometry Get(EN_MENU menu)
    {
        return Icons.TryGetValue(menu, out var geometry)
            ? geometry
            : Geometry.Empty;
    }

    private static Geometry Icon(string data)
    {
        var geometry = Geometry.Parse(data);
        geometry.Freeze();
        return geometry;
    }
}




