namespace MofCoordinateDemo.Models;

/// <summary>
/// WPF 화면에서 입력받는 전체 좌표 변환 파라미터이다.
/// Recipe, Review Camera, Scanner Array, Review Offset을 한곳에 모아
/// "어떤 입력으로 Stage/Scanner 좌표가 만들어지는지" 설명하기 쉽게 구성했다.
/// </summary>
public sealed class CoordinateInput
{
    public double BoardSizeX { get; set; } = 1500;
    public double BoardSizeY { get; set; } = 925;

    public double AlignMarginX { get; set; } = 55;
    public double AlignMarginY { get; set; } = 45;

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
    public int CellColumns { get; set; } = 28;
    public int CellRows { get; set; } = 18;

    public int ScannerCount { get; set; } = 8;
    public double FirstScannerCenterX { get; set; } = 479.7;
    public double FirstScannerCenterY { get; set; } = 1640.1;
    public double ScannerPitchX { get; set; } = 100;
    public double EvenScannerYOffset { get; set; } = 45;
    public double ScannerFieldHalfX { get; set; } = 55;
    public double ScannerFieldHalfY { get; set; } = 55;

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
    public double FieldHalfX { get; init; }
    public double FieldHalfY { get; init; }
}

public sealed class CellCommand
{
    public int Column { get; init; }
    public int Row { get; init; }
    public double LocalX { get; init; }
    public double LocalY { get; init; }
    public double StageX { get; init; }
    public double StageY { get; init; }
    public string ScannerName { get; init; } = "";
    public string ScannerType { get; init; } = "";
    public double RelativeX { get; init; }
    public double RelativeY { get; init; }
    public double Gx { get; init; }
    public double Gy { get; init; }
    public bool InField { get; init; }
}

public sealed class CoordinateResult
{
    public double Ak1GlobalX { get; init; }
    public double Ak1GlobalY { get; init; }
    public IReadOnlyList<ScannerModel> Scanners { get; init; } = Array.Empty<ScannerModel>();
    public IReadOnlyList<CellCommand> Commands { get; init; } = Array.Empty<CellCommand>();
}
