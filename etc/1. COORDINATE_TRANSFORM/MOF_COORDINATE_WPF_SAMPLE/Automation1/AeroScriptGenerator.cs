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
    public const string GeneratorRevision = "20260722-virtual-counter-monitor-v6";

    public const int MonitorStagePositionGlobalRealIndex = 0;
    public const int MonitorStageSpeedGlobalRealIndex = 1;
    public const int MonitorStageTargetGlobalRealIndex = 2;
    public const int MonitorCurrentSequenceGlobalIntegerIndex = 0;
    public const int MonitorTotalTargetsGlobalIntegerIndex = 1;

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
            AeroScriptGenerationMode.VirtualWaitSimulation => GenerateVirtualCounterSimulation(input, commands, options),
            AeroScriptGenerationMode.ExternalStageAuxMofProgram => GenerateExternalStageAuxMofProgram(input, commands, options),
            AeroScriptGenerationMode.HardwareCoordinateProgram => GenerateHardwareCoordinateProgram(input, commands, options),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Mode))
        };
        ValidateGeneratedSource(source);
        return source;
    }

    private static string GenerateExternalStageAuxMofProgram(
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
                "External Stage AUX MOF mode requires {0} in both scanner axis templates when multiple heads are selected.");
        }

        var axesByHead = headNumbers.ToDictionary(
            head => head,
            head => (
                X: ResolveAxisName(options.AxisXTemplate, head),
                Y: ResolveAxisName(options.AxisYTemplate, head)));
        var allAxes = axesByHead.Values
            .SelectMany(pair => new[] { pair.X, pair.Y })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var orderedCommands = commands.OrderBy(command => command.MofSequence).ToArray();
        var monitorSequenceByMof = orderedCommands
            .Select((command, index) => (command.MofSequence, MonitorSequence: index + 1))
            .ToDictionary(item => item.MofSequence, item => item.MonitorSequence);
        var firstLocalY = orderedCommands[0].LocalY;
        var localYGroups = orderedCommands
            .GroupBy(command => Math.Round(command.LocalY, 6))
            .ToArray();
        var requiredTravel = options.AuxiliaryInitialWaitDistance +
                             Math.Max(0, orderedCommands[^1].LocalY - firstLocalY);
        if (Math.Abs(options.StageTravelDistance) + 1e-9 < requiredTravel)
        {
            throw new InvalidOperationException(
                $"External Stage travel must be at least the final AUX wait distance ({requiredTravel:0.###} mm).");
        }

        var source = CreateHeader(input, commands, options);
        source.AppendLine("// MODE: Equipment - External Stage AUX MOF");
        source.AppendLine("// The third-party Stage is not an Automation1 axis and is never commanded by this script.");
        source.AppendLine("// Stage encoder feedback enters each scanner GY auxiliary feedback input.");
        source.AppendLine("// Laser/PSO output remains disabled; validate the AUX signal and MOF compensation before enabling laser output.");
        source.AppendLine();
        source.AppendLine("program");
        source.AppendLine("    var $StageEncoderCountsPerUnit as real");
        foreach (var head in headNumbers)
        {
            source.AppendLine($"    var $EncoderScaleGYH{head} as real");
        }

        source.AppendLine();
        source.AppendLine($"    $StageEncoderCountsPerUnit = {Format(options.ExternalEncoderCountsPerUnit)}");
        source.AppendLine($"    $rglobal[{MonitorStagePositionGlobalRealIndex}] = 0");
        source.AppendLine($"    $rglobal[{MonitorStageSpeedGlobalRealIndex}] = 0");
        source.AppendLine($"    $rglobal[{MonitorStageTargetGlobalRealIndex}] = 0");
        source.AppendLine($"    $iglobal[{MonitorCurrentSequenceGlobalIntegerIndex}] = 0");
        source.AppendLine($"    $iglobal[{MonitorTotalTargetsGlobalIntegerIndex}] = {orderedCommands.Length}");
        source.AppendLine();
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

        foreach (var head in headNumbers)
        {
            var axes = axesByHead[head];
            source.AppendLine($"    WaitForInPosition([{axes.X}, {axes.Y}])");
            source.AppendLine($"    GalvoEncoderScaleFactorSet({axes.Y}, 0)");
            source.AppendLine($"    DriveSetAuxiliaryFeedback({axes.Y}, 0)");
            source.AppendLine(
                $"    $EncoderScaleGYH{head} = ParameterGetAxisValue({axes.Y}, AxisParameter.CountsPerUnit) / $StageEncoderCountsPerUnit");
            source.AppendLine(
                $"    GalvoEncoderScaleFactorSet({axes.Y}, {Format(options.ExternalEncoderDirectionSign)} * $EncoderScaleGYH{head})");
            source.AppendLine($"    SetupAxisSpeed({axes.X}, {Format(options.ScannerRapidSpeed)})");
            source.AppendLine($"    SetupAxisSpeed({axes.Y}, {Format(options.ScannerRapidSpeed)})");
            source.AppendLine(
                $"    MoveRapid([{axes.X}, {axes.Y}], [0, 0], " +
                $"[{Format(options.ScannerRapidSpeed)}, {Format(options.ScannerRapidSpeed)}])");
        }

        source.AppendLine();
        AppendSoftwareLimits(source, allAxes, options);
        for (var groupIndex = 0; groupIndex < localYGroups.Length; groupIndex++)
        {
            var group = localYGroups[groupIndex];
            var threshold = options.AuxiliaryInitialWaitDistance + Math.Max(0, group.Key - firstLocalY);
            source.AppendLine($"    // AK1 -> AK2 AUX gate {groupIndex + 1}: external Stage travel > {Format(threshold)} mm");
            foreach (var head in group.Select(command => command.ScannerIndex).Distinct().OrderBy(index => index))
            {
                source.AppendLine(
                    $"    wait(Abs(StatusGetAxisItem({axesByHead[head].Y}, AxisStatusItem.AuxiliaryFeedback)) " +
                    $"> $StageEncoderCountsPerUnit * {Format(threshold)})");
            }

            foreach (var command in group)
            {
                var axes = axesByHead[command.ScannerIndex];
                source.AppendLine($"    $rglobal[{MonitorStagePositionGlobalRealIndex}] = {Format(threshold)}");
                var monitorSequence = monitorSequenceByMof[command.MofSequence];
                source.AppendLine($"    $iglobal[{MonitorCurrentSequenceGlobalIntegerIndex}] = {monitorSequence}");
                source.AppendLine($"    $iglobal[{MonitorTotalTargetsGlobalIntegerIndex}] = {orderedCommands.Length}");
                source.AppendLine(
                    $"    MoveRapid([{axes.X}, {axes.Y}], [{Format(command.Gx)}, {Format(command.Gy)}], " +
                    $"[{Format(options.ScannerRapidSpeed)}, {Format(options.ScannerRapidSpeed)}]) " +
                    $"// {command.CellIndex}, H{command.ScannerIndex}, MOF #{command.MofSequence}");
                source.AppendLine($"    MoveDelay([{axes.X}, {axes.Y}], {Format(options.MoveDelayMilliseconds)})");
            }

            source.AppendLine();
        }

        source.AppendLine($"    WaitForMotionDone([{string.Join(", ", allAxes)}])");
        foreach (var head in headNumbers)
        {
            source.AppendLine($"    GalvoEncoderScaleFactorSet({axesByHead[head].Y}, 0)");
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
        source.AppendLine($"    $iglobal[{MonitorCurrentSequenceGlobalIntegerIndex}] = 0");
        source.AppendLine($"    $iglobal[{MonitorTotalTargetsGlobalIntegerIndex}] = {commands.Count}");
        source.AppendLine();

        var orderedHardwareCommands = commands.OrderBy(command => command.MofSequence).ToArray();
        for (var commandIndex = 0; commandIndex < orderedHardwareCommands.Length; commandIndex++)
        {
            var command = orderedHardwareCommands[commandIndex];
            source.AppendLine($"    $iglobal[{MonitorCurrentSequenceGlobalIntegerIndex}] = {commandIndex + 1}");
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
        source.AppendLine($"// Generator revision: {GeneratorRevision}");
        source.AppendLine($"// Generated UTC: {DateTimeOffset.UtcNow:O}");
        source.AppendLine($"// Generation mode: {options.Mode}");
        source.AppendLine($"// ONLINE_PPID_NAME: {input.Pp.OnlinePpidName}");
        source.AppendLine($"// PP recipe: StageSpeed={input.Pp.StageSpeed}, LaserPower={input.Pp.LaserPower}, LaserFrequency={input.Pp.LaserFrequency}, ShotCount={input.Pp.ShotCount}");
        source.AppendLine($"// PP geometry: MaxCellNumber={input.Pp.MaxCellNumber}, NumOfPixel=({input.Pp.NumOfPixelX}, {input.Pp.NumOfPixelY}), Pitch={input.Pp.Pitch}, Chess={input.Pp.Chess}, DOE beams={input.Pp.SplitedBeamCount}");
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
            ["External encoder counts per unit"] = options.ExternalEncoderCountsPerUnit,
            ["Virtual Stage tick"] = options.VirtualStageTickSeconds
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
            options.SoftwareLimitHigh,
            options.ExternalEncoderDirectionSign,
            options.AuxiliaryInitialWaitDistance
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

        if (options.ExternalEncoderDirectionSign is not (-1 or 1) || options.AuxiliaryInitialWaitDistance < 0)
        {
            throw new InvalidOperationException(
                "External encoder direction must be -1 or 1, and AUX initial wait distance must be zero or greater.");
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

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex AxisIdentifierRegex();

    [GeneratedRegex(@"(?m)^\s*G\d+[^\r\n]*\b[A-Za-z_][A-Za-z0-9_]*\s+\$[A-Za-z_]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GCodeVariableWhitespaceRegex();
}
