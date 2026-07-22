using MofCoordinateDemo.Models;

namespace MofCoordinateDemo.Services;

/// <summary>
/// Generates the full coordinate chain:
/// recipe local coordinate -> design stage coordinate -> corrected process stage
/// -> review-camera-relative coordinate -> physical camera-to-scanner offset
/// -> scanner Gx/Gy command -> review coordinate based on a selected DOE beam.
/// </summary>
public sealed class CoordinateTransformService
{
    public CoordinateResult Generate(CoordinateInput input)
    {
        var highlightedHeads = ParseHeadSet(input.HighlightScannerHeads);

        var deltaPixelU = input.MeasuredAk1U - input.ReviewPixelCenterU;
        var deltaPixelV = input.MeasuredAk1V - input.ReviewPixelCenterV;
        var ak1GlobalX = input.ReviewCenterGlobalX + deltaPixelU * input.PixelScaleX;
        var ak1GlobalY = input.ReviewCenterGlobalY + deltaPixelV * input.PixelScaleY;

        var scanners = BuildScanners(input, highlightedHeads);
        var expectedFirstScannerStageX = input.ReviewCenterGlobalX + input.ReviewToFirstScannerOffsetX;
        var expectedFirstScannerStageY = input.ReviewCenterGlobalY + input.ReviewToFirstScannerOffsetY;
        var firstScannerOriginErrorX = input.FirstScannerInitialStageX - expectedFirstScannerStageX;
        var firstScannerOriginErrorY = input.FirstScannerInitialStageY - expectedFirstScannerStageY;
        var firstScannerOriginValid =
            Math.Abs(firstScannerOriginErrorX) <= Math.Abs(input.ScannerOriginTolerance) &&
            Math.Abs(firstScannerOriginErrorY) <= Math.Abs(input.ScannerOriginTolerance);
        var doeBeams = BuildDoeBeams(input);
        var selectedReviewScanner = scanners.FirstOrDefault(x => x.Index == Clamp(input.ReviewBasisScannerHead, 1, scanners.Count))
                                    ?? scanners[0];
        var selectedDoeBeam = doeBeams.FirstOrDefault(x => x.BeamNo == Clamp(input.ReviewBasisDoeBeam, 1, 16))
                              ?? doeBeams[0];
        var reviewReference = ToStageDoePosition(selectedReviewScanner, selectedDoeBeam);

        var theta = input.ThetaAlignDeg * Math.PI / 180.0;
        var cos = Math.Cos(theta);
        var sin = Math.Sin(theta);
        var blockColumns = Math.Max(1, input.CellBlockColumns);
        var blockRows = Math.Max(1, input.CellBlockRows);
        var blockPitchX = EffectiveBlockPitchX(input);
        var blockPitchY = EffectiveBlockPitchY(input);
        var commands = new List<CellCommand>(input.CellColumns * input.CellRows * blockColumns * blockRows);

        for (var blockRow = 0; blockRow < blockRows; blockRow++)
        {
            for (var blockColumn = 0; blockColumn < blockColumns; blockColumn++)
            {
                var cellBlock = blockRow * blockColumns + blockColumn + 1;
                var blockOriginX = blockColumn * blockPitchX;
                var blockOriginY = blockRow * blockPitchY;

                for (var row = 0; row < input.CellRows; row++)
                {
                    for (var column = 0; column < input.CellColumns; column++)
                    {
                        var localX = blockOriginX + input.CellFirstX + column * input.CellPitchX + input.PatternOffsetX;
                        var localY = blockOriginY + input.CellFirstY + row * input.CellPitchY + input.PatternOffsetY;

                        // Design stage coordinate is the ideal target from AK1 and theta only.
                        var designStageX = ak1GlobalX + cos * localX - sin * localY;
                        var designStageY = ak1GlobalY + sin * localX + cos * localY;

                        // Process stage coordinate includes the correction offset from review feedback.
                        var processStageX = designStageX + input.ProcessOffsetGlobalX;
                        var processStageY = designStageY + input.ProcessOffsetGlobalY;

                        var command = CreateCommand(
                            input,
                            column,
                            row,
                            cellBlock,
                            blockColumn,
                            blockRow,
                            localX,
                            localY,
                            designStageX,
                            designStageY,
                            processStageX,
                            processStageY,
                            scanners,
                            selectedReviewScanner,
                            selectedDoeBeam,
                            reviewReference);

                        commands.Add(command);
                    }
                }
            }
        }

        // During reverse transport the board features pass the fixed scanner array from
        // AK1 toward AK2. Local board Y is independent of the Stage axis sign, so it is
        // the stable ordering key for both positive and negative transport conventions.
        var forwardSignY = input.ForwardTransportSignY >= 0 ? 1 : -1;
        var mofExecutionCommands = commands
            .OrderBy(command => command.LocalY)
            .ThenBy(command => command.LocalX)
            .ThenBy(command => command.ScannerIndex)
            .ToList();

        for (var index = 0; index < mofExecutionCommands.Count; index++)
        {
            mofExecutionCommands[index].MofSequence = index + 1;
        }

        var scannerZoneEndY = forwardSignY > 0
            ? scanners.Max(scanner => scanner.CenterY + scanner.FieldHalfY)
            : scanners.Min(scanner => scanner.CenterY - scanner.FieldHalfY);
        var targetZoneEndY = forwardSignY > 0
            ? commands.Max(command => command.ProcessStageY)
            : commands.Min(command => command.ProcessStageY);
        var turnaroundStageY = forwardSignY > 0
            ? Math.Max(scannerZoneEndY, targetZoneEndY)
            : Math.Min(scannerZoneEndY, targetZoneEndY);
        var equipmentOrderValid = scanners.All(scanner =>
            (scanner.CenterY - input.ReviewCenterGlobalY) * forwardSignY > 0);

        var motionSteps = BuildMotionSteps(input, turnaroundStageY, forwardSignY);

        return new CoordinateResult
        {
            Ak1GlobalX = Round(ak1GlobalX),
            Ak1GlobalY = Round(ak1GlobalY),
            Scanners = scanners,
            DoeBeams = doeBeams,
            SelectedDoeBeam = selectedDoeBeam,
            SelectedReviewScanner = selectedReviewScanner,
            Commands = commands,
            MofExecutionCommands = mofExecutionCommands,
            MotionSteps = motionSteps,
            TurnaroundStageY = Round(turnaroundStageY),
            EquipmentOrderValid = equipmentOrderValid,
            ExpectedFirstScannerStageX = Round(expectedFirstScannerStageX),
            ExpectedFirstScannerStageY = Round(expectedFirstScannerStageY),
            FirstScannerOriginErrorX = Round(firstScannerOriginErrorX),
            FirstScannerOriginErrorY = Round(firstScannerOriginErrorY),
            FirstScannerOriginValid = firstScannerOriginValid
        };
    }

