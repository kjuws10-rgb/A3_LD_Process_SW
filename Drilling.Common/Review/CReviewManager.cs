using System.Globalization;
using Drilling.Common.Interface;
using Drilling.Common.Managers;
using Drilling.Common.Recipe;

namespace Drilling.Common.Review;

public enum EN_REVIEW_POINT_STATE
{
    Ready,
    Current,
    Ok,
    Ng,
    Skip
}

public enum EN_REVIEW_SEQUENCE_STATE
{
    Idle,
    Running,
    Stopping,
    Stopped,
    Completed,
    Failed
}

public enum EN_REVIEW_RULE_TYPE
{
    AllPoint,
    SamplePoint,
    Edge,
    Center,
    HeadPoint,
    CellPoint,
    ZeroLine
}

public sealed record ST_REVIEW_SEQUENCE_STATUS(
    EN_REVIEW_SEQUENCE_STATE State,
    int TotalCount,
    int CompletedCount,
    int NgCount,
    string Message);

public sealed record ST_REVIEW_RULE_DATA(
    string FileName,
    string RuleName,
    EN_REVIEW_RULE_TYPE RuleType,
    int HeadNo,
    int CellNo,
    int ZeroPointCount,
    IReadOnlyList<string> HoleKeys);

public sealed record ST_REVIEW_PLAN_POINT(
    int PointNo,
    string HoleKey,
    int HeadNo,
    int CellNo,
    int HoleNo,
    int PixelCountX,
    int PixelCountY,
    bool Use,
    double DesignX,
    double DesignY,
    double ReviewTargetX,
    double ReviewTargetY,
    double ErrorX,
    double ErrorY,
    EN_REVIEW_POINT_STATE State,
    string Judge)
{
    public string HeadName => $"H{HeadNo:00}";

    public string CellName => $"CELL{CellNo:00}";

    public string HoleName => CReviewHoleNameFormatter.ToMatrixName(HoleNo, PixelCountX);

    public string PointName => HoleName;
}

public static class CReviewHoleNameFormatter
{
    public static string ToMatrixName(
        int holeNo,
        int columnCount)
    {
        var safeColumnCount = Math.Max(1, columnCount);
        var zeroBasedHoleNo = Math.Max(0, holeNo - 1);
        var column = (zeroBasedHoleNo % safeColumnCount) + 1;
        var row = (zeroBasedHoleNo / safeColumnCount) + 1;

        return $"{ToColumnLetters(column)}{row}";
    }

    private static string ToColumnLetters(int oneBasedColumn)
    {
        var value = Math.Max(1, oneBasedColumn);
        var text = "";

        while (value > 0)
        {
            value--;
            text = (char)('A' + (value % 26)) + text;
            value /= 26;
        }

        return text;
    }
}

public sealed record ST_REVIEW_PLAN(
    string RecipeId,
    string RecipeName,
    int HeadCount,
    int CellCount,
    double ToleranceX,
    double ToleranceY,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ST_REVIEW_PLAN_POINT> Points)
{
    public IReadOnlyList<ST_REVIEW_PLAN_POINT> ReviewPoints => Points
        .Where(point => point.Use)
        .ToArray();

    public int TotalPointCount => Points.Count;

    public int ReviewPointCount => Points.Count(point => point.Use);
}

public sealed record ST_REVIEW_RESULT_DATA(
    ST_REVIEW_PLAN Plan,
    IReadOnlyList<ST_REVIEW_PLAN_POINT> Results,
    DateTimeOffset SavedAt);

public interface IReviewResultFile
{
    Task Save(
        ST_REVIEW_RESULT_DATA result,
        CancellationToken cancellationToken = default);
}

public interface IReviewRuleFile
{
    Task<IReadOnlyList<string>> List(CancellationToken cancellationToken = default);

    Task<ST_REVIEW_RULE_DATA> Load(
        string ruleFileName,
        CancellationToken cancellationToken = default);

    Task Save(
        ST_REVIEW_RULE_DATA rule,
        CancellationToken cancellationToken = default);
}

public interface IReviewManager
{
    ST_REVIEW_PLAN? CurrentPlan { get; }

    EN_REVIEW_SEQUENCE_STATE SequenceState { get; }

    ST_REVIEW_PLAN CreatePlan(
        ST_RECIPE_DATA recipe,
        IReadOnlyCollection<string> selectedHoleKeys);

    ST_REVIEW_PLAN CreatePlan(
        ST_RECIPE_DATA recipe,
        ST_REVIEW_RULE_DATA rule);

    Task<ST_REVIEW_SEQUENCE_STATUS> Start(
        ST_REVIEW_PLAN plan,
        Action<ST_REVIEW_PLAN>? progress = null,
        CancellationToken cancellationToken = default);

    void Stop();

    Task<ST_REVIEW_SEQUENCE_STATUS> RetryRemaining(
        Action<ST_REVIEW_PLAN>? progress = null,
        CancellationToken cancellationToken = default);

    Task SaveResult(
        ST_REVIEW_PLAN plan,
        IReadOnlyList<ST_REVIEW_PLAN_POINT> results,
        CancellationToken cancellationToken = default);
}

