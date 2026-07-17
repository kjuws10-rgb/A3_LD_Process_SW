namespace MofCoordinateDemo.Models;

/// <summary>
/// CoordinateInput contains every parameter needed to explain the coordinate chain.
/// The sample keeps these values in one object so the UI can show how recipe data,
/// review measurement data, scanner geometry, and DOE beam selection create results.
/// </summary>
public sealed class CoordinateInput
{
    public double BoardSizeX { get; set; } = 1500;
    public double BoardSizeY { get; set; } = 925;
    public double AlignMarginX { get; set; } = 55;
    public double AlignMarginY { get; set; } = 45;
    public double HomeStageY { get; set; } = 0;
    // +1 means the forward logistics direction increases Stage Y.
    // The board passes the review camera first and then reaches the scanner zone.
    public int ForwardTransportSignY { get; set; } = 1;

    public double ReviewCenterGlobalX { get; set; } = 105;
    public double ReviewCenterGlobalY { get; set; } = 1200;
    public double ReviewPixelCenterU { get; set; } = 1224;
    public double ReviewPixelCenterV { get; set; } = 1024;
    public double PixelScaleX { get; set; } = 0.00345;
    public double PixelScaleY { get; set; } = 0.00345;
    public double MeasuredAk1U { get; set; } = 1282;
    public double MeasuredAk1V { get; set; } = 1053;
    public double ThetaAlignDeg { get; set; } = 0.05;

    public double CellFirstX { get; set; } = 50;
    public double CellFirstY { get; set; } = 35;
    public double CellPitchX { get; set; } = 50;
    public double CellPitchY { get; set; } = 45;
    public double PatternOffsetX { get; set; } = 10;
    public double PatternOffsetY { get; set; } = 0;
    public int CellColumns { get; set; } = 3;
    public int CellRows { get; set; } = 4;
    public int CellBlockColumns { get; set; } = 1;
    public int CellBlockRows { get; set; } = 2;
    public double CellBlockPitchX { get; set; } = 0;
    public double CellBlockPitchY { get; set; } = 140;

    public int SelectedCellColumn { get; set; } = 0;
    public int SelectedCellRow { get; set; } = 0;
    public int SelectedCellBlock { get; set; } = 1;

    public int ScannerCount { get; set; } = 8;
    // Explicit H1 scanner origin in Stage Global coordinates.
    // It must match ReviewCenter + ReviewToFirstScannerOffset within tolerance.
    public double FirstScannerInitialStageX { get; set; } = 479.7;
    public double FirstScannerInitialStageY { get; set; } = 1640.1;
    public double ScannerOriginTolerance { get; set; } = 0.001;
    // Fixed equipment geometry: H1 scanner center minus review camera center.
    // This calibration value is fundamentally different from a review-error correction.
    public double ReviewToFirstScannerOffsetX { get; set; } = 374.7;
    public double ReviewToFirstScannerOffsetY { get; set; } = 440.1;
    public double ScannerPitchX { get; set; } = 100;
    public double EvenScannerYOffset { get; set; } = 45;
    public double ScannerFieldHalfX { get; set; } = 55;
    public double ScannerFieldHalfY { get; set; } = 55;

    public string HighlightScannerHeads { get; set; } = "1,5";
    public int ReviewBasisScannerHead { get; set; } = 5;
    public int ReviewBasisDoeBeam { get; set; } = 1;
    public double DoeBeamPitchX { get; set; } = 0.18;
    public double DoeBeamPitchY { get; set; } = 0.18;

    public double ProcessOffsetGlobalX { get; set; } = 0;
    public double ProcessOffsetGlobalY { get; set; } = 0;
}

public sealed class ScannerModel
{
    public string Name { get; init; } = "";
    public int Index { get; init; }
    public string MountType { get; init; } = "Odd";
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double ReviewCameraOffsetX { get; init; }
    public double ReviewCameraOffsetY { get; init; }
    public double FieldHalfX { get; init; }
    public double FieldHalfY { get; init; }
    public bool IsHighlighted { get; init; }
}

public sealed class DoeBeamModel
{
    public int BeamNo { get; init; }
    public int Row { get; init; }
    public int Column { get; init; }
    public double ScannerOffsetX { get; init; }
    public double ScannerOffsetY { get; init; }

    public string MatrixCoordinate => FormatPair(ScannerOffsetX, ScannerOffsetY);

    public static string FormatPair(double x, double y) => $"({x:0.###}, {y:0.###})";
}

public sealed class CellCommand
{
    public int MofSequence { get; set; }
    public int Column { get; init; }
    public int Row { get; init; }
    public int CellBlock { get; init; }
    public int CellBlockColumn { get; init; }
    public int CellBlockRow { get; init; }
    public bool IsSelectedCell { get; init; }
    public bool IsHighlightedScanner { get; init; }

    public double LocalX { get; init; }
    public double LocalY { get; init; }
    public double DesignStageX { get; init; }
    public double DesignStageY { get; init; }
    public double ProcessStageX { get; init; }
    public double ProcessStageY { get; init; }
    public double ReviewCameraRelativeX { get; init; }
    public double ReviewCameraRelativeY { get; init; }

