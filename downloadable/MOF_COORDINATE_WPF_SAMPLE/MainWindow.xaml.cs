using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using MofCoordinateDemo.Models;
using MofCoordinateDemo.Services;

namespace MofCoordinateDemo;

public partial class MainWindow : Window
{
    private readonly CoordinateTransformService _service = new();
    private CoordinateInput _input = new();
    private CoordinateResult? _lastResult;
    private double _matrixCellSize = 86;

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

    private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Excel CSV Config",
            Filter = "Excel CSV (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ApplyConfigCsv(dialog.FileName);
            LoadInputToScreen(_input);
            GenerateAndRender();
        }
    }

    private void GenerateAndRender()
    {
        _lastResult = _service.Generate(_input);

        BuildMatrixGrid(DesignMatrixGrid, "Design");
        BuildMatrixGrid(ProcessMatrixGrid, "Process");
        BuildMatrixGrid(ReviewMatrixGrid, "Review");
        DoeGrid.ItemsSource = _lastResult.DoeBeams;

        var selected = _lastResult.Commands.FirstOrDefault(x => x.IsSelectedCell);
        var inFieldCount = _lastResult.Commands.Count(x => x.InField);
        SummaryText.Text =
            $"AK1 Stage Anchor = ({_lastResult.Ak1GlobalX:0.######}, {_lastResult.Ak1GlobalY:0.######}) mm   " +
            $"Cells = {_lastResult.Commands.Count}, In-field = {inFieldCount}   " +
            $"Review Basis = H{_lastResult.SelectedReviewScanner.Index} DOE{_lastResult.SelectedDoeBeam.BeamNo:00}";

        FormulaText.Text =
            "Click a board cell or scanner head to update the selected point/head. Mouse-wheel over a matrix grid to resize cells. " +
            "Rows are numbered and columns are alphabet letters like A1, B1, C1. Each matrix cell shows the point name and one (x, y) coordinate.";

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

        var maxLocalX = (_input.CellBlockColumns - 1) * _input.CellBlockPitchX
                        + _input.CellFirstX
                        + Math.Max(1, _input.CellColumns - 1) * _input.CellPitchX
                        + _input.PatternOffsetX;
        var maxLocalY = (_input.CellBlockRows - 1) * _input.CellBlockPitchY
                        + _input.CellFirstY
                        + Math.Max(1, _input.CellRows - 1) * _input.CellPitchY
                        + _input.PatternOffsetY;
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
                StrokeThickness = isSelected ? 2.2 : 1.0,
                Tag = command,
                Cursor = Cursors.Hand
            };
            rect.MouseLeftButtonDown += CellRect_MouseLeftButtonDown;
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            LayoutCanvas.Children.Add(rect);

            if (cellW > 34 && cellH > 24)
            {
                DrawText(command.MatrixPointName, x + 5, y + 4, Math.Min(13, Math.Max(8, cellH * 0.28)), FontWeights.SemiBold, Brushes.Black);
            }
        }

        foreach (var blockGroup in _lastResult.Commands.GroupBy(x => x.CellBlock))
        {
            var first = blockGroup.OrderBy(x => x.Row).ThenBy(x => x.Column).First();
            var x = boardLeft + 28 + first.LocalX * scale;
            var y = boardTop + 20 + first.LocalY * scale - 22;
            DrawText($"Cell#{first.CellBlock}", x, y, 13, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(23, 32, 51)));
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
                StrokeThickness = isReviewBasis ? 2.8 : 2,
                Tag = scanner,
                Cursor = Cursors.Hand
            };
            box.MouseLeftButtonDown += ScannerRect_MouseLeftButtonDown;
            Canvas.SetLeft(box, x);
            Canvas.SetTop(box, y);
            LayoutCanvas.Children.Add(box);

            DrawText($"Scanner\n#{scanner.Index}", x + 8, y + 17, 15, FontWeights.Normal, Brushes.Black);
        }
    }

    private void CellRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: CellCommand command })
        {
            _input.SelectedCellBlock = command.CellBlock;
            _input.SelectedCellColumn = command.Column;
            _input.SelectedCellRow = command.Row;
            LoadInputToScreen(_input);
            GenerateAndRender();
        }
    }

    private void ScannerRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ScannerModel scanner })
        {
            _input.ReviewBasisScannerHead = scanner.Index;
            _input.HighlightScannerHeads = scanner.Index.ToString(CultureInfo.InvariantCulture);
            LoadInputToScreen(_input);
            GenerateAndRender();
        }
    }

    private void MatrixGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _matrixCellSize = Math.Clamp(_matrixCellSize + (e.Delta > 0 ? 8 : -8), 48, 180);
        if (_lastResult is not null)
        {
            BuildMatrixGrid(DesignMatrixGrid, "Design");
            BuildMatrixGrid(ProcessMatrixGrid, "Process");
            BuildMatrixGrid(ReviewMatrixGrid, "Review");
        }

        e.Handled = true;
    }

    private void BuildMatrixGrid(DataGrid grid, string mode)
    {
        if (_lastResult is null)
        {
            return;
        }

        grid.Columns.Clear();
        grid.RowHeight = _matrixCellSize;
        grid.ColumnWidth = _matrixCellSize;

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Cell#",
            Binding = new Binding(nameof(MatrixRow.RowHeader)),
            Width = Math.Max(70, _matrixCellSize * 0.82)
        });

        var columns = Enumerable.Range(0, Math.Max(1, _input.CellColumns))
            .Select(ToColumnLetter)
            .ToList();

        foreach (var column in columns)
        {
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{column}]"));
            textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            textBlockFactory.SetValue(TextBlock.FontSizeProperty, Math.Clamp(_matrixCellSize / 6.2, 8, 18));

            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = column,
                Width = _matrixCellSize,
                CellTemplate = new DataTemplate { VisualTree = textBlockFactory }
            });
        }

        grid.ItemsSource = BuildMatrixRows(mode, columns);
    }

    private IReadOnlyList<MatrixRow> BuildMatrixRows(string mode, IReadOnlyList<string> columns)
    {
        if (_lastResult is null)
        {
            return Array.Empty<MatrixRow>();
        }

        var rows = new List<MatrixRow>();
        foreach (var blockGroup in _lastResult.Commands.GroupBy(x => x.CellBlock).OrderBy(x => x.Key))
        {
            var header = new MatrixRow { RowHeader = $"Cell#{blockGroup.Key}", IsGroupHeader = true };
            rows.Add(header);

            foreach (var rowGroup in blockGroup.GroupBy(x => x.Row).OrderBy(x => x.Key))
            {
                var matrixRow = new MatrixRow { RowHeader = (rowGroup.Key + 1).ToString(CultureInfo.InvariantCulture) };
                foreach (var command in rowGroup.OrderBy(x => x.Column))
                {
                    var column = ToColumnLetter(command.Column);
                    matrixRow[column] = FormatMatrixCell(command, mode);
                }

                rows.Add(matrixRow);
            }
        }

        return rows;
    }

    private static string FormatMatrixCell(CellCommand command, string mode)
    {
        var coordinate = mode switch
        {
            "Design" => command.DesignLocalMatrix,
            "Process" => command.ProcessGMatrix,
            "Review" => command.ReviewMatrix,
            _ => command.DesignLocalMatrix
        };

        var extra = mode switch
        {
            "Process" => $"{command.ScannerName}",
            "Review" => command.DoeSelection,
            _ => ""
        };

        return string.IsNullOrWhiteSpace(extra)
            ? $"{command.MatrixPointName}\n{coordinate}"
            : $"{command.MatrixPointName}\n{coordinate}\n{extra}";
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
        CellBlockColumnsBox.Text = input.CellBlockColumns.ToString(CultureInfo.InvariantCulture);
        CellBlockRowsBox.Text = input.CellBlockRows.ToString(CultureInfo.InvariantCulture);
        CellBlockPitchXBox.Text = Format(input.CellBlockPitchX);
        CellBlockPitchYBox.Text = Format(input.CellBlockPitchY);
        SelectedCellBlockBox.Text = input.SelectedCellBlock.ToString(CultureInfo.InvariantCulture);
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
        var blockColumns = ReadInt(CellBlockColumnsBox, _input.CellBlockColumns);
        var blockRows = ReadInt(CellBlockRowsBox, _input.CellBlockRows);
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
            CellBlockColumns = blockColumns,
            CellBlockRows = blockRows,
            CellBlockPitchX = ReadDouble(CellBlockPitchXBox, _input.CellBlockPitchX),
            CellBlockPitchY = ReadDouble(CellBlockPitchYBox, _input.CellBlockPitchY),
            SelectedCellBlock = Clamp(ReadInt(SelectedCellBlockBox, _input.SelectedCellBlock), 1, blockColumns * blockRows),
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

    private static string ToColumnLetter(int zeroBasedColumn)
    {
        var value = zeroBasedColumn + 1;
        var text = "";
        while (value > 0)
        {
            value--;
            text = (char)('A' + value % 26) + text;
            value /= 26;
        }

        return text;
    }

    private void ApplyConfigCsv(string path)
    {
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',', 2);
            if (parts.Length != 2 || parts[0].Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ApplyConfigValue(parts[0].Trim(), parts[1].Trim());
        }
    }

    private void ApplyConfigValue(string key, string value)
    {
        value = value.Trim().Trim('"');
        var number = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
        var integer = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : (int)Math.Round(number);

        switch (key)
        {
            case nameof(CoordinateInput.BoardSizeX): _input.BoardSizeX = number; break;
            case nameof(CoordinateInput.BoardSizeY): _input.BoardSizeY = number; break;
            case nameof(CoordinateInput.CellFirstX): _input.CellFirstX = number; break;
            case nameof(CoordinateInput.CellFirstY): _input.CellFirstY = number; break;
            case nameof(CoordinateInput.CellPitchX): _input.CellPitchX = number; break;
            case nameof(CoordinateInput.CellPitchY): _input.CellPitchY = number; break;
            case nameof(CoordinateInput.PatternOffsetX): _input.PatternOffsetX = number; break;
            case nameof(CoordinateInput.PatternOffsetY): _input.PatternOffsetY = number; break;
            case nameof(CoordinateInput.CellColumns): _input.CellColumns = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellRows): _input.CellRows = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellBlockColumns): _input.CellBlockColumns = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellBlockRows): _input.CellBlockRows = Math.Max(1, integer); break;
            case nameof(CoordinateInput.CellBlockPitchX): _input.CellBlockPitchX = number; break;
            case nameof(CoordinateInput.CellBlockPitchY): _input.CellBlockPitchY = number; break;
            case nameof(CoordinateInput.ScannerCount): _input.ScannerCount = Math.Max(1, integer); break;
            case nameof(CoordinateInput.HighlightScannerHeads): _input.HighlightScannerHeads = value; break;
            case nameof(CoordinateInput.ReviewBasisScannerHead): _input.ReviewBasisScannerHead = Math.Max(1, integer); break;
            case nameof(CoordinateInput.ReviewBasisDoeBeam): _input.ReviewBasisDoeBeam = Clamp(integer, 1, 16); break;
        }
    }

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
