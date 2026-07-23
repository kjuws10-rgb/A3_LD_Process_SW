using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Drilling.Common.Managers;
using Drilling.Common.Recipe;
using Drilling.Common.Review;

namespace Drilling.UI.Menu.Menus;

internal static class CReviewGlassPreviewBuilder
{
    private const double CanvasWidth = 860.0;
    private const double CanvasHeight = 430.0;
    private const double FrameLeft = 44.0;
    private const double FrameTop = 50.0;
    private const double FrameMaxWidth = 772.0;
    private const double FrameMaxHeight = 340.0;

    public static ST_REVIEW_GLASS_PREVIEW Build(
        ST_RECIPE_DATA recipe,
        int defaultCellCount,
        int currentCellNo = 0,
        int currentHoleNo = 0,
        IReadOnlyList<ST_REVIEW_PLAN_POINT>? reviewPoints = null,
        bool useSampleSelectionColors = false,
        IReadOnlySet<int>? visibleCellNos = null)
    {
        var glassWidth = ReadDouble(recipe, 500.0, "GLASS_SIZE_X", "GLASS_WIDTH");
        var glassHeight = ReadDouble(recipe, 300.0, "GLASS_SIZE_Y", "GLASS_HEIGHT");
        var akMarginX = ReadDouble(recipe, 55.0, "AK_MARGIN_X", "ALIGN_MARGIN_X");
        var akMarginY = ReadDouble(recipe, 45.0, "AK_MARGIN_Y", "ALIGN_MARGIN_Y");
        var cellCount = Math.Clamp(
            ReadInt(recipe, Math.Max(1, defaultCellCount), "CELL_COUNT", "MAX_CELL_NUMBER"),
            1,
            1000);
        var holeStates = (reviewPoints ?? [])
            .GroupBy(point => (point.CellNo, point.HoleNo))
            .ToDictionary(group => group.Key, group => group.Last().State);
        var displayedCellNos = visibleCellNos is null
            ? Enumerable.Range(1, cellCount).ToArray()
            : visibleCellNos
                .Where(cellNo => cellNo >= 1 && cellNo <= cellCount)
                .Distinct()
                .OrderBy(cellNo => cellNo)
                .ToArray();

        if (glassWidth <= 0.0 || glassHeight <= 0.0)
        {
            return new ST_REVIEW_GLASS_PREVIEW(
                null,
                [],
                null,
                $"{displayedCellNos.Length} Cells / Glass size is invalid");
        }

        var scale = Math.Min(FrameMaxWidth / glassWidth, FrameMaxHeight / glassHeight);
        var frameWidth = glassWidth * scale;
        var frameHeight = glassHeight * scale;
        var frameLeft = FrameLeft + ((FrameMaxWidth - frameWidth) / 2.0);
        var frameTop = FrameTop + ((FrameMaxHeight - frameHeight) / 2.0);
        var frame = new ST_GLASS_PREVIEW_FRAME(frameLeft, frameTop, frameWidth, frameHeight);
        var drawing = new DrawingGroup();
        var labels = new List<ST_CELL_PREVIEW_LABEL>();
        ST_REVIEW_CURRENT_HOLE_MARKER? currentHoleMarker = null;
        long totalHoleCount = 0;

        using (var context = drawing.Open())
        {
            context.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                null,
                new Rect(0.0, 0.0, CanvasWidth, CanvasHeight));

            foreach (var cellNo in displayedCellNos)
            {
                var prefix = $"CELL{cellNo}";
                var paddedPrefix = $"CELL{cellNo:00}";
                var countX = ReadInt(
                    recipe,
                    ReadInt(recipe, 1, "NUM_OF_PIXEL_X", "PIXEL_COUNT_X"),
                    $"{prefix}_NUM_OF_PIXEL_X",
                    $"{paddedPrefix}_NUM_OF_PIXEL_X",
                    $"{prefix}_PIXEL_COUNT_X",
                    $"{paddedPrefix}_PIXEL_COUNT_X");
                var countY = ReadInt(
                    recipe,
                    ReadInt(recipe, 1, "NUM_OF_PIXEL_Y", "PIXEL_COUNT_Y"),
                    $"{prefix}_NUM_OF_PIXEL_Y",
                    $"{paddedPrefix}_NUM_OF_PIXEL_Y",
                    $"{prefix}_PIXEL_COUNT_Y",
                    $"{paddedPrefix}_PIXEL_COUNT_Y");
                var globalPitchX = ReadDouble(recipe, 0.0, "PITCH_X", "PITCH");
                var globalPitchY = ReadDouble(recipe, globalPitchX, "PITCH_Y", "PITCH");
                var pitchX = ReadDouble(
                    recipe,
                    globalPitchX,
                    $"{prefix}_PITCH_X",
                    $"{paddedPrefix}_PITCH_X",
                    $"{prefix}_PITCH",
                    $"{paddedPrefix}_PITCH");
                var pitchY = ReadDouble(
                    recipe,
                    globalPitchY,
                    $"{prefix}_PITCH_Y",
                    $"{paddedPrefix}_PITCH_Y",
                    $"{prefix}_PITCH",
                    $"{paddedPrefix}_PITCH");
                var firstX = ReadDouble(
                    recipe,
                    0.0,
                    $"{prefix}_ALIGN_TO_1ST_PIXEL_X",
                    $"{paddedPrefix}_ALIGN_TO_1ST_PIXEL_X");
                var firstY = ReadDouble(
                    recipe,
                    0.0,
                    $"{prefix}_ALIGN_TO_1ST_PIXEL_Y",
                    $"{paddedPrefix}_ALIGN_TO_1ST_PIXEL_Y");
                var rotation = ReadDouble(
                    recipe,
                    0.0,
                    $"{prefix}_ROTATION",
                    $"{paddedPrefix}_ROTATION");
                var holeSize = Math.Max(
                    0.0,
                    ReadDouble(
                        recipe,
                        ReadDouble(recipe, 0.0, "PIXEL_SIZE", "HOLE_SIZE"),
                        $"{prefix}_PIXEL_SIZE",
                        $"{paddedPrefix}_PIXEL_SIZE",
                        $"{prefix}_HOLE_SIZE",
                        $"{paddedPrefix}_HOLE_SIZE"));
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

                totalHoleCount += result.Points.Count;
                var holeRadius = holeSize / 2.0;
                var previewHoleSize = Math.Clamp(holeSize * scale, 1.5, 12.0);
                var cellPixels = new HashSet<long>();
                var holeVisuals = new List<(double X, double Y, double Size, EN_REVIEW_POINT_STATE State)>();
                var minCanvasX = double.MaxValue;
                var minCanvasY = double.MaxValue;
                var maxCanvasX = double.MinValue;
                var maxCanvasY = double.MinValue;

                foreach (var point in result.Points)
                {
                    var canvasX = frameLeft + (point.X * scale);
                    var canvasY = frameTop + (point.Y * scale);
                    if (cellNo == currentCellNo && point.PointNo == currentHoleNo)
                    {
                        currentHoleMarker = new ST_REVIEW_CURRENT_HOLE_MARKER(
                            canvasX,
                            canvasY,
                            Math.Max(8.0, previewHoleSize + 5.0),
                            CanvasWidth,
                            CanvasHeight);
                    }

                    minCanvasX = Math.Min(minCanvasX, canvasX);
                    minCanvasY = Math.Min(minCanvasY, canvasY);
                    maxCanvasX = Math.Max(maxCanvasX, canvasX);
                    maxCanvasY = Math.Max(maxCanvasY, canvasY);

                    var pixelX = (int)Math.Round(canvasX);
                    var pixelY = (int)Math.Round(canvasY);
                    var pixelKey = ((long)pixelX << 32) | (uint)pixelY;
                    var isInside = point.X - holeRadius >= 0.0 &&
                        point.X + holeRadius <= glassWidth &&
                        point.Y - holeRadius >= 0.0 &&
                        point.Y + holeRadius <= glassHeight;
                    if (cellPixels.Add(pixelKey))
                    {
                        var state = holeStates.GetValueOrDefault(
                            (cellNo, point.PointNo),
                            EN_REVIEW_POINT_STATE.Ready);
                        holeVisuals.Add((
                            pixelX,
                            pixelY,
                            isInside ? previewHoleSize : 4.0,
                            state));
                    }
                }

                foreach (var stateGroup in holeVisuals.GroupBy(hole => hole.State))
                {
                    var geometry = new StreamGeometry();
                    using (var geometryContext = geometry.Open())
                    {
                        foreach (var hole in stateGroup)
                        {
                            AddCircle(geometryContext, hole.X, hole.Y, hole.Size);
                        }
                    }

                    geometry.Freeze();
                    context.DrawGeometry(
                        useSampleSelectionColors
                            ? CReviewStatusBrush.ForSampleSelection(stateGroup.Key)
                            : CReviewStatusBrush.ForPreviewBaseState(stateGroup.Key),
                        null,
                        geometry);
                }

                if (minCanvasX <= maxCanvasX && minCanvasY <= maxCanvasY)
                {
                    var labelPadding = Math.Max(4.0, previewHoleSize / 2.0);
                    var labelBounds = new Rect(
                        minCanvasX - labelPadding,
                        minCanvasY - labelPadding,
                        Math.Max(1.0, maxCanvasX - minCanvasX + (labelPadding * 2.0)),
                        Math.Max(1.0, maxCanvasY - minCanvasY + (labelPadding * 2.0)));
                    var label = CCellPreviewDrawing.CreateCellLabel(
                        cellNo,
                        labelBounds,
                        CanvasWidth,
                        CanvasHeight,
                        cellNo == currentCellNo);
                    if (label is not null)
                    {
                        labels.Add(label);
                    }
                }
            }

            CCellPreviewDrawing.DrawAlignKeys(
                context,
                frame,
                glassWidth,
                glassHeight,
                akMarginX,
                akMarginY);
        }

