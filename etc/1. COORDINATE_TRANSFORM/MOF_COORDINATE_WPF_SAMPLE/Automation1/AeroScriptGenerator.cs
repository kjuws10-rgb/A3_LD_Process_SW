using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MofCoordinateDemo.Models;

namespace MofCoordinateDemo.Automation1;

/// <summary>
/// Runs only on the upper-level client PC. The connected Automation1 controller
/// compiles and executes this generated source without regenerating coordinates.
/// </summary>
public sealed partial class AeroScriptGenerator
{
    public static IReadOnlyList<string> ResolveRequiredAxisNames(
        IReadOnlyList<CellCommand> commands,
        AeroScriptGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(options);

        var heads = commands.Select(command => command.ScannerIndex).Distinct().OrderBy(head => head).ToArray();
        if (heads.Length == 0)
        {
            return Array.Empty<string>();
        }

        var axes = new List<string>();
        if (options.Mode == AeroScriptGenerationMode.VirtualWaitSimulation)
        {
            axes.Add(ResolveAxisName(options.StageAxisName, heads[0]));
        }
        foreach (var head in heads)
        {
            axes.Add(ResolveAxisName(options.AxisXTemplate, head));
            axes.Add(ResolveAxisName(options.AxisYTemplate, head));
        }

        return axes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string Generate(
        CoordinateInput input,
        IReadOnlyList<CellCommand> commands,
        AeroScriptGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(options);

        if (commands.Count == 0)
        {
            throw new InvalidOperationException("AeroScript를 생성할 가공 좌표가 없습니다.");
        }

        ValidateCommonOptions(options);
        ValidateTargetsWithinLimits(commands, options);
        var source = options.Mode switch
        {
            AeroScriptGenerationMode.VirtualWaitSimulation => GenerateVirtualWaitSimulation(input, commands, options),
            AeroScriptGenerationMode.HardwareCoordinateProgram => GenerateHardwareCoordinateProgram(input, commands, options),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Mode))
        };
        ValidateGeneratedSource(source);
        return source;
    }

    private static string GenerateVirtualWaitSimulation(
        CoordinateInput input,
        IReadOnlyList<CellCommand> commands,
        AeroScriptGenerationOptions options)
    {
        var headNumbers = commands.Select(command => command.ScannerIndex).Distinct().ToArray();
        if (headNumbers.Length != 1)
        {
            throw new InvalidOperationException(
                "Virtual Wait Simulation은 GX-Y MOF pair 한 개를 검증하므로 Scanner Head를 정확히 한 개만 선택해야 합니다.");
        }

        var head = headNumbers[0];
        var stageAxis = ResolveAxisName(options.StageAxisName, head);
        var axisX = ResolveAxisName(options.AxisXTemplate, head);
        var axisY = ResolveAxisName(options.AxisYTemplate, head);
        var gxGroups = commands
            .GroupBy(command => Math.Round(command.Gx, 6))
            .OrderBy(group => group.Key)
            .ToArray();
        var waitCount = Math.Max(0, gxGroups.Length - 1);
        var requiredTravel = waitCount * options.WaitStepY;
        if (Math.Abs(options.StageTravelDistance) + 1e-9 < requiredTravel)
        {
            throw new InvalidOperationException(
                $"Stage travel distance는 wait 임계값 범위({requiredTravel:0.###}) 이상이어야 합니다.");
        }

        var source = CreateHeader(input, commands, options);
        source.AppendLine("// MODE: Automation1 Virtual Wait Simulation");
        source.AppendLine("// Laser, PSO, Hardware Aux, Galvo calibration commands are intentionally excluded.");
        source.AppendLine($"// Assumption: {axisX} and {stageAxis} are configured as the MOF pair in the test MCD.");
        source.AppendLine("// Purpose: verify that each wait condition releases as Stage position feedback crosses its threshold.");
        source.AppendLine("// import \"LaserOnLibrary.a1lib\" as static  // Virtual mode: disabled");
        source.AppendLine();
        source.AppendLine("program");
        source.AppendLine("    var $StartYPos as real");
        source.AppendLine();
        AppendCommonMotionSetup(source, new[] { axisX, axisY }, options);

        if (options.EnableAxes)
        {
            source.AppendLine($"    Enable([{stageAxis}, {axisX}, {axisY}])");
            source.AppendLine();
        }

        source.AppendLine($"    SetupAxisSpeed({axisX}, {Format(options.ScannerRapidSpeed)})");
        source.AppendLine($"    SetupAxisSpeed({axisY}, {Format(options.ScannerRapidSpeed)})");
        source.AppendLine();
        source.AppendLine($"    $StartYPos = {Format(options.StartYPosition)}");
        source.AppendLine($"    MoveAbsolute({stageAxis}, $StartYPos, {Format(options.StageSpeed)})");
        source.AppendLine($"    G90 G0 {axisX} 0 {axisY} 0");
        source.AppendLine();
        source.AppendLine($"    WaitForInPosition({stageAxis})");
        source.AppendLine($"    WaitForInPosition({axisX})");
        source.AppendLine($"    WaitForInPosition({axisY})");
        source.AppendLine();
        AppendSoftwareLimits(source, new[] { axisX, axisY }, options);
        source.AppendLine(
            $"    MoveAbsolute({stageAxis}, $StartYPos{FormatSignedExpression(options.StageTravelDistance)}, {Format(options.StageSpeed)})");
        source.AppendLine($"    G90 G0 {axisX} 0 {axisY} 0");
        source.AppendLine($"    WaitForMotionDone([{axisX}, {axisY}])");
        source.AppendLine();

        var directionOperator = options.StageTravelDistance >= 0 ? ">" : "<";
        var directionSign = options.StageTravelDistance >= 0 ? 1.0 : -1.0;
        for (var groupIndex = 0; groupIndex < gxGroups.Length; groupIndex++)
        {
            var group = gxGroups[groupIndex];
            source.AppendLine($"    // REPEAT {groupIndex + 1}: GX band {Format(group.Key)}");
            foreach (var command in group.OrderBy(command => command.Gy))
            {
                source.AppendLine(
                    $"    G0 {axisX} {Format(command.Gx)} {axisY} {Format(command.Gy)} " +
                    $"// {command.CellIndex}, MOF #{command.MofSequence}, " +
                    $"Process Gx/Gy=({Format(command.Gx)}, {Format(command.Gy)}), " +
                    $"Review=({Format(command.ReviewCoordinateX)}, {Format(command.ReviewCoordinateY)})");
                source.AppendLine($"    MoveDelay({axisX}, {Format(options.MoveDelayMilliseconds)})");
            }

            if (groupIndex < waitCount)
            {
                var threshold = directionSign * options.WaitStepY * (groupIndex + 1);
                source.AppendLine(
                    $"    wait(StatusGetAxisItem({stageAxis}, AxisStatusItem.PositionFeedback) " +
                    $"{directionOperator} $StartYPos{FormatSignedExpression(threshold)})");
            }

            source.AppendLine();
        }

        source.AppendLine($"    WaitForMotionDone({stageAxis})");
        source.AppendLine($"    WaitForInPosition({stageAxis})");
        source.AppendLine("    VelocityBlendingOff()");
        if (options.DisableAxesAtEnd)
        {
            source.AppendLine($"    Disable([{stageAxis}, {axisX}, {axisY}])");
        }

        source.AppendLine("end");
        return source.ToString();
    }

    private static string GenerateHardwareCoordinateProgram(
        CoordinateInput input,
        IReadOnlyList<CellCommand> commands,
        AeroScriptGenerationOptions options)
    {
        var headNumbers = commands.Select(command => command.ScannerIndex).Distinct().OrderBy(index => index).ToArray();
        if (headNumbers.Length > 1 &&
            (!options.AxisXTemplate.Contains("{0}", StringComparison.Ordinal) ||
             !options.AxisYTemplate.Contains("{0}", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Hardware 모드에서 Scanner Head를 여러 개 선택하면 GX/GY Axis Template에 {0} Head 자리표시자가 필요합니다.");
        }

        var axesByHead = headNumbers.ToDictionary(
            head => head,
            head => (
                X: ResolveAxisName(options.AxisXTemplate, head),
                Y: ResolveAxisName(options.AxisYTemplate, head)));
        var allAxes = axesByHead.Values.SelectMany(pair => new[] { pair.X, pair.Y }).Distinct().ToArray();
        var source = CreateHeader(input, commands, options);

        if (options.IncludeLaserLibraryImport)
        {
            source.AppendLine($"import \"{ValidateLibraryName(options.LaserLibraryFileName)}\" as static");
            source.AppendLine();
        }

        source.AppendLine("// MODE: Hardware Coordinate Program");
        source.AppendLine("// Laser/PSO function calls remain equipment-specific and require validated interlocks.");
        source.AppendLine("program");
        AppendCommonMotionSetup(source, allAxes, options);
        if (options.EnableAxes)
        {
            foreach (var head in headNumbers)
            {
                var axes = axesByHead[head];
                source.AppendLine($"    Enable([{axes.X}, {axes.Y}])");
            }

            source.AppendLine();
        }

        AppendSoftwareLimits(source, allAxes, options);
        source.AppendLine($"    SetupCoordinatedSpeed({Format(options.CoordinatedSpeed)})");
        source.AppendLine();

        foreach (var command in commands.OrderBy(command => command.MofSequence))
        {
            source.AppendLine(
                $"    // MOF #{command.MofSequence}: {command.CellIndex}, H{command.ScannerIndex}, " +
                $"Stage=({Format(command.ProcessStageX)}, {Format(command.ProcessStageY)}), " +
                $"Process Gx/Gy=({Format(command.Gx)}, {Format(command.Gy)}), " +
                $"Review=({Format(command.ReviewCoordinateX)}, {Format(command.ReviewCoordinateY)})");
            source.AppendLine(
                $"    MoveLinear([{axesByHead[command.ScannerIndex].X}, {axesByHead[command.ScannerIndex].Y}], " +
                $"[{Format(command.Gx)}, {Format(command.Gy)}], {Format(options.CoordinatedSpeed)})");
        }

        source.AppendLine("    VelocityBlendingOff()");
        if (options.DisableAxesAtEnd)
        {
            foreach (var head in headNumbers)
            {
                var axes = axesByHead[head];
                source.AppendLine($"    Disable([{axes.X}, {axes.Y}])");
            }
        }

        source.AppendLine("end");
        return source.ToString();
    }

    private static StringBuilder CreateHeader(
        CoordinateInput input,
        IReadOnlyCollection<CellCommand> commands,
        AeroScriptGenerationOptions options)
    {
        var source = new StringBuilder(Math.Max(4096, commands.Count * 140));
        source.AppendLine("// Generated by the A1 upper-level client. Coordinates must not be regenerated on the controller.");
        source.AppendLine($"// Generated UTC: {DateTimeOffset.UtcNow:O}");
        source.AppendLine($"// Generation mode: {options.Mode}");
        source.AppendLine($"// Review camera center: ({Format(input.ReviewCenterGlobalX)}, {Format(input.ReviewCenterGlobalY)})");
        source.AppendLine($"// Dynamic process offset: ({Format(input.ProcessOffsetGlobalX)}, {Format(input.ProcessOffsetGlobalY)})");
        source.AppendLine($"// Target count: {commands.Count}");
        return source;
    }

    private static void AppendCommonMotionSetup(
        StringBuilder source,
        IReadOnlyCollection<string> scannerAxes,
        AeroScriptGenerationOptions options)
    {
        source.AppendLine("    SetupTaskTimeUnits(TimeUnits.Seconds)");
        source.AppendLine("    SetupTaskTargetMode(TargetMode.Absolute)");
        source.AppendLine("    VelocityBlendingOn()");
        source.AppendLine("    SetupTaskWaitMode(WaitMode.Auto)");
        source.AppendLine();
        foreach (var axis in scannerAxes)
        {
            source.AppendLine($"    SetupAxisRampType({axis}, RampType.Sine)");
            source.AppendLine($"    SetupAxisRampValue({axis}, RampMode.Rate, {Format(options.RampRate)})");
        }

        source.AppendLine("    SetupCoordinatedRampType(RampType.Sine)");
        source.AppendLine($"    SetupCoordinatedRampValue(RampMode.Rate, {Format(options.RampRate)})");
        source.AppendLine(
            $"    ParameterSetTaskValue(TaskGetIndex(), TaskParameter.DefaultCoordinatedAccelLimit, {Format(options.RampRate)})");
        source.AppendLine(
            $"    ParameterSetTaskValue(TaskGetIndex(), TaskParameter.DefaultCoordinatedCircularAccelLimit, {Format(options.RampRate)})");
        foreach (var axis in scannerAxes)
        {
            source.AppendLine(
                $"    ParameterSetAxisValue({axis}, AxisParameter.TrajectoryFirFilter, {Format(options.TrajectoryFirFilter)})");
        }

        source.AppendLine(
            $"    ParameterSetTaskValue(TaskGetIndex(), TaskParameter.MotionUpdateRate, {Format(options.MotionUpdateRateKhz)})");
        source.AppendLine(
            $"    ParameterSetTaskValue(TaskGetIndex(), TaskParameter.ExecuteNumLines, {options.ExecuteNumLines.ToString(CultureInfo.InvariantCulture)})");
        source.AppendLine($"    Dwell({Format(options.SetupDwellSeconds)})");
        source.AppendLine();
    }

    private static void AppendSoftwareLimits(
        StringBuilder source,
        IEnumerable<string> axes,
        AeroScriptGenerationOptions options)
    {
        foreach (var axis in axes)
        {
            source.AppendLine(
                $"    ParameterSetAxisValue({axis}, AxisParameter.SoftwareLimitHigh, {Format(options.SoftwareLimitHigh)})");
            source.AppendLine(
                $"    ParameterSetAxisValue({axis}, AxisParameter.SoftwareLimitLow, {Format(options.SoftwareLimitLow)})");
        }

        source.AppendLine();
    }

    private static void ValidateCommonOptions(AeroScriptGenerationOptions options)
    {
        var positiveValues = new Dictionary<string, double>
        {
            ["Scanner rapid speed"] = options.ScannerRapidSpeed,
            ["Coordinated speed"] = options.CoordinatedSpeed,
            ["Ramp rate"] = options.RampRate,
            ["Motion update rate"] = options.MotionUpdateRateKhz,
            ["Stage speed"] = options.StageSpeed,
            ["Wait step Y"] = options.WaitStepY
        };

        foreach (var (name, value) in positiveValues)
        {
            if (!double.IsFinite(value) || value <= 0)
            {
                throw new InvalidOperationException($"{name}는 0보다 큰 유한값이어야 합니다.");
            }
        }

        var finiteValues = new[]
        {
            options.StartYPosition,
            options.StageTravelDistance,
            options.TrajectoryFirFilter,
            options.MoveDelayMilliseconds,
            options.SetupDwellSeconds,
            options.SoftwareLimitLow,
            options.SoftwareLimitHigh
        };
        if (finiteValues.Any(value => !double.IsFinite(value)) ||
            options.ExecuteNumLines <= 0 ||
            options.MoveDelayMilliseconds < 0 ||
            options.SetupDwellSeconds < 0 ||
            options.TrajectoryFirFilter < 0)
        {
            throw new InvalidOperationException("ExecuteNumLines, MoveDelay 또는 Dwell 설정값이 올바르지 않습니다.");
        }

        if (options.MotionUpdateRateKhz is < 0.02 or > 100)
        {
            throw new InvalidOperationException("MotionUpdateRate는 공식 허용 범위인 0.02~100 kHz 안에 있어야 합니다.");
        }

        if (options.SoftwareLimitLow >= options.SoftwareLimitHigh)
        {
            throw new InvalidOperationException("SoftwareLimitLow는 SoftwareLimitHigh보다 작아야 합니다.");
        }
    }

    private static void ValidateTargetsWithinLimits(
        IEnumerable<CellCommand> commands,
        AeroScriptGenerationOptions options)
    {
        var outOfRange = commands.FirstOrDefault(command =>
            command.Gx < options.SoftwareLimitLow || command.Gx > options.SoftwareLimitHigh ||
            command.Gy < options.SoftwareLimitLow || command.Gy > options.SoftwareLimitHigh);
        if (outOfRange is not null)
        {
            throw new InvalidOperationException(
                $"{outOfRange.CellIndex} Gx/Gy ({outOfRange.Gx:0.###}, {outOfRange.Gy:0.###})가 " +
                $"Software Limit [{options.SoftwareLimitLow:0.###}, {options.SoftwareLimitHigh:0.###}] 밖에 있습니다.");
        }
    }

    private static string ValidateLibraryName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.IndexOfAny(new[] { '\r', '\n', '"' }) >= 0 ||
            !fileName.EndsWith(".a1lib", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Laser library 파일 이름이 올바르지 않습니다.");
        }

        return fileName;
    }

    private static void ValidateGeneratedSource(string source)
    {
        if (!source.Contains("program", StringComparison.Ordinal) ||
            !source.TrimEnd().EndsWith("end", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Generated AeroScript must contain a program/end block.");
        }

        if (GCodeVariableWhitespaceRegex().IsMatch(source))
        {
            throw new InvalidOperationException(
                "Generated AeroScript contains an invalid G-code variable operand. " +
                "Use X$variable syntax or an AeroScript motion function such as MoveAbsolute().");
        }
    }

    private static string ResolveAxisName(string template, int head)
    {
        var axisName = (template ?? "").Replace("{0}", head.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        if (!AxisIdentifierRegex().IsMatch(axisName))
        {
            throw new InvalidOperationException($"Axis 이름 '{axisName}'은 유효한 AeroScript 식별자가 아닙니다.");
        }

        return axisName;
    }

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string FormatSignedExpression(double value) =>
        value >= 0 ? $" + {Format(value)}" : $" - {Format(Math.Abs(value))}";

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex AxisIdentifierRegex();

    [GeneratedRegex(@"(?m)^\s*G\d+[^\r\n]*\b[A-Za-z_][A-Za-z0-9_]*\s+\$[A-Za-z_]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GCodeVariableWhitespaceRegex();
}
