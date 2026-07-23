using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;
using Drilling.Common.Recipe;

namespace Drilling.UI.Menu.Menus;

public sealed class CMenuMain(
    IStationManager stationManager,
    IRecipeManager recipeManager,
    ISettingManager settingManager,
    Func<string> selectedRecipeIdProvider,
    Func<IReadOnlySet<int>> selectedPreviewHeadNosProvider,
    CButtonCommand togglePreviewHeadCommand) : IMenu
{
    public EN_MENU Menu => EN_MENU.Main;

    public IReadOnlyList<ST_HEAD_PREVIEW> HeadPreviews { get; private set; } = [];

    public IReadOnlyList<ST_HEAD_PREVIEW> OddHeadPreviews { get; private set; } = [];

    public IReadOnlyList<ST_HEAD_PREVIEW> EvenHeadPreviews { get; private set; } = [];

    public IReadOnlyList<ST_HEAD_ASSIGNMENT_AREA> HeadAssignmentAreas { get; private set; } = [];

    public ST_GLASS_PREVIEW_FRAME GlassFrame { get; private set; } =
        new(44.0, 42.0, 772.0, 238.0);

    public string GlassPreviewSummary { get; private set; } = "0 heads / 0 points";

    public ImageSource? RecipePreviewImage { get; private set; }

    public IReadOnlyList<ST_CELL_PREVIEW_LABEL> CellPreviewLabels { get; private set; } = [];

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

    public CButtonCommand TogglePreviewHeadCommand { get; private set; } = CButtonCommand.NoOp;

    public async Task<CScreenViewModel> Build(CancellationToken cancellationToken = default)
    {
        var snapshot = await stationManager.GetStatus(cancellationToken);
        var selectedHeadNos = selectedPreviewHeadNosProvider().ToHashSet();
        var previewParameters = await LoadPreviewParameters(snapshot, cancellationToken);
        var previewHeadLayout = await LoadPreviewHeadLayout(cancellationToken);

        var metrics = new List<ST_DISPLAY_ITEM>
        {
            new("Cycle State", snapshot.ProcessStep.ToString()),
            new("Script", FormatScriptStatus(snapshot.ScriptStatus)),
            new("Heads", snapshot.HeadPreviews.Count.ToString()),
            new("Result", snapshot.Result?.Message ?? "Waiting")
        };

        var headAssignmentMap = BuildHeadAssignmentMap(snapshot, previewParameters, selectedHeadNos, previewHeadLayout);
        var recipePreview = BuildRecipePreview(previewParameters, selectedHeadNos, previewHeadLayout);
        var displayedHeadAreas = headAssignmentMap.Areas
            .Select(area => area with
            {
                PointCount = recipePreview.HeadPointCounts.TryGetValue(area.HeadNo, out var count)
                    ? (int)Math.Min(int.MaxValue, count)
                    : 0
            })
            .ToArray();
        var glassPreviewSummary =
            $"{displayedHeadAreas.Length}H / {recipePreview.TotalPointCount:N0}P / {FormatGlassSizeText(previewParameters)}" +
            (recipePreview.UnassignedPointCount > 0 ? $" / U:{recipePreview.UnassignedPointCount:N0}" : "");
        var headStatusMap = snapshot.HeadPreviews
            .GroupBy(head => head.HeadNo)
            .ToDictionary(group => group.Key, group => group.Last().Status);

        var headItems = displayedHeadAreas
            .Select(head => new ST_DISPLAY_ITEM(
                $"Head {head.HeadNo:00}",
                headStatusMap.TryGetValue(head.HeadNo, out var status) ? status.ToString() : "Ready",
                "Head assignment pending"))
            .ToArray();

        var headPreviewItems = displayedHeadAreas
            .Select(head => BuildHeadPreviewItem(
                head.HeadNo,
                headStatusMap.TryGetValue(head.HeadNo, out var status) ? status : EN_HEAD_PROCESS_STATUS.Ready,
                selectedHeadNos))
            .ToArray();

        Apply(
            headPreviewItems,
            displayedHeadAreas,
            recipePreview.Frame,
            glassPreviewSummary,
            recipePreview.Image,
            recipePreview.CellLabels,
            [
                new("Cycle State", snapshot.ProcessStep.ToString()),
                new("Script Status", FormatScriptStatus(snapshot.ScriptStatus)),
                new("Automation", "Simulation"),
                new("Preview Source", "Recipe Cell drilling points")
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
            togglePreviewHeadCommand);

        return new CScreenViewModel(
            EN_MENU.Main,
            "MAIN",
            "Automatic operation, 8-head path preview, script status, and cycle result.",
            metrics,
            [
                new("8 Head Preview Source", headItems)
            ],
            showCycleControls: true,
            this);
    }

    private void Apply(
        IReadOnlyList<ST_HEAD_PREVIEW> headPreviews,
        IReadOnlyList<ST_HEAD_ASSIGNMENT_AREA> headAssignmentAreas,
        ST_GLASS_PREVIEW_FRAME glassFrame,
        string glassPreviewSummary,
        ImageSource? recipePreviewImage,
        IReadOnlyList<ST_CELL_PREVIEW_LABEL> cellPreviewLabels,
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
        CButtonCommand togglePreviewHeadCommand)
    {
        HeadPreviews = headPreviews;
        OddHeadPreviews = headPreviews
            .Where(head => head.HeadNo % 2 != 0)
            .OrderBy(head => head.HeadNo)
            .ToArray();
        EvenHeadPreviews = headPreviews
            .Where(head => head.HeadNo % 2 == 0)
            .OrderBy(head => head.HeadNo)
            .ToArray();
        HeadAssignmentAreas = headAssignmentAreas;
        GlassFrame = glassFrame;
        GlassPreviewSummary = glassPreviewSummary;
        RecipePreviewImage = recipePreviewImage;
        CellPreviewLabels = cellPreviewLabels;
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
        TogglePreviewHeadCommand = togglePreviewHeadCommand;
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

    private static ST_HEAD_PREVIEW BuildHeadPreviewItem(
        int headNo,
        EN_HEAD_PROCESS_STATUS status,
        IReadOnlySet<int> selectedHeadNos)
    {
        return new ST_HEAD_PREVIEW(
            headNo,
            $"HEAD {headNo:00}",
            status.ToString(),
            selectedHeadNos.Contains(headNo));
    }

    private static ST_MAIN_RECIPE_PREVIEW BuildRecipePreview(
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlySet<int> selectedHeadNos,
        ST_PREVIEW_HEAD_LAYOUT headLayout)
    {
        const double canvasWidth = 860.0;
        const double canvasHeight = 520.0;
        var frame = CreateGlassFrame(parameters);
        var glassWidth = ReadDoubleAny(parameters, 0.0, "GLASS_SIZE_X", "GLASS_WIDTH");
        var glassHeight = ReadDoubleAny(parameters, 0.0, "GLASS_SIZE_Y", "GLASS_HEIGHT");
        var akMarginX = ReadDoubleAny(parameters, 55.0, "AK_MARGIN_X", "ALIGN_MARGIN_X");
        var akMarginY = ReadDoubleAny(parameters, 45.0, "AK_MARGIN_Y", "ALIGN_MARGIN_Y");
        if (glassWidth <= 0 || glassHeight <= 0)
        {
            return new ST_MAIN_RECIPE_PREVIEW(
                null,
                frame,
                new Dictionary<int, long>(),
                0,
                0,
                []);
        }

        var cellCount = Math.Clamp(ReadIntAny(parameters, 1, "CELL_COUNT", "MAX_CELL_NUMBER"), 1, 1000);
        var headCount = Math.Clamp(ReadIntAny(parameters, 8, "SCANNER_COUNT", "HEAD_COUNT"), 1, 8);
        var scale = Math.Min(frame.Width / glassWidth, frame.Height / glassHeight);
        var drawing = new DrawingGroup();
        var outsideGeometry = new StreamGeometry();
        var outsidePixels = new HashSet<long>();
        var unassignedPixels = new HashSet<long>();
        var headPixels = Enumerable.Range(1, headCount)
            .ToDictionary(headNo => headNo, _ => new Dictionary<long, double>());
        var headPointCounts = Enumerable.Range(1, headCount)
            .ToDictionary(headNo => headNo, _ => 0L);
        var labels = new List<ST_CELL_PREVIEW_LABEL>();
        long unassignedPointCount = 0;
        long totalPoints = 0;

        using (var context = drawing.Open())
        {
            context.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                null,
                new Rect(0, 0, canvasWidth, canvasHeight));

            for (var cellNo = 1; cellNo <= cellCount; cellNo++)
            {
                var firstX = ReadDoubleAny(parameters, 0.0, $"CELL{cellNo}_ALIGN_TO_1ST_PIXEL_X");
                var firstY = ReadDoubleAny(parameters, 0.0, $"CELL{cellNo}_ALIGN_TO_1ST_PIXEL_Y");
                var rotation = ReadDoubleAny(parameters, 0.0, $"CELL{cellNo}_ROTATION");
                var countX = ReadIntAny(parameters, 0, $"CELL{cellNo}_NUM_OF_PIXEL_X");
                var countY = ReadIntAny(parameters, 0, $"CELL{cellNo}_NUM_OF_PIXEL_Y");
                var pitchX = ReadDoubleAny(parameters, 0.0, $"CELL{cellNo}_PITCH_X");
                var pitchY = ReadDoubleAny(parameters, 0.0, $"CELL{cellNo}_PITCH_Y");
                var pixelSize = Math.Max(0.0, ReadDoubleAny(parameters, 0.0, $"CELL{cellNo}_PIXEL_SIZE"));
                var result = CCellPointCalculator.Calculate(new ST_CELL_POINT_INPUT(
                    cellNo,
                    firstX,
                    firstY,
                    rotation,
                    countX,
                    countY,
                    pitchX,
                    pitchY,
                    akMarginX,
                    akMarginY));
                if (!result.IsValid)
                {
                    continue;
                }

                totalPoints += result.Points.Count;
                var radius = pixelSize / 2.0;
                // Main is an operational overview. Keep the physical point center/scale,
                // but enforce a readable marker size after the fixed preview is scaled down.
                var previewSize = Math.Clamp(pixelSize * scale, 3.0, 14.0);
                foreach (var point in result.Points)
                {
                    var canvasX = frame.CanvasLeft + (point.X * scale);
                    var canvasY = frame.CanvasTop + (point.Y * scale);
                    var pixelX = (int)Math.Round(canvasX);
                    var pixelY = (int)Math.Round(canvasY);
                    var pixelKey = ((long)pixelX << 32) | (uint)pixelY;
                    var inside = point.X - radius >= 0 && point.X + radius <= glassWidth &&
                        point.Y - radius >= 0 && point.Y + radius <= glassHeight;
                    if (!inside)
                    {
                        outsidePixels.Add(pixelKey);
                        continue;
                    }

                    var headNo = AssignPreviewHead(point.X, headCount, headLayout);
                    if (headNo <= 0)
                    {
                        unassignedPointCount++;
                        unassignedPixels.Add(pixelKey);
                        continue;
                    }

                    headPointCounts[headNo]++;
                    if (!headPixels[headNo].TryGetValue(pixelKey, out var storedSize) || previewSize > storedSize)
                    {
                        headPixels[headNo][pixelKey] = previewSize;
                    }
                }
                var boundary = BuildRecipeCellBoundary(
                    akMarginX + firstX, akMarginY + firstY, rotation, countX, countY, pitchX, pitchY,
                    Math.Max(radius, previewSize / (2.0 * scale)) + (4.0 / scale),
                    frame, scale);
                // Cell Size is not defined yet. Use the point-pattern bounds only to
                // place the Cell label; drawing it would imply a physical Cell boundary.
                var label = CCellPreviewDrawing.CreateCellLabel(
                    cellNo,
                    boundary.Bounds,
                    canvasWidth,
                    canvasHeight);
                if (label is not null)
                {
                    labels.Add(label);
                }
            }

            foreach (var headNo in Enumerable.Range(1, headCount))
            {
                var geometry = new StreamGeometry();
                using (var geometryContext = geometry.Open())
                {
                    foreach (var item in headPixels[headNo])
                    {
                        AddPreviewCircle(
                            geometryContext,
                            (int)(item.Key >> 32),
                            (int)item.Key,
                            item.Value);
                    }
                }
                geometry.Freeze();
                // Head selection controls only the visible Scan Fields. Point ownership
                // remains equally readable, whether or not another Head is selected.
                var alpha = selectedHeadNos.Count == 0
                    ? (byte)225
                    : selectedHeadNos.Contains(headNo) ? (byte)255 : (byte)225;
                context.DrawGeometry(CreateHeadBrush(headNo, alpha), null, geometry);
            }

            var unassignedGeometry = new StreamGeometry();
            using (var unassignedContext = unassignedGeometry.Open())
            {
                foreach (var pixel in unassignedPixels)
                {
                    AddPreviewCircle(unassignedContext, (int)(pixel >> 32), (int)pixel, 4.0);
                }
            }
            unassignedGeometry.Freeze();
            context.DrawGeometry(new SolidColorBrush(Color.FromRgb(251, 113, 133)), null, unassignedGeometry);

            using (var outsideContext = outsideGeometry.Open())
            {
                foreach (var pixel in outsidePixels)
                {
                    AddPreviewCircle(outsideContext, (int)(pixel >> 32), (int)pixel, 4.0);
                }
            }
            outsideGeometry.Freeze();
            context.DrawGeometry(new SolidColorBrush(Color.FromRgb(248, 113, 113)), null, outsideGeometry);

            CCellPreviewDrawing.DrawAlignKeys(
                context,
                frame,
                glassWidth,
                glassHeight,
                akMarginX,
                akMarginY);

        }

        drawing.Freeze();
        var image = new DrawingImage(drawing);
        image.Freeze();
        return new ST_MAIN_RECIPE_PREVIEW(
            image,
            frame,
            headPointCounts,
            totalPoints,
            unassignedPointCount,
            labels);
    }

    private static int AssignPreviewHead(
        double x,
        int headCount,
        ST_PREVIEW_HEAD_LAYOUT headLayout)
    {
        if (headCount <= 0)
        {
            return 0;
        }

        for (var headNo = 1; headNo <= headCount; headNo++)
        {
            var range = GetPreviewHeadRange(headNo, headLayout);
            if (x >= range.StartX && x <= range.EndX)
            {
                // The latest MOF sample checks H1 -> H8 and assigns the first field match.
                // With centers ordered left-to-right, this gives the left Head priority
                // inside an overlapping Scan Field.
                return headNo;
            }
        }

        return 0;
    }

    private static (double StartX, double EndX) GetPreviewHeadRange(
        int headNo,
        ST_PREVIEW_HEAD_LAYOUT headLayout)
    {
        var centerX = headLayout.H1PositionX + ((Math.Max(1, headNo) - 1) * headLayout.HeadPitchX);
        return (centerX - headLayout.ScanFieldHalfX, centerX + headLayout.ScanFieldHalfX);
    }

    private static StreamGeometry BuildRecipeCellBoundary(
        double firstX, double firstY, double rotation, int countX, int countY,
        double pitchX, double pitchY, double padding, ST_GLASS_PREVIEW_FRAME frame, double scale)
    {
        var radians = rotation * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var maxX = ((countX - 1) * pitchX) + padding;
        var maxY = ((countY - 1) * pitchY) + padding;
        var localCorners = new[]
        {
            new Point(-padding, -padding), new Point(maxX, -padding),
            new Point(maxX, maxY), new Point(-padding, maxY)
        };
        var corners = localCorners.Select(local =>
        {
            var x = firstX + (local.X * cos) - (local.Y * sin);
            var y = firstY + (local.X * sin) + (local.Y * cos);
            return new Point(frame.CanvasLeft + (x * scale), frame.CanvasTop + (y * scale));
        }).ToArray();
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(corners[0], false, true);
            context.PolyLineTo(corners.Skip(1).ToArray(), true, false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static void AddPreviewCircle(StreamGeometryContext context, double x, double y, double size)
    {
        var radius = size / 2.0;
        var control = radius * 0.5522847498;
        context.BeginFigure(new Point(x + radius, y), true, true);
        context.BezierTo(new Point(x + radius, y + control), new Point(x + control, y + radius), new Point(x, y + radius), true, false);
        context.BezierTo(new Point(x - control, y + radius), new Point(x - radius, y + control), new Point(x - radius, y), true, false);
        context.BezierTo(new Point(x - radius, y - control), new Point(x - control, y - radius), new Point(x, y - radius), true, false);
        context.BezierTo(new Point(x + control, y - radius), new Point(x + radius, y - control), new Point(x + radius, y), true, false);
    }

    private static ST_HEAD_ASSIGNMENT_MAP BuildHeadAssignmentMap(
        ST_STATION_PROCESS_STATUS snapshot,
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlySet<int> selectedHeadNos,
        ST_PREVIEW_HEAD_LAYOUT headLayout)
    {
        var fallbackHeadCount = snapshot.HeadPreviews.Count > 0 ? snapshot.HeadPreviews.Count : 8;
        var headCount = Math.Clamp(
            ReadIntAny(parameters, fallbackHeadCount, "SCANNER_COUNT", "HEAD_COUNT", "HeadCount"),
            1,
            8);
        var frame = CreateGlassFrame(parameters);
        var glassWidth = ReadDoubleAny(parameters, 0.0, "GLASS_SIZE_X", "GLASS_WIDTH", "PANEL_SIZE_X", "PANEL_WIDTH");
        var areas = new List<ST_HEAD_ASSIGNMENT_AREA>(headCount);

        (double Left, double Top, double Width, double Height, double LabelLeft, double LabelWidth) GetHeadRect(int headNo)
        {
            if (glassWidth <= 0.0)
            {
                return (frame.CanvasLeft, frame.CanvasTop, 0.0, frame.Height, frame.CanvasLeft, 0.0);
            }

            var scale = frame.Width / glassWidth;
            var range = GetPreviewHeadRange(headNo, headLayout);
            // A Scan Field belongs to the fixed Head, not to the Glass. Draw its full
            // physical width even when part of the field lies outside the Glass frame.
            var left = frame.CanvasLeft + (range.StartX * scale);
            var top = frame.CanvasTop;
            var width = Math.Max(0.0, (range.EndX - range.StartX) * scale);
            var centerX = headLayout.H1PositionX + ((headNo - 1) * headLayout.HeadPitchX);
            var centerCanvasX = frame.CanvasLeft + (centerX * scale);
            var labelWidth = Math.Clamp(headLayout.HeadPitchX * scale, 36.0, 96.0);
            var labelLeft = Math.Clamp(
                centerCanvasX - (labelWidth / 2.0),
                0.0,
                860.0 - labelWidth);

            return (left, top, width, Math.Max(4.0, frame.Height), labelLeft, labelWidth);
        }

        for (var headNo = 1; headNo <= headCount; headNo++)
        {
            var isSelected = selectedHeadNos.Contains(headNo);
            var rect = GetHeadRect(headNo);

            areas.Add(new ST_HEAD_ASSIGNMENT_AREA(
                headNo,
                $"HEAD {headNo:00}",
                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height,
                rect.LabelLeft,
                rect.LabelWidth,
                isSelected,
                0,
                CreateHeadBrush(headNo, isSelected ? (byte)36 : (byte)0),
                CreateHeadBrush(headNo, isSelected ? (byte)255 : (byte)150),
                new Thickness(isSelected ? 2.3 : 1.0),
                1.0));
        }

        return new ST_HEAD_ASSIGNMENT_MAP(
            areas);
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadPreviewParameters(
        ST_STATION_PROCESS_STATUS snapshot,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>(
            snapshot.ProcessModel?.Parameters
                ?? snapshot.ProcessPlan?.Parameters
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        var recipes = await recipeManager.LoadRecipes(cancellationToken);
        if (recipes.Count == 0)
        {
            return parameters;
        }

        var recipeId = selectedRecipeIdProvider();
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            recipeId = snapshot.ProcessPlan?.RecipeId ?? "DRILL_A01";
        }

        var recipe = recipes.FirstOrDefault(item =>
                item.Id.Equals(recipeId, StringComparison.OrdinalIgnoreCase))
            ?? recipes.FirstOrDefault(item =>
                item.Id.Equals("DRILL_A01", StringComparison.OrdinalIgnoreCase))
            ?? recipes[0];

        foreach (var parameter in recipe.Parameters.Where(parameter =>
            !string.IsNullOrWhiteSpace(parameter.Key)))
        {
            parameters[parameter.Key] = parameter.Value;
        }

        return parameters;
    }

    private async Task<ST_PREVIEW_HEAD_LAYOUT> LoadPreviewHeadLayout(CancellationToken cancellationToken)
    {
        const double defaultH1PositionX = 93.75;
        const double defaultHeadPitchX = 187.5;
        const double defaultScanFieldHalfX = 103.125;

        var settings = await settingManager.LoadSection(EN_SETTING_TAB.Position, cancellationToken);
        var h1PositionX = ReadSettingDouble(settings, defaultH1PositionX, "H1PositionX");
        var headPitchX = ReadSettingDouble(settings, defaultHeadPitchX, "HeadPitchX");
        var scanFieldHalfX = ReadSettingDouble(settings, defaultScanFieldHalfX, "ScanFieldHalfX");

        return new ST_PREVIEW_HEAD_LAYOUT(
            h1PositionX,
            headPitchX > 0.0 ? headPitchX : defaultHeadPitchX,
            scanFieldHalfX > 0.0 ? scanFieldHalfX : defaultScanFieldHalfX);
    }

    private static double ReadSettingDouble(
        IReadOnlyList<ST_SYSTEM_PARAMETER> settings,
        double defaultValue,
        params string[] keys)
    {
        foreach (var setting in settings)
        {
            if (!keys.Any(key =>
                    key.Equals(setting.Key, StringComparison.OrdinalIgnoreCase) ||
                    key.Equals(setting.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (double.TryParse(setting.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return defaultValue;
    }

    private static int ReadIntAny(
        IReadOnlyDictionary<string, string> parameters,
        int defaultValue,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (parameters.TryGetValue(key, out var value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return defaultValue;
    }

    private static double ReadDoubleAny(
        IReadOnlyDictionary<string, string> parameters,
        double defaultValue,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (parameters.TryGetValue(key, out var value) &&
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return defaultValue;
    }

    private static ST_GLASS_PREVIEW_FRAME CreateGlassFrame(IReadOnlyDictionary<string, string> parameters)
    {
        const double maxLeft = 44.0;
        const double maxTop = 62.0;
        const double maxWidth = 772.0;
        const double maxHeight = 420.0;
        var glassSizeX = ReadDoubleAny(
            parameters,
            0.0,
            "GLASS_SIZE_X",
            "GLASS_WIDTH",
            "PANEL_SIZE_X",
            "PANEL_WIDTH");
        var glassSizeY = ReadDoubleAny(
            parameters,
            0.0,
            "GLASS_SIZE_Y",
            "GLASS_HEIGHT",
            "PANEL_SIZE_Y",
            "PANEL_HEIGHT");

        if (glassSizeX <= 0.0 || glassSizeY <= 0.0)
        {
            return new ST_GLASS_PREVIEW_FRAME(maxLeft, maxTop, maxWidth, maxHeight);
        }

        var scale = Math.Min(maxWidth / glassSizeX, maxHeight / glassSizeY);
        var width = glassSizeX * scale;
        var height = glassSizeY * scale;
        var left = maxLeft + (maxWidth - width) / 2.0;
        var top = maxTop + (maxHeight - height) / 2.0;
        return new ST_GLASS_PREVIEW_FRAME(left, top, width, height);
    }

    private static string FormatGlassSizeText(IReadOnlyDictionary<string, string> parameters)
    {
        var glassSizeX = ReadDoubleAny(
            parameters,
            0.0,
            "GLASS_SIZE_X",
            "GLASS_WIDTH",
            "PANEL_SIZE_X",
            "PANEL_WIDTH");
        var glassSizeY = ReadDoubleAny(
            parameters,
            0.0,
            "GLASS_SIZE_Y",
            "GLASS_HEIGHT",
            "PANEL_SIZE_Y",
            "PANEL_HEIGHT");

        return glassSizeX > 0.0 && glassSizeY > 0.0
            ? $"Glass {glassSizeX:0.#} x {glassSizeY:0.#} mm"
            : "Glass size fallback";
    }

    public static Brush CreateHeadBrush(int headNo, byte alpha = 255)
    {
        var palette = new[]
        {
            Color.FromRgb(96, 132, 164),
            Color.FromRgb(105, 150, 126),
            Color.FromRgb(161, 132, 83),
            Color.FromRgb(151, 105, 123),
            Color.FromRgb(95, 142, 140),
            Color.FromRgb(131, 123, 164),
            Color.FromRgb(151, 116, 86),
            Color.FromRgb(92, 119, 156)
        };
        var color = palette[Math.Clamp(headNo, 1, palette.Length) - 1];
        return CreateBrush(color.R, color.G, color.B, alpha);
    }

    private static Brush CreateBrush(byte red, byte green, byte blue, byte alpha = 255)
    {
        var brush = new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
        brush.Freeze();
        return brush;
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
            new("H08", true, "LINEARRAY", 0.90, 20.0, 18000, 900, 15.000, -30.000)
        ];
    }
}

internal sealed record ST_HEAD_ASSIGNMENT_MAP(
    IReadOnlyList<ST_HEAD_ASSIGNMENT_AREA> Areas);

internal sealed record ST_MAIN_RECIPE_PREVIEW(
    ImageSource? Image,
    ST_GLASS_PREVIEW_FRAME Frame,
    IReadOnlyDictionary<int, long> HeadPointCounts,
    long TotalPointCount,
    long UnassignedPointCount,
    IReadOnlyList<ST_CELL_PREVIEW_LABEL> CellLabels);

internal sealed record ST_PREVIEW_HEAD_LAYOUT(
    double H1PositionX,
    double HeadPitchX,
    double ScanFieldHalfX);

public sealed record ST_GLASS_PREVIEW_FRAME(
    double CanvasLeft,
    double CanvasTop,
    double Width,
    double Height);

public sealed record ST_HEAD_PREVIEW(
    int HeadNo,
    string HeadName,
    string Status,
    bool IsSelected)
{
    public Brush StatusBrush => CStatusBrush.ForHeadStatus(Status);
}

public sealed record ST_HEAD_ASSIGNMENT_AREA(
    int HeadNo,
    string HeadName,
    double CanvasLeft,
    double CanvasTop,
    double Width,
    double Height,
    double LabelCanvasLeft,
    double LabelWidth,
    bool IsSelected,
    int PointCount,
    Brush FillBrush,
    Brush StrokeBrush,
    Thickness BorderThicknessValue,
    double Opacity)
{
    public double LabelCanvasTop => CanvasTop - 24.0;

    public Visibility RangeVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

    public string DisplayLabel => $"H{HeadNo:00}";
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




