using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Drilling.UI.Menu.Menus;

internal static class CCellPreviewDrawing
{
    public static void DrawAlignKeys(
        DrawingContext context,
        ST_GLASS_PREVIEW_FRAME frame,
        double glassWidth,
        double glassHeight,
        double marginX,
        double marginY)
    {
        var brush = new SolidColorBrush(Color.FromRgb(251, 191, 36));
        brush.Freeze();
        var typeface = new Typeface(
            new FontFamily("Segoe UI"),
            FontStyles.Normal,
            FontWeights.SemiBold,
            FontStretches.Normal);
        var crossPen = new Pen(brush, 1.6)
        {
            StartLineCap = PenLineCap.Square,
            EndLineCap = PenLineCap.Square
        };
        crossPen.Freeze();
        const double crossHalfLength = 4.0;
        const double labelGap = 6.0;
        var safeGlassWidth = Math.Max(1.0, glassWidth);
        var safeGlassHeight = Math.Max(1.0, glassHeight);
        var safeMarginX = Math.Clamp(marginX, 0.0, safeGlassWidth / 2.0);
        var safeMarginY = Math.Clamp(marginY, 0.0, safeGlassHeight / 2.0);
        var leftX = frame.CanvasLeft + (safeMarginX / safeGlassWidth * frame.Width);
        var rightX = frame.CanvasLeft + frame.Width - (safeMarginX / safeGlassWidth * frame.Width);
        var topY = frame.CanvasTop + (safeMarginY / safeGlassHeight * frame.Height);
        var bottomY = frame.CanvasTop + frame.Height - (safeMarginY / safeGlassHeight * frame.Height);

        DrawKey("AK1", leftX, topY, false, false);
        DrawKey("AK2", leftX, bottomY, false, true);
        DrawKey("AK3", rightX, topY, true, false);
        DrawKey("AK4", rightX, bottomY, true, true);
        return;

        void DrawKey(string name, double centerX, double centerY, bool alignRight, bool alignBelow)
        {
            DrawCross(centerX, centerY);
            DrawLabel(name, centerX, alignBelow ? centerY + 2.0 : centerY - 15.0, alignRight);
        }

        void DrawCross(double centerX, double centerY)
        {
            context.DrawLine(
                crossPen,
                new Point(centerX - crossHalfLength, centerY),
                new Point(centerX + crossHalfLength, centerY));
            context.DrawLine(
                crossPen,
                new Point(centerX, centerY - crossHalfLength),
                new Point(centerX, centerY + crossHalfLength));
        }

        void DrawLabel(string name, double dotX, double top, bool alignRight)
        {
            var text = new FormattedText(
                name,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                10.0,
                brush,
                1.0);
            var left = alignRight
                ? dotX - labelGap - text.Width
                : dotX + labelGap;
            context.DrawText(text, new Point(left, top));
        }
    }

    public static ST_CELL_PREVIEW_LABEL? CreateCellLabel(
        int cellNo,
        Rect cellBounds,
        double designWidth,
        double designHeight,
        bool isSelected = false)
    {
        if (cellBounds.IsEmpty)
        {
            return null;
        }

        var label = new FormattedText(
            $"Cell{cellNo}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            10.0,
            Brushes.White,
            1.0);
        var badgeWidth = Math.Max(34.0, label.Width + 10.0);
        var centerX = cellBounds.Left + (cellBounds.Width / 2.0);
        var centerY = cellBounds.Top + (cellBounds.Height / 2.0);

        return new ST_CELL_PREVIEW_LABEL(
            cellNo,
            centerX,
            centerY,
            badgeWidth,
            designWidth,
            designHeight,
            isSelected);
    }
}

public sealed record ST_CELL_PREVIEW_LABEL(
    int CellNo,
    double CanvasCenterX,
    double CanvasCenterY,
    double Width,
    double DesignWidth,
    double DesignHeight,
    bool IsSelected)
{
    public string DisplayText => $"Cell{CellNo}";

    public double Height => 16.0;
}