        drawing.Freeze();
        var paddingX = frameWidth * 0.03;
        var paddingY = Math.Max(22.0, frameHeight * 0.03);
        var previewRect = new Rect(
            0.0,
            0.0,
            frameWidth + (paddingX * 2.0),
            frameHeight + (paddingY * 2.0));
        var glassRect = new Rect(paddingX, paddingY, frameWidth, frameHeight);
        var translatedLabels = labels
            .Select(label => label with
            {
                CanvasCenterX = label.CanvasCenterX - frameLeft + paddingX,
                CanvasCenterY = label.CanvasCenterY - frameTop + paddingY,
                DesignWidth = previewRect.Width,
                DesignHeight = previewRect.Height
            })
            .ToArray();
        var translatedCurrentHoleMarker = currentHoleMarker is null
            ? null
            : currentHoleMarker with
            {
                CanvasCenterX = currentHoleMarker.CanvasCenterX - frameLeft + paddingX,
                CanvasCenterY = currentHoleMarker.CanvasCenterY - frameTop + paddingY,
                DesignWidth = previewRect.Width,
                DesignHeight = previewRect.Height
            };
        var previewDrawing = new DrawingGroup();

        using (var context = previewDrawing.Open())
        {
            context.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                null,
                previewRect);
            var glassPen = new Pen(new SolidColorBrush(Color.FromRgb(102, 136, 164)), 1.8);
            glassPen.Freeze();
            context.DrawRectangle(null, glassPen, glassRect);
            context.PushClip(new RectangleGeometry(previewRect));
            context.PushTransform(new TranslateTransform(
                -(frameLeft - paddingX),
                -(frameTop - paddingY)));
            context.DrawDrawing(drawing);
            context.Pop();
            context.Pop();
        }

        previewDrawing.Freeze();
        var previewImage = new DrawingImage(previewDrawing);
        previewImage.Freeze();

        return new ST_REVIEW_GLASS_PREVIEW(
            previewImage,
            translatedLabels,
            translatedCurrentHoleMarker,
            $"{displayedCellNos.Length} Cells / {totalHoleCount:N0} Holes / Glass {glassWidth:0.#} x {glassHeight:0.#} mm");
    }

    private static void AddCircle(
        StreamGeometryContext context,
        double x,
        double y,
        double size)
    {
        var radius = size / 2.0;
        var control = radius * 0.5522847498;
        context.BeginFigure(new Point(x + radius, y), true, true);
        context.BezierTo(
            new Point(x + radius, y + control),
            new Point(x + control, y + radius),
            new Point(x, y + radius),
            true,
            false);
        context.BezierTo(
            new Point(x - control, y + radius),
            new Point(x - radius, y + control),
            new Point(x - radius, y),
            true,
            false);
        context.BezierTo(
            new Point(x - radius, y - control),
            new Point(x - control, y - radius),
            new Point(x, y - radius),
            true,
            false);
        context.BezierTo(
            new Point(x + control, y - radius),
            new Point(x + radius, y - control),
            new Point(x + radius, y),
            true,
            false);
    }

    private static int ReadInt(
        ST_RECIPE_DATA recipe,
        int defaultValue,
        params string[] keys)
    {
        var text = ReadText(recipe, keys);
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            ? (int)Math.Round(doubleValue)
            : defaultValue;
    }

    private static double ReadDouble(
        ST_RECIPE_DATA recipe,
        double defaultValue,
        params string[] keys)
    {
        var text = ReadText(recipe, keys);

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
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

public sealed record ST_REVIEW_GLASS_PREVIEW(
    ImageSource? Image,
    IReadOnlyList<ST_CELL_PREVIEW_LABEL> CellLabels,
    ST_REVIEW_CURRENT_HOLE_MARKER? CurrentHoleMarker,
    string Summary);

public sealed record ST_REVIEW_CURRENT_HOLE_MARKER(
    double CanvasCenterX,
    double CanvasCenterY,
    double Width,
    double DesignWidth,
    double DesignHeight)
{
    public double Height => Width;
}
