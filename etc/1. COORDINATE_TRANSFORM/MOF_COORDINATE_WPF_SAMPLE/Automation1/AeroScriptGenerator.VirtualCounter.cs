using System.Text;
using MofCoordinateDemo.Models;

namespace MofCoordinateDemo.Automation1;

public sealed partial class AeroScriptGenerator
{
    private static string GenerateVirtualCounterSimulation(
        CoordinateInput input,
        IReadOnlyList<CellCommand> commands,
        AeroScriptGenerationOptions options)
    {
        var headNumbers = commands.Select(command => command.ScannerIndex).Distinct().ToArray();
        if (headNumbers.Length != 1)
        {
            throw new InvalidOperationException(
                "Virtual counter simulation validates one scanner GX/GY pair at a time. Select exactly one scanner head.");
        }

        var head = headNumbers[0];
        var axisX = ResolveAxisName(options.AxisXTemplate, head);
        var axisY = ResolveAxisName(options.AxisYTemplate, head);
        var orderedCommands = commands.OrderBy(command => command.MofSequence).ToArray();
        var firstLocalY = orderedCommands[0].LocalY;
        var stageDirection = options.StageTravelDistance >= 0 ? 1.0 : -1.0;

        var source = CreateHeader(input, commands, options);
        source.AppendLine("// MODE: Automation1 software Stage counter simulation");
        source.AppendLine("// No Stage axis is created or required. Hardware AUX, Laser, PSO and Galvo calibration are excluded.");
        source.AppendLine("// Simulation-safe substitutes are used instead of hardware-only calls such as GalvoLaserOutput().");
        source.AppendLine("// $rglobal[0]=simulated Stage Y, $rglobal[1]=speed, $rglobal[2]=next target Y");
        source.AppendLine("// $iglobal[0]=current MOF sequence, $iglobal[1]=total MOF targets");
        source.AppendLine("// $iglobal[2]=simulated laser state, $iglobal[3]=simulated laser pulse count");
        source.AppendLine("// Processing order follows board-local AK1 -> AK2 during reverse transport.");
        source.AppendLine();
        source.AppendLine("program");
        source.AppendLine("    var $ScannerXAxisName as string");
        source.AppendLine("    var $ScannerYAxisName as string");
        source.AppendLine("    var $ScannerXAxis as axis");
        source.AppendLine("    var $ScannerYAxis as axis");
        source.AppendLine("    var $VirtualStageDirection as real");
        source.AppendLine();
        source.AppendLine($"    $ScannerXAxisName = \"{axisX}\"");
        source.AppendLine($"    $ScannerYAxisName = \"{axisY}\"");
        source.AppendLine("    $ScannerXAxis = @$ScannerXAxisName");
        source.AppendLine("    $ScannerYAxis = @$ScannerYAxisName");
        source.AppendLine($"    $VirtualStageDirection = {Format(stageDirection)}");
        source.AppendLine($"    $rglobal[{MonitorStagePositionGlobalRealIndex}] = {Format(options.StartYPosition)}");
        source.AppendLine($"    $rglobal[{MonitorStageSpeedGlobalRealIndex}] = {Format(options.StageSpeed)}");
        source.AppendLine($"    $rglobal[{MonitorStageTargetGlobalRealIndex}] = {Format(options.StartYPosition)}");
        source.AppendLine($"    $iglobal[{MonitorCurrentSequenceGlobalIntegerIndex}] = 0");
        source.AppendLine($"    $iglobal[{MonitorTotalTargetsGlobalIntegerIndex}] = {orderedCommands.Length}");
        source.AppendLine($"    $iglobal[{MonitorLaserStateGlobalIntegerIndex}] = 0");
        source.AppendLine($"    $iglobal[{MonitorLaserPulseCountGlobalIntegerIndex}] = 0");
        source.AppendLine();
        AppendCommonMotionSetup(source, new[] { "$ScannerXAxis", "$ScannerYAxis" }, options);

        if (options.EnableAxes)
        {
            source.AppendLine("    Enable([$ScannerXAxis, $ScannerYAxis])");
            source.AppendLine();
        }

        source.AppendLine($"    SetupAxisSpeed($ScannerXAxis, {Format(options.ScannerRapidSpeed)})");
        source.AppendLine($"    SetupAxisSpeed($ScannerYAxis, {Format(options.ScannerRapidSpeed)})");
        source.AppendLine(
            $"    MoveRapid([$ScannerXAxis, $ScannerYAxis], [0, 0], " +
            $"[{Format(options.ScannerRapidSpeed)}, {Format(options.ScannerRapidSpeed)}])");
        source.AppendLine("    WaitForInPosition([$ScannerXAxis, $ScannerYAxis])");
        source.AppendLine();
        AppendSoftwareLimits(source, new[] { "$ScannerXAxis", "$ScannerYAxis" }, options);

        for (var commandIndex = 0; commandIndex < orderedCommands.Length; commandIndex++)
        {
            var command = orderedCommands[commandIndex];
            var monitorSequence = commandIndex + 1;
            var travelFromAk1 = options.AuxiliaryInitialWaitDistance + Math.Max(0, command.LocalY - firstLocalY);
            var targetY = options.StartYPosition + stageDirection * travelFromAk1;
            source.AppendLine($"    // AK1 -> AK2: {command.CellIndex}, reverse MOF #{command.MofSequence}");
            source.AppendLine($"    $rglobal[{MonitorStageTargetGlobalRealIndex}] = {Format(targetY)}");
            source.AppendLine(
                $"    AdvanceVirtualStageCounter($rglobal[{MonitorStageTargetGlobalRealIndex}], " +
                $"$rglobal[{MonitorStageSpeedGlobalRealIndex}], {Format(options.VirtualStageTickSeconds)}, $VirtualStageDirection)");
            source.AppendLine($"    $iglobal[{MonitorCurrentSequenceGlobalIntegerIndex}] = {monitorSequence}");
            source.AppendLine(
                $"    MoveRapid([$ScannerXAxis, $ScannerYAxis], [{Format(command.Gx)}, {Format(command.Gy)}], " +
                $"[{Format(options.ScannerRapidSpeed)}, {Format(options.ScannerRapidSpeed)}]) " +
                $"// Process Gx/Gy, LocalY={Format(command.LocalY)}");
            source.AppendLine("    SimulatedGalvoLaserOutput(1)");
            source.AppendLine($"    MoveDelay([$ScannerXAxis, $ScannerYAxis], {Format(options.MoveDelayMilliseconds)})");
            source.AppendLine("    SimulatedGalvoLaserOutput(0)");
            source.AppendLine();
        }

        source.AppendLine("    WaitForMotionDone([$ScannerXAxis, $ScannerYAxis])");
        source.AppendLine("    VelocityBlendingOff()");
        if (options.DisableAxesAtEnd)
        {
            source.AppendLine("    Disable([$ScannerXAxis, $ScannerYAxis])");
        }

        source.AppendLine("end");
        source.AppendLine();
        AppendVirtualStageCounterFunction(source);
        source.AppendLine();
        AppendSimulatedGalvoLaserFunction(source);
        return source.ToString();
    }