public sealed class CReviewManager(
    IReviewResultFile reviewResultFile,
    IInterfaceManager interfaceManager,
    ISettingManager settingManager) : IReviewManager
{
    private const int MaxHeadCount = 8;
    private const int DefaultHeadCount = 8;
    private const int DefaultCellCount = 20;
    private const double DefaultH1PositionX = 93.75;
    private const double DefaultHeadPitchX = 187.5;
    private const double DefaultScanFieldHalfX = 103.125;
    private const double ReviewSequenceRowTolerance = 0.001;
    private readonly SemaphoreSlim _sequenceLock = new(1, 1);
    private readonly object _stateLock = new();
    private bool _stopRequested;
    private ST_REVIEW_PLAN? _currentPlan;

    public ST_REVIEW_PLAN? CurrentPlan
    {
        get
        {
            lock (_stateLock)
            {
                return _currentPlan;
            }
        }
    }

    public EN_REVIEW_SEQUENCE_STATE SequenceState { get; private set; } = EN_REVIEW_SEQUENCE_STATE.Idle;

    public ST_REVIEW_PLAN CreatePlan(
        ST_RECIPE_DATA recipe,
        IReadOnlyCollection<string> selectedHoleKeys)
    {
        var selectedSet = selectedHoleKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(NormalizeHoleKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return CreatePlanCore(
            recipe,
            point => selectedSet.Contains(point.HoleKey));
    }

    public ST_REVIEW_PLAN CreatePlan(
        ST_RECIPE_DATA recipe,
        ST_REVIEW_RULE_DATA rule)
    {
        var allPlan = CreatePlan(recipe, Array.Empty<string>());
        var selectedKeys = BuildRuleHoleKeys(rule, allPlan);

        return CreatePlan(recipe, selectedKeys);
    }

    public async Task<ST_REVIEW_SEQUENCE_STATUS> Start(
        ST_REVIEW_PLAN plan,
        Action<ST_REVIEW_PLAN>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _sequenceLock.WaitAsync(0, cancellationToken))
        {
            return CreateStatus(
                CurrentPlan ?? plan,
                EN_REVIEW_SEQUENCE_STATE.Running,
                "Review sequence is already running.");
        }

        try
        {
            _stopRequested = false;
            SequenceState = EN_REVIEW_SEQUENCE_STATE.Running;
            var workingPlan = ResetPlanForRun(plan);
            SetCurrentPlan(workingPlan, progress);

            foreach (var point in OrderByReviewSequence(workingPlan.ReviewPoints))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_stopRequested)
                {
                    SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
                    workingPlan = SetWaitingPointsReady(workingPlan);
                    SetCurrentPlan(workingPlan, progress);
                    return CreateStatus(workingPlan, SequenceState, "Review sequence stopped.");
                }

                var currentPoint = point with
                {
                    State = EN_REVIEW_POINT_STATE.Current,
                    Judge = "WAIT"
                };
                workingPlan = UpdatePoint(workingPlan, currentPoint);
                SetCurrentPlan(workingPlan, progress);

                await MoveStageY(currentPoint, cancellationToken);

                if (_stopRequested)
                {
                    SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
                    workingPlan = UpdatePoint(workingPlan, currentPoint with { State = EN_REVIEW_POINT_STATE.Ready });
                    SetCurrentPlan(workingPlan, progress);
                    return CreateStatus(workingPlan, SequenceState, "Review sequence stopped.");
                }

                await MoveVisionX(currentPoint, cancellationToken);

                if (_stopRequested)
                {
                    SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
                    workingPlan = UpdatePoint(workingPlan, currentPoint with { State = EN_REVIEW_POINT_STATE.Ready });
                    SetCurrentPlan(workingPlan, progress);
                    return CreateStatus(workingPlan, SequenceState, "Review sequence stopped.");
                }

                var measurement = await MeasureVision(currentPoint, cancellationToken);

                if (_stopRequested)
                {
                    SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
                    workingPlan = UpdatePoint(workingPlan, currentPoint with { State = EN_REVIEW_POINT_STATE.Ready });
                    SetCurrentPlan(workingPlan, progress);
                    return CreateStatus(workingPlan, SequenceState, "Review sequence stopped.");
                }

                var measuredPoint = ApplyMeasurement(workingPlan, currentPoint, measurement);
                workingPlan = UpdatePoint(workingPlan, measuredPoint);
                SetCurrentPlan(workingPlan, progress);
            }

            SequenceState = EN_REVIEW_SEQUENCE_STATE.Completed;
            SetCurrentPlan(workingPlan, progress);
            await SaveResult(workingPlan, workingPlan.ReviewPoints, cancellationToken);
            return CreateStatus(workingPlan, SequenceState, "Review sequence completed.");
        }
        catch (OperationCanceledException)
        {
            SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
            var stoppedPlan = SetWaitingPointsReady(CurrentPlan ?? plan);
            SetCurrentPlan(stoppedPlan, progress);
            return CreateStatus(stoppedPlan, SequenceState, "Review sequence canceled.");
        }
        catch (Exception ex)
        {
            SequenceState = EN_REVIEW_SEQUENCE_STATE.Failed;
            var failedPlan = SetWaitingPointsReady(CurrentPlan ?? plan);
            SetCurrentPlan(failedPlan, progress);
            return CreateStatus(failedPlan, SequenceState, ex.Message);
        }
        finally
        {
            _sequenceLock.Release();
        }
    }

    public void Stop()
    {
        _stopRequested = true;
        if (SequenceState == EN_REVIEW_SEQUENCE_STATE.Running)
        {
            SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopping;
        }
    }

    public async Task<ST_REVIEW_SEQUENCE_STATUS> RetryRemaining(
        Action<ST_REVIEW_PLAN>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var plan = CurrentPlan;
        if (plan is null)
        {
            return new ST_REVIEW_SEQUENCE_STATUS(
                EN_REVIEW_SEQUENCE_STATE.Idle,
                0,
                0,
                0,
                "Review plan is empty.");
        }

        plan = SetWaitingPointsReady(plan);
        SetCurrentPlan(plan, progress);

        var points = plan.ReviewPoints
            .Where(point => point.State == EN_REVIEW_POINT_STATE.Ready);
        var orderedPoints = OrderByReviewSequence(points)
            .ToArray();

        if (orderedPoints.Length == 0)
        {
            return CreateStatus(plan, SequenceState, "Ready review point is empty.");
        }

        return await RunRetryPoints(orderedPoints, "Review ready point retry completed.", progress, cancellationToken);
    }

    public Task SaveResult(
        ST_REVIEW_PLAN plan,
        IReadOnlyList<ST_REVIEW_PLAN_POINT> results,
        CancellationToken cancellationToken = default)
    {
        return reviewResultFile.Save(
            new ST_REVIEW_RESULT_DATA(plan, results, DateTimeOffset.Now),
            cancellationToken);
    }

    private ST_REVIEW_PLAN CreatePlanCore(
        ST_RECIPE_DATA recipe,
        Func<ST_REVIEW_PLAN_POINT, bool> useSelector)
    {
        var headCount = Math.Clamp(
            ReadInt(recipe, DefaultHeadCount, "SCANNER_COUNT", "HEAD_COUNT"),
            1,
            MaxHeadCount);
        var cellCount = Math.Max(1, ReadInt(recipe, DefaultCellCount, "CELL_COUNT", "MAX_CELL_NUMBER"));
        var toleranceX = ReadDouble(recipe, 0.030, "REVIEW_TOLERANCE_X", "CORRECTION_SPEC_X", "REVIEW_TOLERANCE");
        var toleranceY = ReadDouble(recipe, 0.030, "REVIEW_TOLERANCE_Y", "CORRECTION_SPEC_Y", "REVIEW_TOLERANCE");
        var headLayout = LoadHeadLayout();
        var points = BuildHolePoints(recipe, headCount, cellCount, headLayout)
            .Select(point =>
            {
                var use = useSelector(point);

                return point with
                {
                    Use = use,
                    State = use ? EN_REVIEW_POINT_STATE.Ready : EN_REVIEW_POINT_STATE.Skip,
                    Judge = use ? "WAIT" : "-"
                };
            })
            .ToArray();

        return new ST_REVIEW_PLAN(
            recipe.Id,
            string.IsNullOrWhiteSpace(recipe.Name) ? recipe.Id : recipe.Name,
            headCount,
            cellCount,
            toleranceX,
            toleranceY,
            DateTimeOffset.Now,
            points);
    }

    private ST_REVIEW_HEAD_LAYOUT LoadHeadLayout()
    {
        var settings = settingManager
            .LoadSection(EN_SETTING_TAB.Position)
            .GetAwaiter()
            .GetResult();
        var h1PositionX = ReadSettingDouble(settings, DefaultH1PositionX, "H1PositionX");
        var headPitchX = ReadSettingDouble(settings, DefaultHeadPitchX, "HeadPitchX");
        var scanFieldHalfX = ReadSettingDouble(settings, DefaultScanFieldHalfX, "ScanFieldHalfX");

        return new ST_REVIEW_HEAD_LAYOUT(
            h1PositionX,
            headPitchX > 0.0 ? headPitchX : DefaultHeadPitchX,
            scanFieldHalfX > 0.0 ? scanFieldHalfX : DefaultScanFieldHalfX);
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

    private static IReadOnlyList<ST_REVIEW_PLAN_POINT> BuildHolePoints(
        ST_RECIPE_DATA recipe,
        int headCount,
        int cellCount,
        ST_REVIEW_HEAD_LAYOUT headLayout)
    {
        var akMarginX = ReadDouble(recipe, 55.0, "AK_MARGIN_X", "ALIGN_MARGIN_X");
        var akMarginY = ReadDouble(recipe, 45.0, "AK_MARGIN_Y", "ALIGN_MARGIN_Y");
        var reviewOffsetX = ReadDouble(recipe, 0.0, "REVIEW_OFFSET_X");
        var reviewOffsetY = ReadDouble(recipe, 0.0, "REVIEW_OFFSET_Y");
        var globalPixelCountX = Math.Max(1, ReadInt(recipe, 1, "NUM_OF_PIXEL_X", "PIXEL_COUNT_X"));
        var globalPixelCountY = Math.Max(1, ReadInt(recipe, 1, "NUM_OF_PIXEL_Y", "PIXEL_COUNT_Y"));
        var globalPitchX = ReadDouble(recipe, 0.0, "PITCH_X", "PITCH");
        var globalPitchY = ReadDouble(recipe, globalPitchX, "PITCH_Y", "PITCH");
        var points = new List<ST_REVIEW_PLAN_POINT>();
        var pointNo = 1;

        for (var cellNo = 1; cellNo <= cellCount; cellNo++)
        {
            var holeCount = ReadInt(
                recipe,
                -1,
                $"CELL{cellNo}_HOLE_COUNT",
                $"CELL{cellNo:00}_HOLE_COUNT",
                $"CELL{cellNo}_DRILL_HOLE_COUNT",
                $"CELL{cellNo:00}_DRILL_HOLE_COUNT");

            if (holeCount <= 0)
            {
                foreach (var point in CreateFallbackHoleGrid(
                    recipe,
                    pointNo,
                    cellNo,
                    headCount,
                    headLayout,
                    akMarginX,
                    akMarginY,
                    reviewOffsetX,
                    reviewOffsetY,
                    globalPixelCountX,
                    globalPixelCountY,
                    globalPitchX,
                    globalPitchY))
                {
                    points.Add(point);
                    pointNo++;
                }

                continue;
            }

            var cellBaseX = ReadDouble(recipe, 0.0, $"CELL{cellNo}_ALIGN_TO_1ST_PIXEL_X", $"CELL{cellNo:00}_ALIGN_TO_1ST_PIXEL_X");
            var cellBaseY = ReadDouble(recipe, 0.0, $"CELL{cellNo}_ALIGN_TO_1ST_PIXEL_Y", $"CELL{cellNo:00}_ALIGN_TO_1ST_PIXEL_Y");
            var rotation = ReadDouble(recipe, 0.0, $"CELL{cellNo}_ROTATION", $"CELL{cellNo:00}_ROTATION");
            var radians = rotation * Math.PI / 180.0;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);

            for (var holeNo = 1; holeNo <= holeCount; holeNo++)
            {
                var prefixA = $"CELL{cellNo}_HOLE{holeNo}";
                var prefixB = $"CELL{cellNo:00}_HOLE{holeNo:0000}";
                var localX = ReadDouble(recipe, (holeNo - 1) * globalPitchX, $"{prefixA}_X", $"{prefixB}_X", $"{prefixA}_DESIGN_X", $"{prefixB}_DESIGN_X", $"{prefixA}_POS_X", $"{prefixB}_POS_X");
                var localY = ReadDouble(recipe, 0.0, $"{prefixA}_Y", $"{prefixB}_Y", $"{prefixA}_DESIGN_Y", $"{prefixB}_DESIGN_Y", $"{prefixA}_POS_Y", $"{prefixB}_POS_Y");
                var rotatedX = (localX * cos) - (localY * sin);
                var rotatedY = (localX * sin) + (localY * cos);
                var designX = akMarginX + cellBaseX + rotatedX;
                var designY = akMarginY + cellBaseY + rotatedY;
                var pixelCountX = Math.Max(1, ReadInt(recipe, globalPixelCountX, $"{prefixA}_PIXEL_COUNT_X", $"{prefixB}_PIXEL_COUNT_X", $"{prefixA}_NUM_OF_PIXEL_X", $"{prefixB}_NUM_OF_PIXEL_X"));
                var pixelCountY = Math.Max(1, ReadInt(recipe, globalPixelCountY, $"{prefixA}_PIXEL_COUNT_Y", $"{prefixB}_PIXEL_COUNT_Y", $"{prefixA}_NUM_OF_PIXEL_Y", $"{prefixB}_NUM_OF_PIXEL_Y"));
                var headNo = AssignHeadNo(designX, headCount, headLayout);

                points.Add(new ST_REVIEW_PLAN_POINT(
                    pointNo++,
                    ToHoleKey(cellNo, holeNo),
                    headNo,
                    cellNo,
                    holeNo,
                    pixelCountX,
                    pixelCountY,
                    false,
                    designX,
                    designY,
                    designX + reviewOffsetX,
                    designY + reviewOffsetY,
                    0.0,
                    0.0,
                    EN_REVIEW_POINT_STATE.Skip,
                    "-"));
            }
        }

        return points;
    }

    private static IReadOnlyList<ST_REVIEW_PLAN_POINT> CreateFallbackHoleGrid(
        ST_RECIPE_DATA recipe,
        int startPointNo,
        int cellNo,
        int headCount,
        ST_REVIEW_HEAD_LAYOUT headLayout,
        double akMarginX,
        double akMarginY,
        double reviewOffsetX,
        double reviewOffsetY,
        int globalPixelCountX,
        int globalPixelCountY,
        double globalPitchX,
        double globalPitchY)
    {
        var cellBaseX = ReadDouble(recipe, 0.0, $"CELL{cellNo}_ALIGN_TO_1ST_PIXEL_X", $"CELL{cellNo:00}_ALIGN_TO_1ST_PIXEL_X");
        var cellBaseY = ReadDouble(recipe, 0.0, $"CELL{cellNo}_ALIGN_TO_1ST_PIXEL_Y", $"CELL{cellNo:00}_ALIGN_TO_1ST_PIXEL_Y");
        var rotation = ReadDouble(recipe, 0.0, $"CELL{cellNo}_ROTATION", $"CELL{cellNo:00}_ROTATION");
        var pixelCountX = Math.Max(1, ReadInt(
            recipe,
            globalPixelCountX,
            $"CELL{cellNo}_NUM_OF_PIXEL_X",
            $"CELL{cellNo:00}_NUM_OF_PIXEL_X",
            $"CELL{cellNo}_PIXEL_COUNT_X",
            $"CELL{cellNo:00}_PIXEL_COUNT_X"));
        var pixelCountY = Math.Max(1, ReadInt(
            recipe,
            globalPixelCountY,
            $"CELL{cellNo}_NUM_OF_PIXEL_Y",
            $"CELL{cellNo:00}_NUM_OF_PIXEL_Y",
            $"CELL{cellNo}_PIXEL_COUNT_Y",
            $"CELL{cellNo:00}_PIXEL_COUNT_Y"));
        var pitchX = ReadDouble(
            recipe,
            globalPitchX,
            $"CELL{cellNo}_PITCH_X",
            $"CELL{cellNo:00}_PITCH_X",
            $"CELL{cellNo}_PITCH",
            $"CELL{cellNo:00}_PITCH");
        var pitchY = ReadDouble(
            recipe,
            globalPitchY,
            $"CELL{cellNo}_PITCH_Y",
            $"CELL{cellNo:00}_PITCH_Y",
            $"CELL{cellNo}_PITCH",
            $"CELL{cellNo:00}_PITCH");
        pitchX = pitchX > 0.0 ? pitchX : 1.0;
        pitchY = pitchY > 0.0 ? pitchY : pitchX;
        var calculatedPoints = CCellPointCalculator.Calculate(new ST_CELL_POINT_INPUT(
            cellNo,
            cellBaseX,
            cellBaseY,
            rotation,
            pixelCountX,
            pixelCountY,
            pitchX,
            pitchY,
            akMarginX,
            akMarginY));
        if (!calculatedPoints.IsValid)
        {
            return [];
        }

        var points = new List<ST_REVIEW_PLAN_POINT>(calculatedPoints.Points.Count);
        var pointNo = startPointNo;

        foreach (var point in calculatedPoints.Points)
        {
            var headNo = AssignHeadNo(point.X, headCount, headLayout);
            points.Add(new ST_REVIEW_PLAN_POINT(
                pointNo++,
                ToHoleKey(cellNo, point.PointNo),
                headNo,
                cellNo,
                point.PointNo,
                pixelCountX,
                pixelCountY,
                false,
                point.X,
                point.Y,
                point.X + reviewOffsetX,
                point.Y + reviewOffsetY,
                0.0,
                0.0,
                EN_REVIEW_POINT_STATE.Skip,
                "-"));
        }

        return points;
    }

    private static IReadOnlyCollection<string> BuildRuleHoleKeys(
        ST_REVIEW_RULE_DATA rule,
        ST_REVIEW_PLAN allPlan)
    {
        if (rule.RuleType is EN_REVIEW_RULE_TYPE.SamplePoint && rule.HoleKeys.Count > 0)
        {
            return rule.HoleKeys
                .Select(NormalizeHoleKey)
                .Where(key => allPlan.Points.Any(point => point.HoleKey.Equals(key, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (rule.HoleKeys.Count > 0 && rule.RuleType is not EN_REVIEW_RULE_TYPE.AllPoint)
        {
            return rule.HoleKeys
                .Select(NormalizeHoleKey)
                .Where(key => allPlan.Points.Any(point => point.HoleKey.Equals(key, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return rule.RuleType switch
        {
            EN_REVIEW_RULE_TYPE.AllPoint => allPlan.Points.Select(point => point.HoleKey).ToArray(),
            EN_REVIEW_RULE_TYPE.Edge => SelectEdgeKeys(allPlan),
            EN_REVIEW_RULE_TYPE.Center => SelectCenterKeys(allPlan),
            EN_REVIEW_RULE_TYPE.HeadPoint => allPlan.Points
                .Where(point => point.HeadNo == Math.Clamp(rule.HeadNo, 1, Math.Max(1, allPlan.HeadCount)))
                .Select(point => point.HoleKey)
                .ToArray(),
            EN_REVIEW_RULE_TYPE.CellPoint => allPlan.Points
                .Where(point => point.CellNo == Math.Clamp(rule.CellNo, 1, Math.Max(1, allPlan.CellCount)))
                .Select(point => point.HoleKey)
                .ToArray(),
            EN_REVIEW_RULE_TYPE.ZeroLine => SelectZeroLineKeys(allPlan, rule.ZeroPointCount),
            _ => []
        };
    }

    private static IReadOnlyCollection<string> SelectEdgeKeys(ST_REVIEW_PLAN plan)
    {
        return CReviewSampleRuleSelector.SelectEdgeHoleKeys(plan);
    }

    private static IReadOnlyCollection<string> SelectCenterKeys(ST_REVIEW_PLAN plan)
    {
        return CReviewSampleRuleSelector.SelectCenterHoleKeys(plan);
    }

    private static IReadOnlyCollection<string> SelectZeroLineKeys(
        ST_REVIEW_PLAN plan,
        int zeroPointCount)
    {
        if (plan.Points.Count == 0)
        {
            return [];
        }

        var targetY = (plan.Points.Min(point => point.DesignY) + plan.Points.Max(point => point.DesignY)) / 2.0;
        var count = zeroPointCount <= 0 ? Math.Min(5, plan.Points.Count) : Math.Min(zeroPointCount, plan.Points.Count);

        return plan.Points
            .OrderBy(point => Math.Abs(point.DesignY - targetY))
            .ThenBy(point => point.DesignX)
            .Take(count)
            .Select(point => point.HoleKey)
            .ToArray();
    }

    private async Task MoveStageY(
        ST_REVIEW_PLAN_POINT point,
        CancellationToken cancellationToken)
    {
        var command = FormatCommand(
            "REVIEW_STAGE_Y_MOVE",
            ("HOLE_KEY", point.HoleKey),
            ("POINT", point.PointNo.ToString(CultureInfo.InvariantCulture)),
            ("HEAD", point.HeadNo.ToString(CultureInfo.InvariantCulture)),
            ("CELL", point.CellNo.ToString(CultureInfo.InvariantCulture)),
            ("HOLE", point.HoleNo.ToString(CultureInfo.InvariantCulture)),
            ("Y", FormatDouble(point.ReviewTargetY)));

        await interfaceManager.ExecuteFunction(
            EN_EQP_MODULE.WonikCtrl,
            0,
            command,
            cancellationToken);
    }

    private async Task MoveVisionX(
        ST_REVIEW_PLAN_POINT point,
        CancellationToken cancellationToken)
    {
        var command = FormatCommand(
            "REVIEW_VISION_X_MOVE",
            ("HOLE_KEY", point.HoleKey),
            ("POINT", point.PointNo.ToString(CultureInfo.InvariantCulture)),
            ("HEAD", point.HeadNo.ToString(CultureInfo.InvariantCulture)),
            ("CELL", point.CellNo.ToString(CultureInfo.InvariantCulture)),
            ("HOLE", point.HoleNo.ToString(CultureInfo.InvariantCulture)),
            ("X", FormatDouble(point.ReviewTargetX)));

        await interfaceManager.ExecuteFunction(
            EN_EQP_MODULE.WonikCtrl,
            0,
            command,
            cancellationToken);
    }

    private async Task<ST_REVIEW_MEASURE_RESULT> MeasureVision(
        ST_REVIEW_PLAN_POINT point,
        CancellationToken cancellationToken)
    {
        var command = FormatCommand(
            "REVIEW_MEASURE",
            ("HOLE_KEY", point.HoleKey),
            ("POINT", point.PointNo.ToString(CultureInfo.InvariantCulture)),
            ("HEAD", point.HeadNo.ToString(CultureInfo.InvariantCulture)),
            ("CELL", point.CellNo.ToString(CultureInfo.InvariantCulture)),
            ("HOLE", point.HoleNo.ToString(CultureInfo.InvariantCulture)),
            ("X", FormatDouble(point.ReviewTargetX)),
            ("Y", FormatDouble(point.ReviewTargetY)));
        var response = await interfaceManager.ExecuteFunction(
            EN_EQP_MODULE.Vision,
            0,
            command,
            cancellationToken);
        await DelayForSimulation(cancellationToken);

        return ParseVisionResponse(response, point);
    }

    private async Task DelayForSimulation(CancellationToken cancellationToken)
    {
        if (!IsReviewSimulation())
        {
            return;
        }

        for (var step = 0; step < 30; step++)
        {
            if (_stopRequested)
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    private bool IsReviewSimulation()
    {
        return interfaceManager.IsSimulation ||
            interfaceManager.IsSimul(EN_EQP_MODULE.WonikCtrl, 0) ||
            interfaceManager.IsSimul(EN_EQP_MODULE.Vision, 0);
    }

    private static ST_REVIEW_PLAN_POINT ApplyMeasurement(
        ST_REVIEW_PLAN plan,
        ST_REVIEW_PLAN_POINT point,
        ST_REVIEW_MEASURE_RESULT measurement)
    {
        var errorX = measurement.X - point.DesignX;
        var errorY = measurement.Y - point.DesignY;
        var judge = !string.IsNullOrWhiteSpace(measurement.Judge) &&
            !measurement.Judge.Equals("WAIT", StringComparison.OrdinalIgnoreCase)
            ? measurement.Judge.ToUpperInvariant()
            : Math.Abs(errorX) <= plan.ToleranceX && Math.Abs(errorY) <= plan.ToleranceY
                ? "OK"
                : "NG";
        var state = judge.Equals("OK", StringComparison.OrdinalIgnoreCase)
            ? EN_REVIEW_POINT_STATE.Ok
            : EN_REVIEW_POINT_STATE.Ng;

        return point with
        {
            ReviewTargetX = measurement.X,
            ReviewTargetY = measurement.Y,
            ErrorX = errorX,
            ErrorY = errorY,
            State = state,
            Judge = judge
        };
    }

    private static ST_REVIEW_MEASURE_RESULT ParseVisionResponse(
        string response,
        ST_REVIEW_PLAN_POINT point)
    {
        var values = SplitResponse(response);
        var hasX = TryReadDouble(values, "X", out var x) ||
            TryReadDouble(values, "REVIEW_X", out x) ||
            TryReadDouble(values, "MEASURE_X", out x);
        var hasY = TryReadDouble(values, "Y", out var y) ||
            TryReadDouble(values, "REVIEW_Y", out y) ||
            TryReadDouble(values, "MEASURE_Y", out y);
        var judge = values.TryGetValue("JUDGE", out var judgeValue)
            ? judgeValue
            : values.TryGetValue("RESULT", out var resultValue)
                ? resultValue
                : "";

        if (hasX && hasY)
        {
            return new ST_REVIEW_MEASURE_RESULT(x, y, judge, response);
        }

        var isSimulatedNg = point.PointNo % 11 == 0 || point.PointNo % 17 == 0;
        var simulatedX = point.ReviewTargetX + (isSimulatedNg ? 0.055 : 0.002);
        var simulatedY = point.ReviewTargetY + (isSimulatedNg ? -0.048 : -0.003);
        var simulatedJudge = string.IsNullOrWhiteSpace(judge) && isSimulatedNg
            ? "NG"
            : judge;

        return new ST_REVIEW_MEASURE_RESULT(simulatedX, simulatedY, simulatedJudge, response);
    }

    private static IReadOnlyDictionary<string, string> SplitResponse(string response)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tokens = response
            .Split([';', ',', '|', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == token.Length - 1)
            {
                continue;
            }

            values[token[..separatorIndex].Trim()] = token[(separatorIndex + 1)..].Trim();
        }

        return values;
    }

    private static bool TryReadDouble(
        IReadOnlyDictionary<string, string> values,
        string key,
        out double value)
    {
        value = 0.0;

        return values.TryGetValue(key, out var text) &&
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatCommand(
        string command,
        params (string Key, string Value)[] arguments)
    {
        return string.Join(
            ";",
            new[] { command }.Concat(arguments.Select(argument => $"{argument.Key}={argument.Value}")));
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.000000", CultureInfo.InvariantCulture);
    }

    private void SetCurrentPlan(
        ST_REVIEW_PLAN plan,
        Action<ST_REVIEW_PLAN>? progress)
    {
        lock (_stateLock)
        {
            _currentPlan = plan;
        }

        progress?.Invoke(plan);
    }

    private static ST_REVIEW_PLAN ResetPlanForRun(ST_REVIEW_PLAN plan)
    {
        var firstHoleKey = OrderByReviewSequence(plan.ReviewPoints)
            .Select(point => point.HoleKey)
            .FirstOrDefault();

        return plan with
        {
            Points = plan.Points.Select(point => point.Use
                ? point with
                {
                    ErrorX = 0.0,
                    ErrorY = 0.0,
                    State = point.HoleKey.Equals(firstHoleKey, StringComparison.OrdinalIgnoreCase)
                        ? EN_REVIEW_POINT_STATE.Current
                        : EN_REVIEW_POINT_STATE.Ready,
                    Judge = "WAIT"
                }
                : point with
                {
                    State = EN_REVIEW_POINT_STATE.Skip,
                    Judge = "-"
                }).ToArray()
        };
    }

    private static ST_REVIEW_PLAN SetWaitingPointsReady(ST_REVIEW_PLAN plan)
    {
        return plan with
        {
            Points = plan.Points.Select(point => point.State == EN_REVIEW_POINT_STATE.Current
                ? point with { State = EN_REVIEW_POINT_STATE.Ready }
                : point).ToArray()
        };
    }

    private static ST_REVIEW_PLAN UpdatePoint(
        ST_REVIEW_PLAN plan,
        ST_REVIEW_PLAN_POINT point)
    {
        return plan with
        {
            Points = plan.Points.Select(item => item.HoleKey.Equals(point.HoleKey, StringComparison.OrdinalIgnoreCase) ? point : item).ToArray()
        };
    }

    private static ST_REVIEW_SEQUENCE_STATUS CreateStatus(
        ST_REVIEW_PLAN plan,
        EN_REVIEW_SEQUENCE_STATE state,
        string message)
    {
        var reviewPoints = plan.ReviewPoints;
        var completedCount = reviewPoints.Count(point => point.State is EN_REVIEW_POINT_STATE.Ok or EN_REVIEW_POINT_STATE.Ng);
        var ngCount = reviewPoints.Count(point => point.State == EN_REVIEW_POINT_STATE.Ng);

        return new ST_REVIEW_SEQUENCE_STATUS(
            state,
            reviewPoints.Count,
            completedCount,
            ngCount,
            message);
    }

    private async Task<ST_REVIEW_SEQUENCE_STATUS> RunRetryPoints(
        IReadOnlyList<ST_REVIEW_PLAN_POINT> retryPoints,
        string completedMessage,
        Action<ST_REVIEW_PLAN>? progress,
        CancellationToken cancellationToken)
    {
        if (!await _sequenceLock.WaitAsync(0, cancellationToken))
        {
            return CreateStatus(
                CurrentPlan!,
                EN_REVIEW_SEQUENCE_STATE.Running,
                "Review sequence is already running.");
        }

        try
        {
            var workingPlan = CurrentPlan!;
            _stopRequested = false;
            SequenceState = EN_REVIEW_SEQUENCE_STATE.Running;

            foreach (var point in OrderByReviewSequence(retryPoints))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_stopRequested)
                {
                    SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
                    return CreateStatus(workingPlan, SequenceState, "Review retry stopped.");
                }

                var currentPoint = point with
                {
                    State = EN_REVIEW_POINT_STATE.Current,
                    Judge = "WAIT"
                };
                workingPlan = UpdatePoint(workingPlan, currentPoint);
                SetCurrentPlan(workingPlan, progress);

                await MoveStageY(currentPoint, cancellationToken);
                await MoveVisionX(currentPoint, cancellationToken);
                var measurement = await MeasureVision(currentPoint, cancellationToken);

                if (_stopRequested)
                {
                    SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
                    workingPlan = UpdatePoint(workingPlan, currentPoint with { State = EN_REVIEW_POINT_STATE.Ready });
                    SetCurrentPlan(workingPlan, progress);
                    return CreateStatus(workingPlan, SequenceState, "Review retry stopped.");
                }

                var measuredPoint = ApplyMeasurement(workingPlan, currentPoint, measurement);
                workingPlan = UpdatePoint(workingPlan, measuredPoint);
                SetCurrentPlan(workingPlan, progress);
            }

            SequenceState = EN_REVIEW_SEQUENCE_STATE.Completed;
            await SaveResult(workingPlan, workingPlan.ReviewPoints, cancellationToken);
            return CreateStatus(workingPlan, SequenceState, completedMessage);
        }
        catch (OperationCanceledException)
        {
            SequenceState = EN_REVIEW_SEQUENCE_STATE.Stopped;
            var stoppedPlan = SetWaitingPointsReady(CurrentPlan!);
            SetCurrentPlan(stoppedPlan, progress);
            return CreateStatus(stoppedPlan, SequenceState, "Review retry canceled.");
        }
        catch (Exception ex)
        {
            SequenceState = EN_REVIEW_SEQUENCE_STATE.Failed;
            var failedPlan = SetWaitingPointsReady(CurrentPlan!);
            SetCurrentPlan(failedPlan, progress);
            return CreateStatus(failedPlan, SequenceState, ex.Message);
        }
        finally
        {
            _sequenceLock.Release();
        }
    }

    private sealed record ST_REVIEW_MEASURE_RESULT(
        double X,
        double Y,
        string Judge,
        string RawResponse);

    private static IReadOnlyList<ST_REVIEW_PLAN_POINT> OrderByReviewSequence(
        IEnumerable<ST_REVIEW_PLAN_POINT> points)
    {
        var source = points
            .OrderBy(point => point.DesignY)
            .ThenBy(point => point.DesignX)
            .ThenBy(point => point.CellNo)
            .ThenBy(point => point.HoleNo)
            .ToArray();
        var rows = new List<List<ST_REVIEW_PLAN_POINT>>();

        foreach (var point in source)
        {
            if (rows.Count == 0 ||
                Math.Abs(point.DesignY - rows[^1][0].DesignY) > ReviewSequenceRowTolerance)
            {
                rows.Add(new List<ST_REVIEW_PLAN_POINT>());
            }

            rows[^1].Add(point);
        }

        var orderedPoints = new List<ST_REVIEW_PLAN_POINT>(source.Length);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var orderedRow = rowIndex % 2 == 0
                ? row
                    .OrderBy(point => point.DesignX)
                    .ThenBy(point => point.CellNo)
                    .ThenBy(point => point.HoleNo)
                : row
                    .OrderByDescending(point => point.DesignX)
                    .ThenBy(point => point.CellNo)
                    .ThenBy(point => point.HoleNo);

            orderedPoints.AddRange(orderedRow);
        }

        return orderedPoints;
    }

    private static int AssignHeadNo(
        double designX,
        int headCount,
        ST_REVIEW_HEAD_LAYOUT headLayout)
    {
        if (headCount <= 0)
        {
            return 0;
        }

        for (var headNo = 1; headNo <= headCount; headNo++)
        {
            var centerX = headLayout.H1PositionX + ((headNo - 1) * headLayout.HeadPitchX);
            var startX = centerX - headLayout.ScanFieldHalfX;
            var endX = centerX + headLayout.ScanFieldHalfX;
            if (designX >= startX && designX <= endX)
            {
                // Main preview and the MOF coordinate sample both check H1 -> H8.
                // In an overlapping Scan Field, the left Head therefore owns the Hole.
                return headNo;
            }
        }

        return 0;
    }

    private sealed record ST_REVIEW_HEAD_LAYOUT(
        double H1PositionX,
        double HeadPitchX,
        double ScanFieldHalfX);

    public static string ToHoleKey(int cellNo, int holeNo)
    {
        return $"CELL{Math.Max(1, cellNo):000}_HOLE{Math.Max(1, holeNo):0000}";
    }

    public static string NormalizeHoleKey(string value)
    {
        var text = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var holeIndex = text.IndexOf("_HOLE", StringComparison.OrdinalIgnoreCase);
        if (text.StartsWith("CELL", StringComparison.OrdinalIgnoreCase) && holeIndex > 4)
        {
            var cellDigits = new string(text[4..holeIndex].Where(char.IsDigit).ToArray());
            var holeDigits = new string(text[(holeIndex + 5)..].Where(char.IsDigit).ToArray());

            if (int.TryParse(cellDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cellNo) &&
                int.TryParse(holeDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var holeNo) &&
                cellNo > 0 &&
                holeNo > 0)
            {
                return ToHoleKey(cellNo, holeNo);
            }
        }

        return "";
    }

    private static int ReadInt(
        ST_RECIPE_DATA recipe,
        int defaultValue,
        params string[] keys)
    {
        var value = ReadText(recipe, keys);

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            ? (int)Math.Round(doubleValue)
            : defaultValue;
    }

    private static double ReadDouble(
        ST_RECIPE_DATA recipe,
        double defaultValue,
        params string[] keys)
    {
        var value = ReadText(recipe, keys);

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            ? doubleValue
            : defaultValue;
    }

    private static string ReadText(
        ST_RECIPE_DATA recipe,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            var parameter = recipe.Parameters.FirstOrDefault(item =>
                item.Key.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (parameter is not null && !string.IsNullOrWhiteSpace(parameter.Value))
            {
                return parameter.Value.Trim();
            }
        }

        return "";
    }
}
