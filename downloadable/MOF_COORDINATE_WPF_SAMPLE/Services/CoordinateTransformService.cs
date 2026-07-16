using MofCoordinateDemo.Models;

namespace MofCoordinateDemo.Services;

/// <summary>
/// Review AK1 측정값에서 Stage Anchor를 만들고,
/// Recipe Cell 좌표를 Stage Global 좌표와 Scanner Gx/Gy 명령으로 변환한다.
/// 실제 장비 코드에서는 이 서비스가 Process Plan 생성기의 핵심 계산부가 된다.
/// </summary>
public sealed class CoordinateTransformService
{
    public CoordinateResult Generate(CoordinateInput input)
    {
        // 1) Review Camera 영상 중심과 측정된 AK1 pixel 차이를 mm로 환산한다.
        //    이 값이 실제 Stage 위에 놓인 기판의 AK1 위치가 된다.
        var deltaPixelU = input.MeasuredAk1U - input.ReviewPixelCenterU;
        var deltaPixelV = input.MeasuredAk1V - input.ReviewPixelCenterV;
        var ak1GlobalX = input.ReviewCenterGlobalX + deltaPixelU * input.PixelScaleX;
        var ak1GlobalY = input.ReviewCenterGlobalY + deltaPixelV * input.PixelScaleY;

        var scanners = BuildScanners(input);
        var commands = new List<CellCommand>(input.CellColumns * input.CellRows);

        var theta = input.ThetaAlignDeg * Math.PI / 180.0;
        var cos = Math.Cos(theta);
        var sin = Math.Sin(theta);

        for (var row = 0; row < input.CellRows; row++)
        {
            for (var column = 0; column < input.CellColumns; column++)
            {
                // 2) Recipe Local 좌표: AK1을 원점으로 하는 제품 내부 좌표이다.
                var localX = input.CellFirstX + column * input.CellPitchX + input.PatternOffsetX;
                var localY = input.CellFirstY + row * input.CellPitchY + input.PatternOffsetY;

                // 3) Review에서 얻은 기판 회전각을 적용한 뒤 Stage Global로 이동한다.
                var rotatedX = cos * localX - sin * localY;
                var rotatedY = sin * localX + cos * localY;
                var stageX = ak1GlobalX + rotatedX + input.ProcessOffsetGlobalX;
                var stageY = ak1GlobalY + rotatedY + input.ProcessOffsetGlobalY;

                var command = CreateScannerCommand(column, row, localX, localY, stageX, stageY, scanners);
                commands.Add(command);
            }
        }

        return new CoordinateResult
        {
            Ak1GlobalX = ak1GlobalX,
            Ak1GlobalY = ak1GlobalY,
            Scanners = scanners,
            Commands = commands
        };
    }

    private static IReadOnlyList<ScannerModel> BuildScanners(CoordinateInput input)
    {
        var scannerCount = Math.Max(1, input.ScannerCount);
        var scanners = new List<ScannerModel>(scannerCount);

        for (var i = 0; i < scannerCount; i++)
        {
            var oneBasedIndex = i + 1;
            var isOdd = oneBasedIndex % 2 == 1;

            // Zigzag 배치: 홀수 Scanner는 기준 Y, 짝수 Scanner는 Y Offset을 적용한다.
            // 실제 설비에서는 이 값을 기구 설계치 또는 캘리브레이션 데이터에서 읽는다.
            scanners.Add(new ScannerModel
            {
                Name = $"H{oneBasedIndex}",
                Index = oneBasedIndex,
                MountType = isOdd ? "Odd" : "Even",
                CenterX = input.FirstScannerCenterX + i * input.ScannerPitchX,
                CenterY = input.FirstScannerCenterY + (isOdd ? 0 : input.EvenScannerYOffset),
                FieldHalfX = input.ScannerFieldHalfX,
                FieldHalfY = input.ScannerFieldHalfY
            });
        }

        return scanners;
    }

    private static CellCommand CreateScannerCommand(
        int column,
        int row,
        double localX,
        double localY,
        double stageX,
        double stageY,
        IReadOnlyList<ScannerModel> scanners)
    {
        // 우선 Field 안에 들어오는 Scanner를 찾고, 없다면 가장 가까운 Scanner를 선택한다.
        // 화면 설명용 샘플이라 모든 Cell을 보여주기 위해 Out-of-field 상태도 같이 표시한다.
        ScannerModel? selected = null;
        var inField = false;
        var nearestDistance = double.MaxValue;

        foreach (var scanner in scanners)
        {
            var dx = stageX - scanner.CenterX;
            var dy = stageY - scanner.CenterY;
            var inside = Math.Abs(dx) <= scanner.FieldHalfX && Math.Abs(dy) <= scanner.FieldHalfY;

            if (inside)
            {
                selected = scanner;
                inField = true;
                break;
            }

            var distance = dx * dx + dy * dy;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                selected = scanner;
            }
        }

        selected ??= scanners[0];

        var relativeX = stageX - selected.CenterX;
        var relativeY = stageY - selected.CenterY;

        // Scanner 장착 방향에 따른 부호 규칙이다.
        // Odd : +Gx=-StageRelativeX, +Gy=+StageRelativeY
        // Even: +Gx=+StageRelativeX, +Gy=-StageRelativeY
        var gx = selected.MountType == "Odd" ? -relativeX : relativeX;
        var gy = selected.MountType == "Odd" ? relativeY : -relativeY;

        return new CellCommand
        {
            Column = column,
            Row = row,
            LocalX = Round(localX),
            LocalY = Round(localY),
            StageX = Round(stageX),
            StageY = Round(stageY),
            ScannerName = selected.Name,
            ScannerType = selected.MountType,
            RelativeX = Round(relativeX),
            RelativeY = Round(relativeY),
            Gx = Round(gx),
            Gy = Round(gy),
            InField = inField
        };
    }

    private static double Round(double value) => Math.Round(value, 6);
}
