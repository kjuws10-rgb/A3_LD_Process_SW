using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MofCoordinateDemo.Models;
using MofCoordinateDemo.Services;

namespace MofCoordinateDemo;

public partial class MainWindow : Window
{
    private readonly CoordinateTransformService _service = new();
    private CoordinateInput _input = new();
    private CoordinateResult? _lastResult;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            LoadInputToScreen(_input);
            GenerateAndRender();
        };
        SizeChanged += (_, _) => DrawLayout();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        _input = ReadInputFromScreen();
        GenerateAndRender();
    }

    private void GenerateAndRender()
    {
        _lastResult = _service.Generate(_input);

        DesignGrid.ItemsSource = _lastResult.Commands;
        ProcessGrid.ItemsSource = _lastResult.Commands;
        ReviewGrid.ItemsSource = _lastResult.Commands
            .OrderBy(x => x.ScannerIndex)
            .ThenBy(x => x.Row)
            .ThenBy(x => x.Column)
            .ToList();
        DoeGrid.ItemsSource = _lastResult.DoeBeams;

        var selected = _lastResult.Commands.FirstOrDefault(x => x.IsSelectedCell);
        var inFieldCount = _lastResult.Commands.Count(x => x.InField);
        SummaryText.Text =
            $"AK1 Stage Anchor = ({_lastResult.Ak1GlobalX:0.######}, {_lastResult.Ak1GlobalY:0.######}) mm   " +
            $"Cells = {_lastResult.Commands.Count}, In-field = {inFieldCount}   " +
            $"Review Basis = H{_lastResult.SelectedReviewScanner.Index} DOE{_lastResult.SelectedDoeBeam.BeamNo:00}";

        FormulaText.Text =
            "Design: Pstage = AK1 + R(theta) * Plocal.  " +
            "Process: Pprocess = Pstage + review offset, then GxGy = sign(head) * (Pprocess - scanner center).  " +
            "Review: every result is expressed from the selected head and DOE16 beam reference, so each row shows one 2D matrix value as (x, y).";

        if (selected is not null)
        {
            ProcessGrid.ScrollIntoView(selected);
            ReviewGrid.ScrollIntoView(selected);
        }

        DrawLayout();
    }

    private void DrawLayout()
    {
        if (_lastResult is null || LayoutCanvas.ActualWidth < 20 || LayoutCanvas.ActualHeight < 20)
        {
            return;
        }

        LayoutCanvas.Children.Clear();

        DrawTitle("Board cell selection and zigzag scanner layout", 20, 8, 21, FontWeights.Bold);

        var boardLeft = 24.0;
        var boardTop = 52.0;
        var boardWidth = LayoutCanvas.ActualWidth - 48.0;
        var boardHeight = Math.Max(220.0, LayoutCanvas.ActualHeight - 180.0);

        DrawBoardFrame(boardLeft, boardTop, boardWidth, boardHeight);
        DrawCellBlocks(boardLeft, boardTop, boardWidth, boardHeight);
        DrawScannerHeads(boardLeft, boardTop + boardHeight + 22.0, boardWidth);
        DrawLegend(boardLeft, LayoutCanvas.ActualHeight - 32.0);
    }

    private void DrawBoardFrame(double left, double top, double width, double height)
    {
        var frame = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.FromRgb(183, 160, 37)),
            StrokeThickness = 1.2
        };
        Canvas.SetLeft(frame, left);
        Canvas.SetTop(frame, top);
        LayoutCanvas.Children.Add(frame);

        DrawAlignKey(left + 10, top + 10, "AK1");
        DrawAlignKey(left + 10, top + height - 18, "AK2");
        DrawAlignKey(left + width - 18, top + 10, "AK3");
        DrawAlignKey(left + width - 18, top + height - 18, "AK4");
    }

    private void DrawCellBlocks(double boardLeft, double boardTop, double boardWidth, double boardHeight)
    {
        if (_lastResult is null)
        {
            return;
        }

        var maxLocalX = _input.CellFirstX + Math.Max(1, _input.CellColumns - 1) * _input.CellPitchX + _input.PatternOffsetX;
        var maxLocalY = _input.CellFirstY + Math.Max(1, _input.CellRows - 1) * _input.CellPitchY + _input.PatternOffsetY;
        var scaleX = (boardWidth - 60) / Math.Max(maxLocalX + _input.CellPitchX, 1);
        var scaleY = (boardHeight - 42) / Math.Max(maxLocalY + _input.CellPitchY, 1);
        var scale = Math.Min(scaleX, scaleY);
        var cellW = Math.Max(8, _input.CellPitchX * scale * 0.72);
        var cellH = Math.Max(8, _input.CellPitchY * scale * 0.72);

        foreach (var command in _lastResult.Commands)
        {
            var x = boardLeft + 28 + command.LocalX * scale;
            var y = boardTop + 20 + command.LocalY * scale;
            var isSelected = command.IsSelectedCell;
            var isHeadSelected = command.IsHighlightedScanner;

            var fill = Brushes.White;
            if (isHeadSelected)
            {
                fill = new SolidColorBrush(Color.FromRgb(247, 180, 132));
            }
            if (isSelected)
            {
                fill = new SolidColorBrush(Color.FromRgb(255, 168, 104));
            }

            var stroke = command.ScannerIndex % 2 == 1
                ? new SolidColorBrush(Color.FromRgb(178, 158, 47))
                : new SolidColorBrush(Color.FromRgb(93, 143, 169));

            var rect = new Rectangle
            {
                Width = cellW,
                Height = cellH,
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = isSelected ? 2.2 : 1.0
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            LayoutCanvas.Children.Add(rect);
        }
    }

    private void DrawScannerHeads(double left, double top, double width)
    {
        if (_lastResult is null)
        {
            return;
        }

        var count = _lastResult.Scanners.Count;
        var gap = width / Math.Max(1, count);
        var boxWidth = Math.Min(86, Math.Max(54, gap * 0.72));
        var boxHeight = 64.0;

        foreach (var scanner in _lastResult.Scanners)
        {
            var indexZero = scanner.Index - 1;
            var x = left + gap * indexZero + gap * 0.5 - boxWidth * 0.5;
            var y = top + (scanner.Index % 2 == 0 ? 78 : 0);
            var isReviewBasis = scanner.Index == _input.ReviewBasisScannerHead;
            var fill = scanner.IsHighlighted || isReviewBasis
                ? new SolidColorBrush(Color.FromRgb(144, 211, 78))
                : Brushes.White;

            var box = new Rectangle
            {
                Width = boxWidth,
                Height = boxHeight,
                Fill = fill,
                Stroke = isReviewBasis
                    ? new SolidColorBrush(Color.FromRgb(30, 91, 190))
                    : Brushes.Black,
                StrokeThickness = isReviewBasis ? 2.8 : 2
            };
            Canvas.SetLeft(box, x);
            Canvas.SetTop(box, y);
            LayoutCanvas.Children.Add(box);

            DrawText($"Scanner\n#{scanner.Index}", x + 8, y + 17, 15, FontWeights.Normal, Brushes.Black);
        }
    }

    private void DrawLegend(double left, double top)
    {
        DrawLegendBox(left, top, Color.FromRgb(255, 168, 104), "Selected cell");
        DrawLegendBox(left + 150, top, Color.FromRgb(247, 180, 132), "Cells handled by highlighted heads");
        DrawLegendBox(left + 425, top, Color.FromRgb(144, 211, 78), "Highlighted / review head");
    }

    private void DrawLegendBox(double x, double y, Color color, string text)
    {
        var box = new Rectangle
        {
            Width = 18,
            Height = 18,
            Fill = new SolidColorBrush(color),
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(box, x);
        Canvas.SetTop(box, y);
        LayoutCanvas.Children.Add(box);
        DrawText(text, x + 25, y - 1, 13, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(37, 54, 77)));
    }

    private void DrawAlignKey(double x, double y, string text)
    {
        var ak = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(Color.FromRgb(255, 204, 77)),
            Stroke = new SolidColorBrush(Color.FromRgb(117, 85, 0)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(ak, x);
        Canvas.SetTop(ak, y);
        LayoutCanvas.Children.Add(ak);
        DrawText(text, x + 13, y - 4, 12, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(80, 58, 0)));
    }

    private void DrawTitle(string text, double x, double y, double size, FontWeight weight)
    {
        DrawText(text, x, y, size, weight, new SolidColorBrush(Color.FromRgb(23, 32, 51)));
    }

    private void DrawText(string text, double x, double y, double size, FontWeight weight, Brush brush)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = brush,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        LayoutCanvas.Children.Add(block);
    }

    private void LoadInputToScreen(CoordinateInput input)
    {
        BoardXBox.Text = Format(input.BoardSizeX);
        BoardYBox.Text = Format(input.BoardSizeY);
        AkMarginXBox.Text = Format(input.AlignMarginX);
        AkMarginYBox.Text = Format(input.AlignMarginY);
        ReviewCenterXBox.Text = Format(input.ReviewCenterGlobalX);
        ReviewCenterYBox.Text = Format(input.ReviewCenterGlobalY);
        U0Box.Text = Format(input.ReviewPixelCenterU);
        V0Box.Text = Format(input.ReviewPixelCenterV);
        ScaleXBox.Text = Format(input.PixelScaleX);
        ScaleYBox.Text = Format(input.PixelScaleY);
        Ak1UBox.Text = Format(input.MeasuredAk1U);
        Ak1VBox.Text = Format(input.MeasuredAk1V);
        ThetaBox.Text = Format(input.ThetaAlignDeg);
        CellFirstXBox.Text = Format(input.CellFirstX);
        CellFirstYBox.Text = Format(input.CellFirstY);
        CellPitchXBox.Text = Format(input.CellPitchX);
        CellPitchYBox.Text = Format(input.CellPitchY);
        PatternOffsetXBox.Text = Format(input.PatternOffsetX);
        PatternOffsetYBox.Text = Format(input.PatternOffsetY);
        CellColumnsBox.Text = input.CellColumns.ToString(CultureInfo.InvariantCulture);
        CellRowsBox.Text = input.CellRows.ToString(CultureInfo.InvariantCulture);
        SelectedCellColumnBox.Text = input.SelectedCellColumn.ToString(CultureInfo.InvariantCulture);
        SelectedCellRowBox.Text = input.SelectedCellRow.ToString(CultureInfo.InvariantCulture);
        ScannerCountBox.Text = input.ScannerCount.ToString(CultureInfo.InvariantCulture);
        HighlightHeadsBox.Text = input.HighlightScannerHeads;
        FirstScannerXBox.Text = Format(input.FirstScannerCenterX);
        FirstScannerYBox.Text = Format(input.FirstScannerCenterY);
        ScannerPitchXBox.Text = Format(input.ScannerPitchX);
        EvenYOffsetBox.Text = Format(input.EvenScannerYOffset);
        FieldHalfXBox.Text = Format(input.ScannerFieldHalfX);
        FieldHalfYBox.Text = Format(input.ScannerFieldHalfY);
        ReviewBasisHeadBox.Text = input.ReviewBasisScannerHead.ToString(CultureInfo.InvariantCulture);
        ReviewBasisBeamBox.Text = input.ReviewBasisDoeBeam.ToString(CultureInfo.InvariantCulture);
        DoePitchXBox.Text = Format(input.DoeBeamPitchX);
        DoePitchYBox.Text = Format(input.DoeBeamPitchY);
        OffsetXBox.Text = Format(input.ProcessOffsetGlobalX);
        OffsetYBox.Text = Format(input.ProcessOffsetGlobalY);
    }

    private CoordinateInput ReadInputFromScreen()
    {
        var columns = ReadInt(CellColumnsBox, _input.CellColumns);
        var rows = ReadInt(CellRowsBox, _input.CellRows);
        var scannerCount = ReadInt(ScannerCountBox, _input.ScannerCount);

        return new CoordinateInput
        {
            BoardSizeX = ReadDouble(BoardXBox, _input.BoardSizeX),
            BoardSizeY = ReadDouble(BoardYBox, _input.BoardSizeY),
            AlignMarginX = ReadDouble(AkMarginXBox, _input.AlignMarginX),
            AlignMarginY = ReadDouble(AkMarginYBox, _input.AlignMarginY),
            ReviewCenterGlobalX = ReadDouble(ReviewCenterXBox, _input.ReviewCenterGlobalX),
            ReviewCenterGlobalY = ReadDouble(ReviewCenterYBox, _input.ReviewCenterGlobalY),
            ReviewPixelCenterU = ReadDouble(U0Box, _input.ReviewPixelCenterU),
            ReviewPixelCenterV = ReadDouble(V0Box, _input.ReviewPixelCenterV),
            PixelScaleX = ReadDouble(ScaleXBox, _input.PixelScaleX),
            PixelScaleY = ReadDouble(ScaleYBox, _input.PixelScaleY),
            MeasuredAk1U = ReadDouble(Ak1UBox, _input.MeasuredAk1U),
            MeasuredAk1V = ReadDouble(Ak1VBox, _input.MeasuredAk1V),
            ThetaAlignDeg = ReadDouble(ThetaBox, _input.ThetaAlignDeg),
            CellFirstX = ReadDouble(CellFirstXBox, _input.CellFirstX),
            CellFirstY = ReadDouble(CellFirstYBox, _input.CellFirstY),
            CellPitchX = ReadDouble(CellPitchXBox, _input.CellPitchX),
            CellPitchY = ReadDouble(CellPitchYBox, _input.CellPitchY),
            PatternOffsetX = ReadDouble(PatternOffsetXBox, _input.PatternOffsetX),
            PatternOffsetY = ReadDouble(PatternOffsetYBox, _input.PatternOffsetY),
            CellColumns = columns,
            CellRows = rows,
            SelectedCellColumn = Clamp(ReadZeroBasedInt(SelectedCellColumnBox, _input.SelectedCellColumn), 0, Math.Max(0, columns - 1)),
            SelectedCellRow = Clamp(ReadZeroBasedInt(SelectedCellRowBox, _input.SelectedCellRow), 0, Math.Max(0, rows - 1)),
            ScannerCount = scannerCount,
            HighlightScannerHeads = HighlightHeadsBox.Text,
            FirstScannerCenterX = ReadDouble(FirstScannerXBox, _input.FirstScannerCenterX),
            FirstScannerCenterY = ReadDouble(FirstScannerYBox, _input.FirstScannerCenterY),
            ScannerPitchX = ReadDouble(ScannerPitchXBox, _input.ScannerPitchX),
            EvenScannerYOffset = ReadDouble(EvenYOffsetBox, _input.EvenScannerYOffset),
            ScannerFieldHalfX = ReadDouble(FieldHalfXBox, _input.ScannerFieldHalfX),
            ScannerFieldHalfY = ReadDouble(FieldHalfYBox, _input.ScannerFieldHalfY),
            ReviewBasisScannerHead = Clamp(ReadInt(ReviewBasisHeadBox, _input.ReviewBasisScannerHead), 1, scannerCount),
            ReviewBasisDoeBeam = Clamp(ReadInt(ReviewBasisBeamBox, _input.ReviewBasisDoeBeam), 1, 16),
            DoeBeamPitchX = ReadDouble(DoePitchXBox, _input.DoeBeamPitchX),
            DoeBeamPitchY = ReadDouble(DoePitchYBox, _input.DoeBeamPitchY),
            ProcessOffsetGlobalX = ReadDouble(OffsetXBox, _input.ProcessOffsetGlobalX),
            ProcessOffsetGlobalY = ReadDouble(OffsetYBox, _input.ProcessOffsetGlobalY)
        };
    }

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    private static double ReadDouble(TextBox textBox, double fallback)
    {
        if (double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        textBox.Text = Format(fallback);
        return fallback;
    }

    private static int ReadInt(TextBox textBox, int fallback)
    {
        if (int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Math.Max(1, value);
        }

        textBox.Text = fallback.ToString(CultureInfo.InvariantCulture);
        return fallback;
    }

    private static int ReadZeroBasedInt(TextBox textBox, int fallback)
    {
        if (int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Math.Max(0, value);
        }

        textBox.Text = fallback.ToString(CultureInfo.InvariantCulture);
        return fallback;
    }
}