    private static IReadOnlyList<StageMotionStep> BuildMotionSteps(
        CoordinateInput input,
        double turnaroundStageY,
        int forwardSignY)
    {
        var forward = forwardSignY > 0 ? "+Y 정방향" : "-Y 정방향";
        var reverse = forwardSignY > 0 ? "-Y 역방향" : "+Y 역방향";

        return new[]
        {
            new StageMotionStep
            {
                StepNo = 1,
                Name = "Home → Review Camera",
                Direction = forward,
                FromStageY = Round(input.HomeStageY),
                ToStageY = Round(input.ReviewCenterGlobalY),
                Operation = "Review Camera 위치를 통과하며 Scanner 방향으로 전진"
            },
            new StageMotionStep
            {
                StepNo = 2,
                Name = "Review Camera → Scanner Turnaround",
                Direction = forward,
                FromStageY = Round(input.ReviewCenterGlobalY),
                ToStageY = Round(turnaroundStageY),
                Operation = "Scanner 가공 시작측까지 이동 후 방향 반전"
            },
            new StageMotionStep
            {
                StepNo = 3,
                Name = "Reverse MOF Processing",
                Direction = reverse,
                FromStageY = Round(turnaroundStageY),
                ToStageY = Round(input.ReviewCenterGlobalY),
                Operation = "역물류 이동 중 기판 AK1 측에서 AK2 측 순서로 MOF 가공"
            },
            new StageMotionStep
            {
                StepNo = 4,
                Name = "Post-Process Review",
                Direction = "정지/측정",
                FromStageY = Round(input.ReviewCenterGlobalY),
                ToStageY = Round(input.ReviewCenterGlobalY),
                Operation = "MOF 완료 후 Review Camera로 가공 결과 측정"
            },
            new StageMotionStep
            {
                StepNo = 5,
                Name = "Review Camera → Home",
                Direction = reverse,
                FromStageY = Round(input.ReviewCenterGlobalY),
                ToStageY = Round(input.HomeStageY),
                Operation = "측정 완료 후 원점 복귀"
            }
        };
    }