    private static void AppendVirtualStageCounterFunction(StringBuilder source)
    {
        source.AppendLine("function AdvanceVirtualStageCounter($targetY as real, $speed as real, $tick as real, $direction as real)");
        source.AppendLine("    if $direction > 0");
        source.AppendLine($"        while $rglobal[{MonitorStagePositionGlobalRealIndex}] < $targetY");
        source.AppendLine("            Dwell($tick)");
        source.AppendLine($"            $rglobal[{MonitorStagePositionGlobalRealIndex}] = $rglobal[{MonitorStagePositionGlobalRealIndex}] + $speed * $tick");
        source.AppendLine($"            if $rglobal[{MonitorStagePositionGlobalRealIndex}] > $targetY");
        source.AppendLine($"                $rglobal[{MonitorStagePositionGlobalRealIndex}] = $targetY");
        source.AppendLine("            end");
        source.AppendLine("        end");
        source.AppendLine("    else");
        source.AppendLine($"        while $rglobal[{MonitorStagePositionGlobalRealIndex}] > $targetY");
        source.AppendLine("            Dwell($tick)");
        source.AppendLine($"            $rglobal[{MonitorStagePositionGlobalRealIndex}] = $rglobal[{MonitorStagePositionGlobalRealIndex}] - $speed * $tick");
        source.AppendLine($"            if $rglobal[{MonitorStagePositionGlobalRealIndex}] < $targetY");
        source.AppendLine($"                $rglobal[{MonitorStagePositionGlobalRealIndex}] = $targetY");
        source.AppendLine("            end");
        source.AppendLine("        end");
        source.AppendLine("    end");
        source.AppendLine("end");
    }

    private static void AppendSimulatedGalvoLaserFunction(StringBuilder source)
    {
        source.AppendLine("function SimulatedGalvoLaserOutput($state as real)");
        source.AppendLine($"    $iglobal[{MonitorLaserStateGlobalIntegerIndex}] = $state");
        source.AppendLine("    if $state > 0");
        source.AppendLine($"        $iglobal[{MonitorLaserPulseCountGlobalIntegerIndex}] = $iglobal[{MonitorLaserPulseCountGlobalIntegerIndex}] + 1");
        source.AppendLine("    end");
        source.AppendLine("end");
    }
}
