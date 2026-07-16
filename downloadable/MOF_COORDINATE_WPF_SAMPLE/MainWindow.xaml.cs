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
        CommandGrid.ItemsSource = _lastResult.Commands;

        var inFieldCount = _lastResult.Commands.Count(x => x.InField);
        SummaryText.Text =
            $"AK1 Stage Anchor = ({_lastResult.Ak1GlobalX:F6}, {_lastResult.Ak1GlobalY:F6}) mm, " +
            $"Cell Commands = {_lastResult.Commands.Count}, In-field = {inFieldCount}, " +
            $"Scanner Count = {_lastResult.Scanners.Count}";

        DrawLayout();
    }

    private void DrawLayout()
    {
        if (_lastResult is null || LayoutCanvas.ActualWidth < 10 || LayoutCanvas.ActualHeight < 10)
        {
            return;
        }

        LayoutCanvas.Children.Clear();

        var margin = 28.0;
        var scaleX = (LayoutCanvas.ActualWidth - margin * 2) / Math.Max(1, _input.BoardSizeX);
        var scaleY = (LayoutCanvas.ActualHeight - margin * 2) / Math.Max(1, _input.BoardSizeY);
        var scale = Math.Min(scaleX, scaleY);

        double ToCanvasX(double stageX)
        {
            var localX = stageX - _lastResult.Ak1GlobalX;
            return margin + localX * scale;
        }

        double ToCanvasY(double stageY)
        {
            var localY = stageY - _lastResult.Ak1GlobalY;
            return margin + localY * scale;
        }

        var board = new Rectangle
        {
            Width = _input.BoardSizeX * scale,
            Height = _input.BoardSizeY * scale,
            Fill = new SolidColorBrush(Color.FromRgb(248, 251, 255)),
            Stroke = new SolidColorBrush(Color.FromRgb(45, 59, 79)),
            StrokeThickness = 2
        };
        Canvas.SetLeft(board, margin);
        Canvas.SetTop(board, margin);
        LayoutCanvas.Children.Add(board);

        DrawAlignKey(margin, margin, "AK1");
        DrawAlignKey(margin, margin + (_input.BoardSizeY - _input.AlignMarginY * 2) * scale, "AK2");
        DrawAlignKey(margin + (_input.BoardSizeX - _input.AlignMarginX * 2) * scale, margin, "AK3");
        DrawAlignKey(margin + (_input.BoardSizeX - _input.AlignMarginX * 2) * scale, margin + (_input.BoardSizeY - _input.AlignMarginY * 2) * scale, "AK4");

        foreach (var command in _lastResult.Commands.Where((_, index) => index % 2 == 0))
        {
            var x = ToCanvasX(command.StageX);
            var y = ToCanvasY(command.StageY);
            var dot = new Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = command.InField
                    ? new SolidColorBrush(Color.FromRgb(42, 127, 191))
                    : new SolidColorBrush(Color.FromRgb(180, 190, 202))
            };
            Canvas.SetLeft(dot, x - 2);
            Canvas.SetTop(dot, y - 2);
            LayoutCanvas.Children.Add(dot);
        }

        foreach (var scanner in _lastResult.Scanners)
        {
            var x = ToCanvasX(scanner.CenterX);
            var y = ToCanvasY(scanner.CenterY);
            var width = scanner.FieldHalfX * 2 * scale;
            var height = scanner.FieldHalfY * 2 * scale;
            var isOdd = scanner.MountType == "Odd";

            var field = new Rectangle
            {
                Width = Math.Max(16, width),
                Height = Math.Max(16, height),
                Fill = new SolidColorBrush(isOdd ? Color.FromArgb(80, 140, 205, 125) : Color.FromArgb(80, 245, 180, 95)),
                Stroke = new SolidColorBrush(isOdd ? Color.FromRgb(64, 130, 62) : Color.FromRgb(165, 92, 0)),
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(field, x - field.Width / 2);
            Canvas.SetTop(field, y - field.Height / 2);
            LayoutCanvas.Children.Add(field);

            var label = new TextBlock
            {
                Text = $"{scanner.Name} {scanner.MountType}",
                Foreground = new SolidColorBrush(Color.FromRgb(23, 32, 51)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            Canvas.SetLeft(label, x - 24);
            Canvas.SetTop(label, y - 8);
            LayoutCanvas.Children.Add(label);
        }

        var example = _lastResult.Commands.FirstOrDefault(x => x.Column == 14 && x.Row == 9)
                      ?? _lastResult.Commands.FirstOrDefault();
        if (example is not null)
        {
            var x = ToCanvasX(example.StageX);
            var y = ToCanvasY(example.StageY);
            var target = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(214, 0, 75)),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(target, x - 6);
            Canvas.SetTop(target, y - 6);
            LayoutCanvas.Children.Add(target);

            var targetLabel = new TextBlock
            {
                Text = $"Cell({example.Column},{example.Row}) {example.ScannerName} G=({example.Gx:F3},{example.Gy:F3})",
                Foreground = new SolidColorBrush(Color.FromRgb(214, 0, 75)),
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(targetLabel, x + 8);
            Canvas.SetTop(targetLabel, y - 18);
            LayoutCanvas.Children.Add(targetLabel);
        }
    }

    private void DrawAlignKey(double x, double y, string text)
    {
        var ak = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Color.FromRgb(255, 204, 77)),
            Stroke = new SolidColorBrush(Color.FromRgb(117, 85, 0)),
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(ak, x - 6);
        Canvas.SetTop(ak, y - 6);
        LayoutCanvas.Children.Add(ak);

        var label = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 58, 0))
        };
        Canvas.SetLeft(label, x + 8);
        Canvas.SetTop(label, y - 8);
        LayoutCanvas.Children.Add(label);
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
        ScannerCountBox.Text = input.ScannerCount.ToString(CultureInfo.InvariantCulture);
        FirstScannerXBox.Text = Format(input.FirstScannerCenterX);
        FirstScannerYBox.Text = Format(input.FirstScannerCenterY);
        ScannerPitchXBox.Text = Format(input.ScannerPitchX);
        EvenYOffsetBox.Text = Format(input.EvenScannerYOffset);
        FieldHalfXBox.Text = Format(input.ScannerFieldHalfX);
        FieldHalfYBox.Text = Format(input.ScannerFieldHalfY);
        OffsetXBox.Text = Format(input.ProcessOffsetGlobalX);
        OffsetYBox.Text = Format(input.ProcessOffsetGlobalY);
    }

    private CoordinateInput ReadInputFromScreen()
    {
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
            CellColumns = ReadInt(CellColumnsBox, _input.CellColumns),
            CellRows = ReadInt(CellRowsBox, _input.CellRows),
            ScannerCount = ReadInt(ScannerCountBox, _input.ScannerCount),
            FirstScannerCenterX = ReadDouble(FirstScannerXBox, _input.FirstScannerCenterX),
            FirstScannerCenterY = ReadDouble(FirstScannerYBox, _input.FirstScannerCenterY),
            ScannerPitchX = ReadDouble(ScannerPitchXBox, _input.ScannerPitchX),
            EvenScannerYOffset = ReadDouble(EvenYOffsetBox, _input.EvenScannerYOffset),
            ScannerFieldHalfX = ReadDouble(FieldHalfXBox, _input.ScannerFieldHalfX),
            ScannerFieldHalfY = ReadDouble(FieldHalfYBox, _input.ScannerFieldHalfY),
            ProcessOffsetGlobalX = ReadDouble(OffsetXBox, _input.ProcessOffsetGlobalX),
            ProcessOffsetGlobalY = ReadDouble(OffsetYBox, _input.ProcessOffsetGlobalY)
        };
    }

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

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
}