    private static IReadOnlyList<ScannerModel> BuildScanners(CoordinateInput input, IReadOnlySet<int> highlightedHeads)
    {
        var scannerCount = Math.Max(1, input.ScannerCount);
        var scanners = new List<ScannerModel>(scannerCount);

        for (var i = 0; i < scannerCount; i++)
        {
            var index = i + 1;
            var isOdd = index % 2 == 1;
            scanners.Add(new ScannerModel
            {
                Name = $"H{index}",
                Index = index,
                MountType = isOdd ? "Odd" : "Even",
                // Scanner layout starts from the explicit H1 Stage origin. The separately
                // configured camera-to-H1 offset is checked against this origin.
                CenterX = input.FirstScannerInitialStageX + i * input.ScannerPitchX,
                CenterY = input.FirstScannerInitialStageY + (isOdd ? 0 : input.EvenScannerYOffset),
                ReviewCameraOffsetX = input.ReviewToFirstScannerOffsetX + i * input.ScannerPitchX,
                ReviewCameraOffsetY = input.ReviewToFirstScannerOffsetY + (isOdd ? 0 : input.EvenScannerYOffset),
                FieldHalfX = input.ScannerFieldHalfX,
                FieldHalfY = input.ScannerFieldHalfY,
                IsHighlighted = highlightedHeads.Contains(index)
            });
        }

        return scanners;
    }

