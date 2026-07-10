using System.Windows;
using System.Windows.Media;
using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;

namespace Drilling.UI.Menu.Menus;

public sealed class CMenuMain(
    IStationManager stationManager,
    Func<int> selectedHeadNoProvider,
    CButtonCommand selectHeadCommand) : IMenu
{
    public EN_MENU Menu => EN_MENU.Main;

    public IReadOnlyList<ST_HEAD_PREVIEW> HeadPreviews { get; private set; } = [];

    public ST_HEAD_PREVIEW SelectedHeadPreview { get; private set; } =
        new(0, "HEAD --", "No Data", "0 pts", Geometry.Empty, true);

    public IReadOnlyList<ST_DISPLAY_ITEM> CycleItems { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> ResultItems { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> ProcessSequenceItems { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> CurrentStepDetails { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> ProcessSummaryItems { get; private set; } = [];

    public IReadOnlyList<ST_INTERFACE_LOG_ITEM> InterfaceLogs { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> ScriptStatusItems { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> LifecycleItems { get; private set; } = [];

    public IReadOnlyList<ST_INTERLOCK_ITEM> InterlockItems { get; private set; } = [];

    public IReadOnlyList<ST_HEAD_PARAMETER> HeadParameters { get; private set; } = [];

    public string ProcessStep { get; private set; } = "";

    public string ScriptStatus { get; private set; } = "";

    public string ResultMessage { get; private set; } = "";

    public string TotalPointsText { get; private set; } = "Total Points        0";

    public string MoveCountText { get; private set; } = "Move Count (G0)     0";

    public string LaserOnSegmentsText { get; private set; } = "Laser On Segments   0";

    public string EstimatedTimeText { get; private set; } = "Estimated Time       00:00:00";

    public string ElapsedTimeText { get; private set; } = "Elapsed Time         00:00:00";

    public string ProgressText { get; private set; } = "Progress             0.0%";

    public double ProgressPercent { get; private set; }

    public string ProcessResultValue { get; private set; } = "PENDING";

    public Brush ProcessResultBrush { get; private set; } = CStatusBrush.Wait;

    public CButtonCommand SelectHeadCommand { get; private set; } = CButtonCommand.NoOp;

    public async Task<CScreenViewModel> Build(CancellationToken cancellationToken = default)
    {
        var snapshot = await stationManager.GetStatus(cancellationToken);
        var selectedHeadNo = selectedHeadNoProvider();

        var metrics = new List<ST_DISPLAY_ITEM>
        {
            new("Cycle State", snapshot.ProcessStep.ToString()),
            new("Script", FormatScriptStatus(snapshot.ScriptStatus)),
            new("Heads", snapshot.HeadPreviews.Count.ToString()),
            new("Result", snapshot.Result?.Message ?? "Waiting")
        };

        var headItems = snapshot.HeadPreviews
            .Select(head => new ST_DISPLAY_ITEM(
                $"Head {head.HeadNo:00}",
                head.Status.ToString(),
                $"{head.Points.Count} path points"))
            .ToArray();

        var headPreviewItems = snapshot.HeadPreviews
            .Select(head => BuildHeadPreviewItem(head, selectedHeadNo))
            .ToArray();
        var selectedHeadPreview = headPreviewItems.FirstOrDefault(head => head.IsSelected)
            ?? headPreviewItems.FirstOrDefault()
            ?? new ST_HEAD_PREVIEW(0, "HEAD --", "No Data", "0 pts", Geometry.Empty, true);

        Apply(
            headPreviewItems,
            selectedHeadPreview,
            [
                new("Cycle State", snapshot.ProcessStep.ToString()),
                new("Script Status", FormatScriptStatus(snapshot.ScriptStatus)),
                new("Automation", "Simulation"),
                new("Preview Source", "Pre-script path model")
            ],
            [
                new("Complete Time", snapshot.Result?.CompletedAt.ToString("HH:mm:ss") ?? "-"),
                new("Result", snapshot.Result?.IsSuccess == true ? "OK" : "Waiting"),
                new("Message", snapshot.Result?.Message ?? "Ready")
            ],
            [
                .. snapshot.ProcessSequence.Select(ToDisplayItem)
            ],
            [
                .. snapshot.CurrentStepDetails.Select(ToDisplayItem)
            ],
            [
                .. snapshot.ProcessSummary.Select(ToDisplayItem)
            ],
            [
                .. snapshot.ProcessLogs.Select(ToInterfaceLogItem)
            ],
            [
                .. snapshot.ScriptStatusItems.Select(ToDisplayItem)
            ],
            [
                .. snapshot.ScriptLifecycleItems.Select(ToDisplayItem)
            ],
            [
                .. snapshot.InterlockItems.Select(ToInterlockItem)
            ],
            BuildHeadParameters(),
            snapshot.ProcessStep.ToString(),
            FormatScriptStatus(snapshot.ScriptStatus),
            snapshot.Result?.Message ?? "Waiting for operator command.",
            snapshot.Statistics,
            FormatProcessResult(snapshot),
            selectHeadCommand);

        return new CScreenViewModel(
            EN_MENU.Main,
            "MAIN",
            "Automatic operation, 12-head path preview, script status, and cycle result.",
            metrics,
            [
                new("12 Head Preview Source", headItems)
            ],
            showCycleControls: true,
            this);
    }

    private void Apply(
        IReadOnlyList<ST_HEAD_PREVIEW> headPreviews,
        ST_HEAD_PREVIEW selectedHeadPreview,
        IReadOnlyList<ST_DISPLAY_ITEM> cycleItems,
        IReadOnlyList<ST_DISPLAY_ITEM> resultItems,
        IReadOnlyList<ST_DISPLAY_ITEM> processSequenceItems,
        IReadOnlyList<ST_DISPLAY_ITEM> currentStepDetails,
        IReadOnlyList<ST_DISPLAY_ITEM> processSummaryItems,
        IReadOnlyList<ST_INTERFACE_LOG_ITEM> interfaceLogs,
        IReadOnlyList<ST_DISPLAY_ITEM> scriptStatusItems,
        IReadOnlyList<ST_DISPLAY_ITEM> lifecycleItems,
        IReadOnlyList<ST_INTERLOCK_ITEM> interlockItems,
        IReadOnlyList<ST_HEAD_PARAMETER> headParameters,
        string processStep,
        string scriptStatus,
        string resultMessage,
        ST_PROCESS_STATISTICS statistics,
        string processResultValue,
        CButtonCommand selectHeadCommand)
    {
        HeadPreviews = headPreviews;
        SelectedHeadPreview = selectedHeadPreview;
        CycleItems = cycleItems;
        ResultItems = resultItems;
        ProcessSequenceItems = processSequenceItems;
        CurrentStepDetails = currentStepDetails;
        ProcessSummaryItems = processSummaryItems;
        InterfaceLogs = interfaceLogs;
        ScriptStatusItems = scriptStatusItems;
        LifecycleItems = lifecycleItems;
        InterlockItems = interlockItems;
        HeadParameters = headParameters;
        ProcessStep = processStep;
        ScriptStatus = scriptStatus;
        ResultMessage = resultMessage;
        TotalPointsText = $"Total Points        {statistics.TotalPoints:N0}";
        MoveCountText = $"Move Count (G0)     {statistics.MoveCount:N0}";
        LaserOnSegmentsText = $"Laser On Segments   {statistics.LaserOnSegments:N0}";
        EstimatedTimeText = $"Estimated Time       {FormatDuration(statistics.EstimatedTime)}";
        ElapsedTimeText = $"Elapsed Time         {FormatDuration(statistics.ElapsedTime)}";
        ProgressText = $"Progress             {statistics.ProgressPercent:F1}%";
        ProgressPercent = statistics.ProgressPercent;
        ProcessResultValue = processResultValue;
        ProcessResultBrush = CStatusBrush.ForDisplayState(processResultValue);
        SelectHeadCommand = selectHeadCommand;
    }

    public static string FormatScriptStatus(EN_SCRIPT_STATUS status)
    {
        return status switch
        {
            EN_SCRIPT_STATUS.NotCreated => "Not Created",
            _ => status.ToString()
        };
    }

    private static ST_DISPLAY_ITEM ToDisplayItem(ST_PROCESS_DISPLAY_ITEM item)
    {
        return new ST_DISPLAY_ITEM(item.Name, item.Value, item.Detail);
    }

    private static ST_INTERFACE_LOG_ITEM ToInterfaceLogItem(ST_PROCESS_LOG_ITEM item)
    {
        return new ST_INTERFACE_LOG_ITEM(
            item.OccurredAt.ToString("HH:mm:ss.fff"),
            item.Level,
            item.Source,
            item.Message);
    }

    private static ST_INTERLOCK_ITEM ToInterlockItem(Drilling.Common.InterLock.ST_INTERLOCK_ITEM item)
    {
        return new ST_INTERLOCK_ITEM(
            item.Signal,
            FormatInterLockState(item),
            item.Detail,
            "-");
    }

    private static string FormatInterLockState(Drilling.Common.InterLock.ST_INTERLOCK_ITEM item)
    {
        return item.Level switch
        {
            EN_INTERLOCK_LEVEL.Ok => item.State,
            EN_INTERLOCK_LEVEL.Warn => "WARN",
            EN_INTERLOCK_LEVEL.Error => "ERROR",
            _ => item.State
        };
    }

    private static string FormatProcessResult(ST_STATION_PROCESS_STATUS snapshot)
    {
        if (snapshot.Result is null)
        {
            return "PENDING";
        }

        return snapshot.Result.IsSuccess ? "OK" : "NG";
    }

    private static string FormatDuration(TimeSpan value)
    {
        return value == TimeSpan.Zero
            ? "00:00:00"
            : value.ToString(@"hh\:mm\:ss");
    }

    private static ST_HEAD_PREVIEW BuildHeadPreviewItem(ST_HEAD_PATH_DATA head, int selectedHeadNo)
    {
        return new ST_HEAD_PREVIEW(
            head.HeadNo,
            $"HEAD {head.HeadNo:00}",
            head.Status.ToString(),
            $"{head.Points.Count} pts",
            BuildPreviewGeometry(head.Points),
            head.HeadNo == selectedHeadNo);
    }

    private static Geometry BuildPreviewGeometry(IReadOnlyList<ST_PATH_POINT> points)
    {
        if (points.Count == 0)
        {
            return Geometry.Empty;
        }

        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        var width = Math.Max(maxX - minX, 0.001);
        var height = Math.Max(maxY - minY, 0.001);
        const double previewWidth = 90.0;
        const double previewHeight = 50.0;
        const double previewPadding = 7.0;
        var scale = Math.Min((previewWidth - previewPadding * 2.0) / width, (previewHeight - previewPadding * 2.0) / height);
        var offsetX = previewWidth / 2.0 - width * scale / 2.0;
        var offsetY = previewHeight / 2.0 - height * scale / 2.0;

        Point Map(ST_PATH_POINT point)
        {
            return new Point(
                offsetX + (point.X - minX) * scale,
                offsetY + (point.Y - minY) * scale);
        }

        var figure = new PathFigure
        {
            StartPoint = Map(points[0]),
            IsClosed = true,
            IsFilled = true
        };

        figure.Segments.Add(new PolyLineSegment(points.Skip(1).Select(Map), true));

        var geometry = new PathGeometry([figure]);
        geometry.Freeze();
        return geometry;
    }

    private static IReadOnlyList<ST_HEAD_PARAMETER> BuildHeadParameters()
    {
        return
        [
            new("H01", true, "CIRCLE", 1.00, 20.0, 20000, 900, -80.000, 30.000),
            new("H02", true, "LINE", 0.80, 20.0, 16000, 800, -20.000, 30.000),
            new("H03", true, "SLOT", 1.20, 25.0, 24000, 900, 20.000, 30.000),
            new("H04", true, "MIXED", 1.00, 20.0, 21000, 900, 45.000, -10.000),
            new("H05", true, "RECT", 0.90, 20.0, 18000, 850, -75.000, -30.000),
            new("H06", true, "SPIRAL", 1.10, 20.0, 22000, 800, -15.000, -30.000),
            new("H07", false, "SKIP", 0.00, 0.0, 0, 0, 0.000, 0.000),
            new("H08", true, "LINEARRAY", 0.90, 20.0, 18000, 900, 15.000, -30.000),
            new("H09", true, "ARC", 1.00, 20.0, 20000, 850, 60.000, -30.000),
            new("H10", false, "SKIP", 0.00, 0.0, 0, 0, 0.000, 0.000),
            new("H11", true, "COLUMN", 0.80, 20.0, 16000, 800, -60.000, -50.000),
            new("H12", true, "HOLEPATTERN", 0.90, 20.0, 18000, 900, 60.000, -50.000)
        ];
    }
}

public sealed record ST_HEAD_PREVIEW(
    int HeadNo,
    string HeadName,
    string Status,
    string PointSummary,
    Geometry PreviewGeometry,
    bool IsSelected)
{
    public Brush StatusBrush => CStatusBrush.ForHeadStatus(Status);
}

public sealed record ST_INTERFACE_LOG_ITEM(
    string Time,
    string Level,
    string Source,
    string Message);

public sealed record ST_INTERLOCK_ITEM(
    string Signal,
    string State,
    string Detail,
    string Result);

public sealed record ST_HEAD_PARAMETER(
    string Head,
    bool Use,
    string Shape,
    double Power,
    double Frequency,
    int Shot,
    double Speed,
    double OffsetX,
    double OffsetY);