    public string ScannerName { get; init; } = "";
    public int ScannerIndex { get; init; }
    public string ScannerType { get; init; } = "";
    public double ScannerPhysicalOffsetX { get; init; }
    public double ScannerPhysicalOffsetY { get; init; }
    public double ScannerRelativeFromPhysicalOffsetX { get; init; }
    public double ScannerRelativeFromPhysicalOffsetY { get; init; }
    public double PhysicalTransformErrorX { get; init; }
    public double PhysicalTransformErrorY { get; init; }
    public double RelativeX { get; init; }
    public double RelativeY { get; init; }
    public double Gx { get; init; }
    public double Gy { get; init; }
    public bool InField { get; init; }

    public int ReviewBasisHead { get; init; }
    public int ReviewBasisBeam { get; init; }
    public double ReviewReferenceStageX { get; init; }
    public double ReviewReferenceStageY { get; init; }
    public double ReviewCoordinateX { get; init; }
    public double ReviewCoordinateY { get; init; }
    public double ReviewPixelU { get; init; }
    public double ReviewPixelV { get; init; }

    public string ColumnLetter => ToColumnLetter(Column);
    public string CellIndex => $"Cell#{CellBlock} {ColumnLetter}{Row + 1}";
    public string MatrixPointName => $"{ColumnLetter}{Row + 1}";
    public string DesignLocalMatrix => FormatPair(LocalX, LocalY);
    public string DesignStageMatrix => FormatPair(DesignStageX, DesignStageY);
    public string ProcessStageMatrix => FormatPair(ProcessStageX, ProcessStageY);
    public string ProcessGMatrix => FormatPair(Gx, Gy);
    public string ReviewCameraRelativeMatrix => FormatPair(ReviewCameraRelativeX, ReviewCameraRelativeY);
    public string ScannerPhysicalOffsetMatrix => FormatPair(ScannerPhysicalOffsetX, ScannerPhysicalOffsetY);
    public string ScannerRelativeFromPhysicalOffsetMatrix => FormatPair(ScannerRelativeFromPhysicalOffsetX, ScannerRelativeFromPhysicalOffsetY);
    public string PhysicalTransformErrorMatrix => FormatPair(PhysicalTransformErrorX, PhysicalTransformErrorY);
    public string ScannerRelativeMatrix => FormatPair(RelativeX, RelativeY);
    public string ReviewReferenceMatrix => FormatPair(ReviewReferenceStageX, ReviewReferenceStageY);
    public string ReviewMatrix => FormatPair(ReviewCoordinateX, ReviewCoordinateY);
    public string ReviewPixelMatrix => FormatPair(ReviewPixelU, ReviewPixelV);
    public string DoeSelection => $"H{ReviewBasisHead} / DOE{ReviewBasisBeam:00}";
    public string MofSequenceText => $"Reverse MOF #{MofSequence}";

    private static string FormatPair(double x, double y) => $"({x:0.###}, {y:0.###})";

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
}

public sealed class StageMotionStep
{
    public int StepNo { get; init; }
    public string Name { get; init; } = "";
    public string Direction { get; init; } = "";
    public double FromStageY { get; init; }
    public double ToStageY { get; init; }
    public string Operation { get; init; } = "";
}

public sealed class MatrixRow
{
    private readonly Dictionary<string, string> _values = new();

    public string RowHeader { get; set; } = "";
    public bool IsGroupHeader { get; set; }
    public int CellBlock { get; set; }
    public int PointRow { get; set; }

    public string this[string column]
    {
        get => _values.TryGetValue(column, out var value) ? value : "";
        set => _values[column] = value;
    }
}

public sealed class CoordinateResult
{
    public double Ak1GlobalX { get; init; }
    public double Ak1GlobalY { get; init; }
    public IReadOnlyList<ScannerModel> Scanners { get; init; } = Array.Empty<ScannerModel>();
    public IReadOnlyList<DoeBeamModel> DoeBeams { get; init; } = Array.Empty<DoeBeamModel>();
    public DoeBeamModel SelectedDoeBeam { get; init; } = new();
    public ScannerModel SelectedReviewScanner { get; init; } = new();
    public IReadOnlyList<CellCommand> Commands { get; init; } = Array.Empty<CellCommand>();
    public IReadOnlyList<CellCommand> MofExecutionCommands { get; init; } = Array.Empty<CellCommand>();
    public IReadOnlyList<StageMotionStep> MotionSteps { get; init; } = Array.Empty<StageMotionStep>();
    public double TurnaroundStageY { get; init; }
    public bool EquipmentOrderValid { get; init; }
    public double ExpectedFirstScannerStageX { get; init; }
    public double ExpectedFirstScannerStageY { get; init; }
    public double FirstScannerOriginErrorX { get; init; }
    public double FirstScannerOriginErrorY { get; init; }
    public bool FirstScannerOriginValid { get; init; }
}