    private static IReadOnlyList<DoeBeamModel> BuildDoeBeams(CoordinateInput input)
    {
        var beams = new List<DoeBeamModel>(16);
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                var beamNo = row * 4 + column + 1;
                beams.Add(new DoeBeamModel
                {
                    BeamNo = beamNo,
                    Row = row + 1,
                    Column = column + 1,
                    ScannerOffsetX = Round((column - 1.5) * input.DoeBeamPitchX),
                    ScannerOffsetY = Round((row - 1.5) * input.DoeBeamPitchY)
                });
            }
        }

        return beams;
    }

    private static CellCommand CreateCommand(
        CoordinateInput input,
        int column,
        int row,
        int cellBlock,
        int blockColumn,
        int blockRow,
        double localX,
        double localY,
        double designStageX,
        double designStageY,
        double processStageX,
        double processStageY,
        IReadOnlyList<ScannerModel> scanners,
        ScannerModel selectedReviewScanner,
        DoeBeamModel selectedDoeBeam,
        (double X, double Y) reviewReference)
    {
        var selected = SelectScanner(processStageX, processStageY, scanners, out var inField);
        var isInHighlightedScannerArea = scanners.Any(scanner =>
            scanner.IsHighlighted &&
            Math.Abs(processStageX - scanner.CenterX) <= scanner.FieldHalfX);
        // First express the target from the review-camera optical center. Subtracting
        // the calibrated camera-to-head vector moves the same target into scanner space.
        //   CameraRelative = ProcessStage - ReviewCameraCenter
        //   ScannerRelative = CameraRelative - CameraToScannerPhysicalOffset
        var reviewCameraRelativeX = processStageX - input.ReviewCenterGlobalX;
        var reviewCameraRelativeY = processStageY - input.ReviewCenterGlobalY;
        var relativeFromPhysicalOffsetX = reviewCameraRelativeX - selected.ReviewCameraOffsetX;
        var relativeFromPhysicalOffsetY = reviewCameraRelativeY - selected.ReviewCameraOffsetY;
        var relativeX = processStageX - selected.CenterX;
        var relativeY = processStageY - selected.CenterY;
        var physicalTransformErrorX = relativeFromPhysicalOffsetX - relativeX;
        var physicalTransformErrorY = relativeFromPhysicalOffsetY - relativeY;

        var gx = selected.MountType == "Odd" ? -relativeX : relativeX;
        var gy = selected.MountType == "Odd" ? relativeY : -relativeY;

        // Convert the selected head's scanner-relative Stage coordinate back into the
        // actual review-camera coordinate system. The physical head offset must be added,
        // not removed again. DOE offset selects the actual split-beam landing position.
        var basisRelativeX = processStageX - selectedReviewScanner.CenterX;
        var basisRelativeY = processStageY - selectedReviewScanner.CenterY;
        var selectedDoeStageOffset = ToStageDoeOffset(selectedReviewScanner, selectedDoeBeam);
        var reviewX = basisRelativeX + selectedReviewScanner.ReviewCameraOffsetX + selectedDoeStageOffset.X;
        var reviewY = basisRelativeY + selectedReviewScanner.ReviewCameraOffsetY + selectedDoeStageOffset.Y;
        var reviewU = input.ReviewPixelCenterU + reviewX / input.PixelScaleX;
        var reviewV = input.ReviewPixelCenterV + reviewY / input.PixelScaleY;

        return new CellCommand
        {
            Column = column,
            Row = row,
            CellBlock = cellBlock,
            CellBlockColumn = blockColumn,
            CellBlockRow = blockRow,
            IsSelectedCell = cellBlock == input.SelectedCellBlock && column == input.SelectedCellColumn && row == input.SelectedCellRow,
            IsHighlightedScanner = isInHighlightedScannerArea,
            LocalX = Round(localX),
            LocalY = Round(localY),
            DesignStageX = Round(designStageX),
            DesignStageY = Round(designStageY),
            ProcessStageX = Round(processStageX),
            ProcessStageY = Round(processStageY),
            ReviewCameraRelativeX = Round(reviewCameraRelativeX),
            ReviewCameraRelativeY = Round(reviewCameraRelativeY),
            ScannerName = selected.Name,
            ScannerIndex = selected.Index,
            ScannerType = selected.MountType,
            ScannerPhysicalOffsetX = Round(selected.ReviewCameraOffsetX),
            ScannerPhysicalOffsetY = Round(selected.ReviewCameraOffsetY),
            ScannerRelativeFromPhysicalOffsetX = Round(relativeFromPhysicalOffsetX),
            ScannerRelativeFromPhysicalOffsetY = Round(relativeFromPhysicalOffsetY),
            PhysicalTransformErrorX = Round(physicalTransformErrorX),
            PhysicalTransformErrorY = Round(physicalTransformErrorY),
            RelativeX = Round(relativeX),
            RelativeY = Round(relativeY),
            Gx = Round(gx),
            Gy = Round(gy),
            InField = inField,
            ReviewBasisHead = selectedReviewScanner.Index,
            ReviewBasisBeam = selectedDoeBeam.BeamNo,
            ReviewReferenceStageX = Round(reviewReference.X),
            ReviewReferenceStageY = Round(reviewReference.Y),
            ReviewCoordinateX = Round(reviewX),
            ReviewCoordinateY = Round(reviewY),
            ReviewPixelU = Round(reviewU),
            ReviewPixelV = Round(reviewV)
        };
    }

    private static ScannerModel SelectScanner(double stageX, double stageY, IReadOnlyList<ScannerModel> scanners, out bool inField)
    {
        ScannerModel? nearest = null;
        var nearestDistance = double.MaxValue;
        inField = false;

        foreach (var scanner in scanners)
        {
            var dx = stageX - scanner.CenterX;
            if (Math.Abs(dx) <= scanner.FieldHalfX)
            {
                inField = true;
                return scanner;
            }

            var distance = dx * dx;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = scanner;
            }
        }

        return nearest ?? scanners[0];
    }

    private static (double X, double Y) ToStageDoePosition(ScannerModel scanner, DoeBeamModel beam)
    {
        // DOE beam offset is defined in scanner Gx/Gy space.
        // Convert it back to stage relative space by applying the inverse sign rule.
        var (stageDx, stageDy) = ToStageDoeOffset(scanner, beam);
        return (scanner.CenterX + stageDx, scanner.CenterY + stageDy);
    }

    private static (double X, double Y) ToStageDoeOffset(ScannerModel scanner, DoeBeamModel beam)
    {
        var stageDx = scanner.MountType == "Odd" ? -beam.ScannerOffsetX : beam.ScannerOffsetX;
        var stageDy = scanner.MountType == "Odd" ? beam.ScannerOffsetY : -beam.ScannerOffsetY;
        return (stageDx, stageDy);
    }

    private static IReadOnlySet<int> ParseHeadSet(string text)
    {
        var heads = new HashSet<int>();
        foreach (var token in text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(token, out var head) && head > 0)
            {
                heads.Add(head);
            }
        }

        return heads;
    }

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    private static double Round(double value) => Math.Round(value, 6);

    private static double EffectiveBlockPitchX(CoordinateInput input)
    {
        if (input.CellBlockPitchX > 0)
        {
            return input.CellBlockPitchX;
        }

        return Math.Max(1, input.CellColumns) * Math.Max(1, input.CellPitchX) + Math.Max(1, input.CellPitchX);
    }

    private static double EffectiveBlockPitchY(CoordinateInput input)
    {
        if (input.CellBlockPitchY > 0)
        {
            return input.CellBlockPitchY;
        }

        return Math.Max(1, input.CellRows) * Math.Max(1, input.CellPitchY) + Math.Max(1, input.CellPitchY);
    }
}
