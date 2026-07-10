using System.Globalization;
using System.Text;
using Drilling.Common.Station;

namespace Drilling.File.Script;

public sealed class CAutomation1ScriptFile : IAutomationScriptFile
{
    public const string DefaultScriptFileName = "PROCESS.ascript";

    private readonly string _scriptDirectory;

    public CAutomation1ScriptFile(string? scriptDirectory = null)
    {
        _scriptDirectory = string.IsNullOrWhiteSpace(scriptDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data", "Script")
            : scriptDirectory;
    }

    public string ScriptFileName => DefaultScriptFileName;

    public IAutomation1Script Create(string? fileName = null)
    {
        return new CAutomation1Script(
            _scriptDirectory,
            NormalizeFileName(fileName));
    }

    public async Task<ST_AUTOMATION1_SCRIPT> Build(
        ST_PROCESS_MODEL processModel,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_scriptDirectory);

        var createdAt = DateTimeOffset.Now;
        var lines = BuildLines(processModel, createdAt);
        var filePath = Path.Combine(_scriptDirectory, ScriptFileName);

        await System.IO.File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8, cancellationToken);

        return new ST_AUTOMATION1_SCRIPT(
            ScriptFileName,
            filePath,
            lines,
            processModel.Heads.Where(head => head.Use).Sum(head => head.Path.Count),
            processModel.Heads.Count(head => head.Use),
            createdAt);
    }

    private static IReadOnlyList<string> BuildLines(
        ST_PROCESS_MODEL processModel,
        DateTimeOffset createdAt)
    {
        var lines = new List<string>();
        var common = new Dictionary<string, string>(
            processModel.Parameters,
            StringComparer.OrdinalIgnoreCase);

        AppendScriptStart(lines, processModel, createdAt);
        AppendDefaultSetting(lines, common);

        foreach (var head in processModel.Heads.Where(head => head.Use && head.Path.Count > 0))
        {
            AppendHeadProcess(lines, common, head);
        }

        AppendScriptEnd(lines, common);

        return lines;
    }

    private static void AppendScriptStart(
        List<string> lines,
        ST_PROCESS_MODEL processModel,
        DateTimeOffset createdAt)
    {
        var plan = processModel.Plan;

        lines.Add("// Laser Drilling Automation1 process script");
        lines.Add($"// ProcessId={plan.ProcessId}");
        lines.Add($"// RecipeId={plan.RecipeId}");
        lines.Add($"// ProductId={plan.ProductId}");
        lines.Add($"// PanelId={plan.PanelId}");
        lines.Add($"// CreatedAt={createdAt:yyyy-MM-dd HH:mm:ss.fff zzz}");
        lines.Add("program");
    }

    private static void AppendDefaultSetting(
        List<string> lines,
        IReadOnlyDictionary<string, string> parameters)
    {
        var scannerAcc = ReadDouble(
            parameters,
            500000.0,
            "SCANNER_ACC",
            "MAKE_SCANNER_ACC",
            "SCAN_ACCEL",
            "ScannerAcc");
        var motionUpdateRate = ReadInt(
            parameters,
            0,
            "MOTION_UPDATE_RATE",
            "SCAN_MOTION_UPDATE_RATE",
            "MotionUpdateRate");

        lines.Add("IfovOff()");
        lines.Add("SetupTaskDistanceUnits(DistanceUnits.Primary)");
        lines.Add("SetupTaskTimeUnits(TimeUnits.Seconds)");
        lines.Add("SetupCoordinatedRampType(RampType.Sine)");
        lines.Add($"SetupCoordinatedRampValue(RampMode.Rate, {Format(scannerAcc)})");

        if (motionUpdateRate > 0)
        {
            lines.Add($"ParameterSetTaskValue(TaskGetIndex(), TaskParameter.MotionUpdateRate, {motionUpdateRate})");
        }

        lines.Add("");
    }

    private static void AppendHeadProcess(
        List<string> lines,
        IReadOnlyDictionary<string, string> common,
        ST_HEAD_PROCESS_DATA head)
    {
        var xAxis = ReadText(
            common,
            "X",
            CreateHeadKeys(head.HeadNo, "X_AXIS", "GX_AXIS", "SCANNER_X_AXIS", "GALVO_X_AXIS"));
        var yAxis = ReadText(
            common,
            "Y",
            CreateHeadKeys(head.HeadNo, "Y_AXIS", "GY_AXIS", "SCANNER_Y_AXIS", "GALVO_Y_AXIS"));
        var laserAxis = ReadText(
            common,
            xAxis,
            CreateHeadKeys(head.HeadNo, "LASER_AXIS", "LASER_CTRL_AXIS", "GALVO_CTRL_AXIS"));

        lines.Add($"// HEAD {head.HeadNo:00} / Shape={head.Shape} / Points={head.Path.Count}");
        AppendLaserSetting(lines, common, head, laserAxis);
        AppendSpeedSetting(lines, head, xAxis, yAxis);
        AppendPath(lines, head, xAxis, yAxis);
        AppendHeadEnd(lines, common, head, laserAxis);
        lines.Add("");
    }

    private static void AppendLaserSetting(
        List<string> lines,
        IReadOnlyDictionary<string, string> common,
        ST_HEAD_PROCESS_DATA head,
        string laserAxis)
    {
        var outputRate = ReadDouble(
            common,
            100.0,
            "LASER_OUTPUT_RATE",
            "LaserOutputRate");
        var laserPower = head.LaserPower;
        var analogOutputUse = ReadBool(
            common,
            false,
            "LASER_ANALOG_OUTPUT_USE",
            "ANALOG_OUTPUT_USE");
        var laserMode = ReadInt(
            common,
            0,
            CreateHeadKeys(head.HeadNo, "LASER_MODE", "SCAN_LASER_MODE"));
        var onDelay = ReadDouble(
            common,
            0.0,
            CreateHeadKeys(head.HeadNo, "LASER_ON_DELAY", "SCAN_LASER_ON_DELAY"));
        var offDelay = ReadDouble(
            common,
            0.0,
            CreateHeadKeys(head.HeadNo, "LASER_OFF_DELAY", "SCAN_LASER_OFF_DELAY"));

        lines.Add($"GalvoConfigureLaserOutputPeriod({laserAxis}, {Format(CalculateLaserPeriod(head.FrequencyKhz))})");
        lines.Add($"GalvoConfigureLaser1PulseWidth({laserAxis}, {Format(laserPower)})");
        lines.Add($"GalvoConfigureLaserMode({laserAxis}, {laserMode})");

        if (onDelay > 0.0 ||
            offDelay > 0.0)
        {
            lines.Add($"GalvoConfigureLaserDelays({laserAxis}, {Format(onDelay / 2.0)}, {Format(offDelay / 2.0)})");
        }

        if (analogOutputUse)
        {
            var voltage = (laserPower / 100.0) * 10.0 * (outputRate / 100.0);
            lines.Add($"AnalogOutputSet({laserAxis}, 0, {Format(voltage)})");
        }

        AppendPsoSetting(lines, common, head, laserAxis);
        lines.Add($"GalvoLaserOutput({laserAxis}, GalvoLaser.Auto)");
    }

    private static void AppendPsoSetting(
        List<string> lines,
        IReadOnlyDictionary<string, string> common,
        ST_HEAD_PROCESS_DATA head,
        string laserAxis)
    {
        if (!ReadBool(
            common,
            false,
            CreateHeadKeys(head.HeadNo, "PSO_USE", "SCAN_PSO_USE", "SCAN_PSO_CONTROL_USE")))
        {
            return;
        }

        var pulseDistance = ReadDouble(
            common,
            0.0,
            CreateHeadKeys(head.HeadNo, "PSO_PULSE_DISTANCE", "SCAN_PSO_PULSE_DISTANCE"));
        var totalTime = ReadDouble(
            common,
            0.0,
            CreateHeadKeys(head.HeadNo, "PSO_PULSE_TOTAL_TIME", "SCAN_PSO_PULSE_TOTAL_TIME"));
        var onTime = ReadDouble(
            common,
            0.0,
            CreateHeadKeys(head.HeadNo, "PSO_PULSE_ON_TIME", "SCAN_PSO_PULSE_LASER_ON_TIME"));
        var delay = ReadDouble(
            common,
            0.0,
            CreateHeadKeys(head.HeadNo, "PSO_PULSE_DELAY", "SCAN_PSO_PULSE_TIME_DELAY"));

        lines.Add($"PsoDistanceConfigureInputs({laserAxis}, [PsoDistanceInput.GL4PrimaryFeedbackAxis1Encoder0, PsoDistanceInput.GL4IfovFeedbackAxis2])");

        if (pulseDistance > 0.0)
        {
            lines.Add($"PsoDistanceConfigureFixedDistance({laserAxis}, Round(UnitsToCounts({laserAxis}, {Format(pulseDistance)})))");
        }

        lines.Add($"PsoDistanceCounterOn({laserAxis})");
        lines.Add($"PsoDistanceEventsOn({laserAxis})");
        lines.Add($"PsoDistanceConfigureCounterReset({laserAxis}, PsoDistanceCounterResetMask.ResetWhenLaserOff)");
        lines.Add($"PsoWaveformConfigureMode({laserAxis}, PsoWaveformMode.Pulse)");

        if (totalTime > 0.0)
        {
            lines.Add($"PsoWaveformConfigurePulseFixedTotalTime({laserAxis}, {Format(totalTime)})");
        }

        if (onTime > 0.0)
        {
            lines.Add($"PsoWaveformConfigurePulseFixedOnTime({laserAxis}, {Format(onTime)})");
        }

        if (delay > 0.0)
        {
            lines.Add($"PsoWaveformConfigureDelay({laserAxis}, {Format(delay)})");
        }

        lines.Add($"PsoWaveformConfigurePulseFixedCount({laserAxis}, 1)");
        lines.Add($"PsoWaveformApplyPulseConfiguration({laserAxis})");
        lines.Add($"PsoWaveformOn({laserAxis})");
        lines.Add($"PsoOutputConfigureSource({laserAxis}, PsoOutputSource.Waveform)");
        lines.Add($"PsoOutputConfigureOutput({laserAxis}, PsoOutputPin.GL4LaserOutput0)");
        lines.Add($"PsoEventConfigureMask({laserAxis}, PsoEventMask.LaserMask)");
    }

    private static void AppendSpeedSetting(
        List<string> lines,
        ST_HEAD_PROCESS_DATA head,
        string xAxis,
        string yAxis)
    {
        lines.Add($"SetupAxisSpeed({xAxis}, {Format(head.JumpSpeed * 1000.0)})");
        lines.Add($"SetupAxisSpeed({yAxis}, {Format(head.JumpSpeed * 1000.0)})");
        lines.Add($"SetupCoordinatedSpeed({Format(head.MarkSpeed * 1000.0)})");
    }

    private static void AppendPath(
        List<string> lines,
        ST_HEAD_PROCESS_DATA head,
        string xAxis,
        string yAxis)
    {
        var firstPoint = ApplyOffset(head.Path[0], head);
        lines.Add($"MoveRapid([{xAxis}, {yAxis}], [{Format(firstPoint.X)}, {Format(firstPoint.Y)}])");

        foreach (var rawPoint in head.Path.Skip(1))
        {
            var point = ApplyOffset(rawPoint, head);
            var command = point.LaserOn ? "MoveLinear" : "MoveRapid";
            lines.Add($"{command}([{xAxis}, {yAxis}], [{Format(point.X)}, {Format(point.Y)}])");
        }
    }

    private static void AppendHeadEnd(
        List<string> lines,
        IReadOnlyDictionary<string, string> common,
        ST_HEAD_PROCESS_DATA head,
        string laserAxis)
    {
        if (ReadBool(
            common,
            false,
            CreateHeadKeys(head.HeadNo, "PSO_USE", "SCAN_PSO_USE", "SCAN_PSO_CONTROL_USE")))
        {
            lines.Add($"PsoWaveformOff({laserAxis})");
        }
    }

    private static void AppendScriptEnd(
        List<string> lines,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (ReadBool(parameters, false, "A1_BUFFERED_RUN", "USE_A1_BUFFERED_RUN"))
        {
            lines.Add("CommandQueueStop()");
        }

        lines.Add("end");
    }

    private static ST_PATH_POINT ApplyOffset(
        ST_PATH_POINT point,
        ST_HEAD_PROCESS_DATA head)
    {
        return point with
        {
            X = point.X + head.OffsetX,
            Y = point.Y + head.OffsetY
        };
    }

    private static double CalculateLaserPeriod(double frequencyKhz)
    {
        return frequencyKhz <= 0.0
            ? 0.0
            : 1.0 / (frequencyKhz / 1000.0);
    }

    private static IReadOnlyList<string> CreateHeadKeys(
        int headNo,
        params string[] names)
    {
        var keys = new List<string>();

        foreach (var name in names)
        {
            keys.Add($"H{headNo:00}_{name}");
            keys.Add($"HEAD{headNo:00}_{name}");
            keys.Add(name);
        }

        return keys;
    }

    private static string ReadText(
        IReadOnlyDictionary<string, string> parameters,
        string defaultValue,
        IEnumerable<string> keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (parameters.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return defaultValue;
    }

    private static double ReadDouble(
        IReadOnlyDictionary<string, string> parameters,
        double defaultValue,
        params string[] keys)
    {
        return ReadDouble(parameters, defaultValue, (IEnumerable<string>)keys);
    }

    private static double ReadDouble(
        IReadOnlyDictionary<string, string> parameters,
        double defaultValue,
        IEnumerable<string> keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (parameters.TryGetValue(key, out var value) &&
                double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    private static int ReadInt(
        IReadOnlyDictionary<string, string> parameters,
        int defaultValue,
        params string[] keys)
    {
        return ReadInt(parameters, defaultValue, (IEnumerable<string>)keys);
    }

    private static int ReadInt(
        IReadOnlyDictionary<string, string> parameters,
        int defaultValue,
        IEnumerable<string> keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (parameters.TryGetValue(key, out var value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    private static bool ReadBool(
        IReadOnlyDictionary<string, string> parameters,
        bool defaultValue,
        params string[] keys)
    {
        return ReadBool(parameters, defaultValue, (IEnumerable<string>)keys);
    }

    private static bool ReadBool(
        IReadOnlyDictionary<string, string> parameters,
        bool defaultValue,
        IEnumerable<string> keys)
    {
        foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (!parameters.TryGetValue(key, out var value) ||
                string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                return intValue != 0;
            }

            if (value.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("USE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.Equals("N", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("NO", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("OFF", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("UNUSE", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return defaultValue;
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return DefaultScriptFileName;
        }

        var normalized = Path.GetFileName(fileName.Trim());

        return Path.HasExtension(normalized)
            ? normalized
            : $"{normalized}.ascript";
    }

    private sealed class CAutomation1Script : IAutomation1Script
    {
        private readonly string _scriptDirectory;
        private readonly List<string> _lines = [];
        private string _xAxis = "X";
        private string _yAxis = "Y";
        private string _laserAxis = "X";
        private string _stageXAxis = "STAGE_X";
        private string _stageYAxis = "STAGE_Y";
        private int _deviceNo;
        private int _pointCount;
        private double _currentX;
        private double _currentY;
        private double _laserOutputPeriod;
        private double _jumpSpeed;
        private double _markSpeed;
        private double _tactTime;
        private bool _nMarkDriveLaserControl;
        private bool _scanPlannerStageEncoderMode;
        private DateTimeOffset _createdAt = DateTimeOffset.Now;

        public CAutomation1Script(
            string scriptDirectory,
            string fileName)
        {
            _scriptDirectory = scriptDirectory;
            FileName = fileName;
        }

        public string FileName { get; }

        public string FilePath => Path.Combine(_scriptDirectory, FileName);

        public IReadOnlyList<string> Lines => _lines;

        public void Clear()
        {
            _lines.Clear();
            _pointCount = 0;
            _currentX = 0.0;
            _currentY = 0.0;
            _createdAt = DateTimeOffset.Now;
            _xAxis = "X";
            _yAxis = "Y";
            _laserAxis = "X";
            _stageXAxis = "STAGE_X";
            _stageYAxis = "STAGE_Y";
            _deviceNo = 0;
            _laserOutputPeriod = 0.0;
            _jumpSpeed = 0.0;
            _markSpeed = 0.0;
            _tactTime = 0.0;
            _nMarkDriveLaserControl = false;
            _scanPlannerStageEncoderMode = false;
        }

        public void AddLine(string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _lines.Add(line);
            }
        }

        public void Start(string title = "")
        {
            _createdAt = DateTimeOffset.Now;

            if (!string.IsNullOrWhiteSpace(title))
            {
                _lines.Add($"// {title.Trim()}");
            }

            _lines.Add($"// CreatedAt={_createdAt:yyyy-MM-dd HH:mm:ss.fff zzz}");
            _lines.Add("program");
        }

        public void SetDeviceNo(int deviceNo)
        {
            _deviceNo = Math.Max(0, deviceNo);
        }

        public void SetNMarkDriveLaserControl(bool use)
        {
            _nMarkDriveLaserControl = use;
        }

        public void SetScanPlannerStageEncoderMode(bool use)
        {
            _scanPlannerStageEncoderMode = use;
        }

        public void DefaultSetting(
            double scannerAcc = 500000.0,
            int motionUpdateRate = 0,
            int executeLineCount = 110,
            bool resetPso = true)
        {
            _lines.Add("SetupTaskDistanceUnits(DistanceUnits.Primary)");
            _lines.Add("SetupTaskTimeUnits(TimeUnits.Seconds)");
            SetMoveBlending(true);
            SetWaitModeAuto();
            SetAbsoluteMode();
            SetScannerAcc(scannerAcc);

            if (!_nMarkDriveLaserControl)
            {
                _lines.Add($"GalvoConfigureLaserOutputPeriod({_laserAxis}, 0)");
                _lines.Add($"GalvoConfigureLaser1PulseWidth({_laserAxis}, 0)");
                SetLaserMode(0);
            }

            if (motionUpdateRate > 0)
            {
                SetMoveUpdateRate(motionUpdateRate);
            }

            SetExecuteLineCount(executeLineCount);

            if (resetPso &&
                !_nMarkDriveLaserControl)
            {
                _lines.Add($"PsoReset({_laserAxis})");
            }
        }

        public void SetAxis(
            string xAxis,
            string yAxis,
            string? laserAxis = null)
        {
            _xAxis = NormalizeAxis(xAxis, "X");
            _yAxis = NormalizeAxis(yAxis, "Y");
            _laserAxis = NormalizeAxis(laserAxis, _xAxis);
        }

        public void SetStageAxis(
            string xAxis,
            string yAxis)
        {
            _stageXAxis = NormalizeAxis(xAxis, "STAGE_X");
            _stageYAxis = NormalizeAxis(yAxis, "STAGE_Y");
        }

        public void SetFrequency(double frequencyKhz)
        {
            _laserOutputPeriod = CalculateLaserPeriod(frequencyKhz);

            if (!_nMarkDriveLaserControl)
            {
                _lines.Add($"GalvoConfigureLaserOutputPeriod({_laserAxis}, {Format(_laserOutputPeriod)})");
            }
        }

        public void SetLaserPower(
            double powerPercent,
            double outputRate = 100.0,
            bool analogOutputUse = false)
        {
            if (!_nMarkDriveLaserControl)
            {
                SetMoveDelay(100, false);
            }

            var voltage = (powerPercent / 100.0) * 10.0 * (outputRate / 100.0);
            _lines.Add($"AnalogOutputSet({_laserAxis}, 0, {Format(voltage)})");

            if (!_nMarkDriveLaserControl)
            {
                var pulseWidth = _laserOutputPeriod * (powerPercent / 100.0) * (outputRate / 100.0);
                _lines.Add($"GalvoConfigureLaser1PulseWidth({_laserAxis}, {Format(pulseWidth)})");
            }
        }

        public void SetPulseOnTimeLaserPower(
            double powerPercent,
            double dutyPercent,
            double outputRate = 100.0)
        {
            if (!_nMarkDriveLaserControl)
            {
                SetMoveDelay(100, false);
            }

            var voltage = (powerPercent / 100.0) * 10.0 * (outputRate / 100.0);
            _lines.Add($"AnalogOutputSet({_laserAxis}, 0, {Format(voltage)})");

            if (!_nMarkDriveLaserControl)
            {
                var pulseWidth = _laserOutputPeriod * (dutyPercent / 100.0);
                _lines.Add($"GalvoConfigureLaser1PulseWidth({_laserAxis}, {Format(pulseWidth)})");
            }

            SetLaserMode(0);
        }

        public void SetLaserMode(int mode)
        {
            if (!_nMarkDriveLaserControl)
            {
                _lines.Add($"GalvoConfigureLaserMode({_laserAxis}, {mode})");
            }
        }

        public void SetLaserDelay(
            double onDelay,
            double offDelay)
        {
            if (onDelay <= 0.0 &&
                offDelay <= 0.0)
            {
                return;
            }

            _lines.Add($"GalvoConfigureLaserDelays({_laserAxis}, {Format(onDelay / 2.0)}, {Format(offDelay / 2.0)})");
        }

        public void SetJumpSpeed(double speedMmPerSec)
        {
            _lines.Add($"SetupAxisSpeed({_xAxis}, {Format(speedMmPerSec * 1000.0)})");
            _lines.Add($"SetupAxisSpeed({_yAxis}, {Format(speedMmPerSec * 1000.0)})");
            _jumpSpeed = speedMmPerSec;
        }

        public void SetJumpSpeedRate(
            double speedMmPerSec,
            double rate = 1.0)
        {
            if (rate < 1.0)
            {
                rate = 1.0;
            }

            _lines.Add($"SetupAxisSpeed({_xAxis}, {Format(speedMmPerSec * rate * 1000.0)})");
            _lines.Add($"SetupAxisSpeed({_yAxis}, {Format(speedMmPerSec * rate * 1000.0)})");
            _jumpSpeed = speedMmPerSec;
        }

        public void SetMarkSpeed(double speedMmPerSec)
        {
            _lines.Add($"SetupCoordinatedSpeed({Format(speedMmPerSec * 1000.0)})");
            _markSpeed = speedMmPerSec;
        }

        public void SetStageSpeed(
            double speedX,
            double speedY)
        {
            if (IsUsableAxis(_stageXAxis))
            {
                _lines.Add($"SetupAxisSpeed({_stageXAxis}, {Format(speedX)})");
            }

            if (IsUsableAxis(_stageYAxis))
            {
                _lines.Add($"SetupAxisSpeed({_stageYAxis}, {Format(speedY)})");
            }
        }

        public void SetScannerAcc(double acc)
        {
            if (acc <= 0.0)
            {
                return;
            }

            _lines.Add($"SetupAxisRampType([{_xAxis}, {_yAxis}], RampType.Sine)");
            _lines.Add($"SetupAxisRampValue([{_xAxis}, {_yAxis}], RampMode.Rate, {Format(acc)})");
            _lines.Add("SetupCoordinatedRampType(RampType.Sine)");
            _lines.Add($"SetupCoordinatedRampValue(RampMode.Rate, {Format(acc)})");
        }

        public void SetMarkAcc(double acc)
        {
            if (acc <= 0.0)
            {
                return;
            }

            _lines.Add("SetupCoordinatedRampType(RampType.Sine)");
            _lines.Add($"SetupCoordinatedRampValue(RampMode.Rate, {Format(acc)})");
        }

        public void SetIFOV(bool use)
        {
            _lines.Add(use ? "IfovOn()" : "IfovOff()");
        }

        public void SetIFOVEmulatedQuadratureDivider()
        {
            if (IsUsableAxis(_stageXAxis))
            {
                _lines.Add($"ParameterSetAxisValue({_stageXAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, 16)");
            }

            if (IsUsableAxis(_stageYAxis))
            {
                _lines.Add($"ParameterSetAxisValue({_stageYAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, 16)");
            }

            if (IsUsableAxis(_stageXAxis))
            {
                _lines.Add($"$rglobal[{_deviceNo}] = ParameterGetAxisValue({_xAxis}, AxisParameter.CountsPerUnit) / (ParameterGetAxisValue({_stageXAxis}, AxisParameter.CountsPerUnit) / ParameterGetAxisValue({_stageXAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider))");
                _lines.Add($"ParameterSetAxisValue({_xAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, $rglobal[{_deviceNo}])");
            }

            if (IsUsableAxis(_stageYAxis))
            {
                _lines.Add($"$rglobal[{_deviceNo + 1}] = ParameterGetAxisValue({_yAxis}, AxisParameter.CountsPerUnit) / (ParameterGetAxisValue({_stageYAxis}, AxisParameter.CountsPerUnit) / ParameterGetAxisValue({_stageYAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider))");
                _lines.Add($"ParameterSetAxisValue({_yAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, $rglobal[{_deviceNo + 1}])");
            }
        }

        public void SetIFOVIO(bool use = true)
        {
            if (use)
            {
                if (IsUsableAxis(_stageXAxis))
                {
                    _lines.Add($"DriveEncoderOutputConfigureInput({_stageXAxis}, 0, 0)");
                    _lines.Add($"DriveEncoderOutputConfigureDivider({_stageXAxis}, 0, 1)");
                    _lines.Add($"DriveEncoderOutputOn({_stageXAxis}, 0, 0)");
                }

                if (IsUsableAxis(_stageYAxis))
                {
                    _lines.Add($"DriveEncoderOutputConfigureInput({_stageYAxis}, 0, 0)");
                    _lines.Add($"DriveEncoderOutputConfigureDivider({_stageYAxis}, 0, 1)");
                    _lines.Add($"DriveEncoderOutputOn({_stageYAxis}, 0, 0)");
                }

                return;
            }

            if (IsUsableAxis(_stageXAxis))
            {
                _lines.Add($"DriveEncoderOutputOff({_stageXAxis}, 0)");
            }

            if (IsUsableAxis(_stageYAxis))
            {
                _lines.Add($"DriveEncoderOutputOff({_stageYAxis}, 0)");
            }
        }

        public void SetIFOVScaleXY()
        {
            if (IsUsableAxis(_stageXAxis))
            {
                _lines.Add($"$rglobal[{_deviceNo + 100}] = ParameterGetAxisValue({_xAxis}, AxisParameter.CountsPerUnit) / ParameterGetAxisValue({_stageXAxis}, AxisParameter.CountsPerUnit) * ParameterGetAxisValue({_stageXAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider)");
            }

            if (IsUsableAxis(_stageYAxis))
            {
                _lines.Add($"$rglobal[{_deviceNo + 101}] = ParameterGetAxisValue({_yAxis}, AxisParameter.CountsPerUnit) / ParameterGetAxisValue({_stageYAxis}, AxisParameter.CountsPerUnit) * ParameterGetAxisValue({_stageYAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider)");
            }
        }

        public void SetIFOVTime(long time)
        {
            _lines.Add($"IfovSetTime({time})");
        }

        public void SetIFOVSize(double size)
        {
            _lines.Add($"IfovSetSize({Format(size)})");
        }

        public void SetIFOVTrackingSpeed(long speed)
        {
            _lines.Add($"IfovSetTrackingSpeed({speed})");
        }

        public void SetIFOVTrackingAccel(long acc)
        {
            _lines.Add($"IfovSetTrackingAcceleration({acc})");
        }

        public void SetIFOVPair(
            string xStageAxis,
            string yStageAxis,
            bool xDirection,
            bool yDirection)
        {
            var scaleX = xDirection
                ? $"$rglobal[{_deviceNo + 100}]"
                : $"$rglobal[{_deviceNo + 100}]*-1";
            var scaleY = yDirection
                ? $"$rglobal[{_deviceNo + 101}]"
                : $"$rglobal[{_deviceNo + 101}]*-1";

            _lines.Add($"IfovSetAxisPairs([{_xAxis},{NormalizeAxis(xStageAxis, _stageXAxis)}],[{_yAxis},{NormalizeAxis(yStageAxis, _stageYAxis)}], {scaleX},{scaleY})");
        }

        public void SetIFOVSyncAxis()
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetIFOVSYNCAXIS.
        }

        public void SetMoveBlending(bool use)
        {
            _lines.Add(use ? "VelocityBlendingOn()" : "VelocityBlendingOff()");
        }

        public void SetAbsoluteMode()
        {
            _lines.Add("SetupTaskTargetMode(TargetMode.Absolute)");
        }

        public void SetIncrementalMode()
        {
            _lines.Add("SetupTaskTargetMode(TargetMode.Incremental)");
        }

        public void SetWaitModeAuto()
        {
            _lines.Add("SetupTaskWaitMode(WaitMode.Auto)");
        }

        public void SetMoveDelay(
            double delay,
            bool addTactTime = true)
        {
            if (delay < 0.001)
            {
                return;
            }

            if (delay / 100.0 <= 0.021)
            {
                delay = 2.1;
            }

            _lines.Add($"MoveDelay([{_xAxis},{_yAxis}], {Format(delay / 100.0)})");

            if (addTactTime)
            {
                _tactTime += delay / 100000.0;
            }
        }

        public void SetExecuteLineCount(int lineCount)
        {
            if (lineCount <= 0)
            {
                return;
            }

            _lines.Add($"ParameterSetTaskValue(TaskGetIndex(), TaskParameter.ExecuteNumLines, {lineCount})");
        }

        public void SetScannerRotate(double angle)
        {
            if (!_nMarkDriveLaserControl)
            {
                _lines.Add($"GalvoRotationSet({_laserAxis}, {Format(angle)})");
            }
        }

        public void SetScannerRotate(
            string laserAxis,
            double angle)
        {
            _lines.Add($"GalvoRotationSet({NormalizeAxis(laserAxis, _laserAxis)}, {Format(angle)})");
        }

        public void SetMoveUpdateRate(int rate)
        {
            _lines.Add($"ParameterSetTaskValue(TaskGetIndex(), TaskParameter.MotionUpdateRate, {rate})");
        }

        public void SetCoordinatedAccelLimit(
            long acc,
            long arcAcc)
        {
            _lines.Add($"SetupCoordinatedAccelLimit({acc},{arcAcc})");
        }

        public void SetTaskAccelLimit(
            long acc,
            long arcAcc)
        {
            _lines.Add($"ParameterSetTaskValue(TaskGetIndex(), TaskParameter.DefaultCoordinatedAccelLimit, {acc})");
            _lines.Add($"ParameterSetTaskValue(TaskGetIndex(), TaskParameter.DefaultCoordinatedCircularAccelLimit, {arcAcc})");
        }

        public void SetScanTrajectoryFIRFilterX(long delay)
        {
            _lines.Add($"ParameterSetAxisValue({_xAxis},AxisParameter.TrajectoryFirFilter, {delay})");
        }

        public void SetScanTrajectoryFIRFilterY(long delay)
        {
            _lines.Add($"ParameterSetAxisValue({_yAxis},AxisParameter.TrajectoryFirFilter, {delay})");
        }

        public void SetStageTrajectoryFIRFilterX(long delay)
        {
            if (IsUsableAxis(_stageXAxis))
            {
                _lines.Add($"ParameterSetAxisValue({_stageXAxis},AxisParameter.TrajectoryFirFilter, {delay})");
            }
        }

        public void SetStageTrajectoryFIRFilterY(long delay)
        {
            if (IsUsableAxis(_stageYAxis))
            {
                _lines.Add($"ParameterSetAxisValue({_stageYAxis},AxisParameter.TrajectoryFirFilter, {delay})");
            }
        }

        public void SetProjection(
            string axis,
            double offsetX,
            double offsetY,
            double offsetT)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetProjection.
        }

        public void SetProjectionOff(string axis)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetProjectionOFF.
        }

        public void SetGearing(
            string masterAxis,
            string slaveAxis)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetGearing.
        }

        public void SetGearingOff(string slaveAxis = "AUTO")
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetGearingOFF.
        }

        public void SetSoftwareLimitSetup(bool use = true)
        {
            if (use)
            {
                return;
            }

            _lines.Add($"ParameterSetAxisValue({_xAxis}, AxisParameter.SoftwareLimitSetup, 0)");
            _lines.Add($"ParameterSetAxisValue({_yAxis}, AxisParameter.SoftwareLimitSetup, 0)");
        }

        public void SetAerotechEncoderReset(
            string axisX,
            string axisY)
        {
            var x = NormalizeAxis(axisX, "NONE");
            var y = NormalizeAxis(axisY, "NONE");

            if (IsUsableAxis(x))
            {
                _lines.Add($"DriveSetAuxiliaryFeedback({x},0)");
            }

            if (IsUsableAxis(y))
            {
                _lines.Add($"DriveSetAuxiliaryFeedback({y},0)");
            }
        }

        public void SetScanPlannerStageEncoder(string stageAxis)
        {
            var axis = NormalizeAxis(stageAxis, "NONE");

            if (IsUsableAxis(axis))
            {
                _lines.Add($"$StageEncoder = ParameterGetAxisValue({axis}, AxisParameter.CountsPerUnit) / ParameterGetAxisValue({axis}, AxisParameter.PrimaryEmulatedQuadratureDivider)");
            }
        }

        public void SetEmulatedQuadratureDividerX(int value)
        {
            _lines.Add($"ParameterSetAxisValue({_xAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, {value})");
        }

        public void SetEmulatedQuadratureDividerY(int value)
        {
            _lines.Add($"ParameterSetAxisValue({_yAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, {value})");
        }

        public void SetStageEmulatedQuadratureDivider(
            int xValue,
            int yValue)
        {
            if (IsUsableAxis(_stageXAxis))
            {
                _lines.Add($"ParameterSetAxisValue({_stageXAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, {xValue})");
            }

            if (IsUsableAxis(_stageYAxis))
            {
                _lines.Add($"ParameterSetAxisValue({_stageYAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, {yValue})");
            }
        }

        public void SetPSO(
            double pulseDistance,
            double totalTime,
            double laserOnTime,
            double delay,
            EN_AEROTECH_MODE mode,
            EN_AEROTECH_PSO_MODE psoMode,
            double frequencyKhz,
            double powerPercent,
            int windowMaskDirection,
            double markSpeed,
            bool manual = false)
        {
            if (_nMarkDriveLaserControl)
            {
                return;
            }

            if (mode == EN_AEROTECH_MODE.Mof)
            {
                WaitMoveDone();
                _lines.Add($"$Cpu{_xAxis} = {Format(markSpeed * 1000.0)} * ParameterGetAxisValue({_xAxis}, AxisParameter.CountsPerUnit) / 15000000 ; pso max inputrate 15Mhz ; marking velocity 6000");
                _lines.Add($"$Cpu{_yAxis} = {Format(markSpeed * 1000.0)} * ParameterGetAxisValue({_yAxis}, AxisParameter.CountsPerUnit) / 15000000 ; pso max inputrate 15Mhz ; marking velocity 6000");
                _lines.Add($"ParameterSetAxisValue({_xAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, $Cpu{_xAxis})");
                _lines.Add($"ParameterSetAxisValue({_yAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, $Cpu{_yAxis})");
            }
            else if (mode == EN_AEROTECH_MODE.Ifov)
            {
                SetIFOVEmulatedQuadratureDivider();
            }
            else
            {
                WaitMoveDone();
                _lines.Add($"$rglobal[{_deviceNo}] = {Format(markSpeed * 1000.0)} * ParameterGetAxisValue({_xAxis}, AxisParameter.CountsPerUnit) / 15000000 ; pso max inputrate 15Mhz ; marking velocity 6000");
                _lines.Add($"$rglobal[{_deviceNo + 1}] = {Format(markSpeed * 1000.0)} * ParameterGetAxisValue({_yAxis}, AxisParameter.CountsPerUnit) / 15000000 ; pso max inputrate 15Mhz ; marking velocity 6000");
                _lines.Add($"ParameterSetAxisValue({_xAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, $rglobal[{_deviceNo}])");
                _lines.Add($"ParameterSetAxisValue({_yAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, $rglobal[{_deviceNo + 1}])");
            }

            if (_scanPlannerStageEncoderMode)
            {
                _lines.Add($"ParameterSetAxisValue({_xAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, ParameterGetAxisValue({_xAxis}, AxisParameter.CountsPerUnit) / $StageEncoder)");
                _lines.Add($"ParameterSetAxisValue({_yAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider, ParameterGetAxisValue({_yAxis}, AxisParameter.CountsPerUnit) / $StageEncoder)");
            }

            _lines.Add($"PsoReset({_laserAxis})");

            if (psoMode == EN_AEROTECH_PSO_MODE.Unused)
            {
                SetPSOOnOff(false);
                LaserAuto();
                return;
            }

            _lines.Add($"PsoDistanceConfigureInputs({_laserAxis}, [PsoDistanceInput.GL4PrimaryFeedbackAxis1Encoder0, PsoDistanceInput.GL4IfovFeedbackAxis2])");

            if (mode == EN_AEROTECH_MODE.Mof)
            {
                _lines.Add($"PsoDistanceConfigureFixedDistance({_laserAxis},  Round(   UnitsToCounts({_laserAxis}, {Format(pulseDistance)}) / $Cpu{_xAxis} ))");
            }
            else
            {
                _lines.Add($"PsoDistanceConfigureFixedDistance({_laserAxis},  Round(   UnitsToCounts({_laserAxis}, {Format(pulseDistance)}) / $rglobal[{_deviceNo}] ))");
            }

            _lines.Add($"PsoDistanceCounterOn({_laserAxis})");
            _lines.Add($"PsoDistanceEventsOn({_laserAxis})");
            _lines.Add($"PsoDistanceConfigureCounterReset({_laserAxis}, PsoDistanceCounterResetMask.ResetWhenLaserOff)");
            _lines.Add($"PsoWaveformConfigureMode({_laserAxis}, PsoWaveformMode.Pulse)");
            _lines.Add($"PsoWaveformConfigurePulseFixedTotalTime({_laserAxis}, {Format(totalTime)})");
            _lines.Add($"PsoWaveformConfigurePulseFixedOnTime({_laserAxis},{Format(laserOnTime)})");
            _lines.Add($"PsoWaveformConfigurePulseFixedCount({_laserAxis}, 1)");
            _lines.Add($"PsoWaveformApplyPulseConfiguration({_laserAxis})");

            SetPSOOnOff(true);

            _lines.Add($"PsoOutputConfigureSource({_laserAxis}, PsoOutputSource.Waveform)");
            _lines.Add($"PsoOutputConfigureOutput({_laserAxis}, PsoOutputPin.GL4LaserOutput0)");
            _lines.Add($"PsoEventConfigureMask({_laserAxis}, PsoEventMask.LaserMask)");
            LaserAuto();
        }

        public void SetPSODistance(double pulseDistance)
        {
            _lines.Add($"PsoDistanceConfigureFixedDistance({_laserAxis}, Round(UnitsToCounts({_laserAxis}, {Format(pulseDistance)}) / ParameterGetAxisValue({_laserAxis}, AxisParameter.PrimaryEmulatedQuadratureDivider)))");
        }

        public void SetPSOOnOff(bool on)
        {
            _lines.Add(on
                ? $"PsoWaveformOn({_laserAxis})"
                : $"PsoWaveformOff({_laserAxis})");
        }

        public void SetPSOChangePower(
            double frequencyKhz,
            double powerPercent)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetPSOChangePower.
        }

        public void SetPSOFire(
            double totalTime,
            double laserOnTime,
            int count,
            double delay,
            EN_AEROTECH_MODE mode)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetPSOFire.
        }

        public void SetPSOLaserWindowMask(
            bool on,
            double windowStartRange = 0,
            double windowEndRange = 0)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetPSOLaserWindowMask.
        }

        public void DeclareEncoderVariable(
            string axis = "",
            bool useFeedback = false)
        {
            var encoderAxis = NormalizeAxis(axis, _xAxis);

            _lines.Add("var $encoder as real");
            _lines.Add(useFeedback
                ? $"$encoder = StatusGetAxisItem({encoderAxis}, AxisStatusItem.AuxiliaryFeedback)"
                : "$encoder = 0");
        }

        public void InitDeclareVariable()
        {
            _lines.Add($"var $EncoderScale{_xAxis} as real");
            _lines.Add($"var $EncoderScale{_yAxis} as real");
            _lines.Add($"var $Cpu{_xAxis} as real");
            _lines.Add($"var $Cpu{_yAxis} as real");
            _lines.Add("var $StageEncoder as real");
        }

        public void InitDeclareVariableIFOV()
        {
            // Automation1 branch only documents rglobal usage in ScanMaster; no script line is generated.
        }

        public void SetWaitForEncoder(
            string axis,
            double position,
            bool directionPlus = true)
        {
            var encoderAxis = NormalizeAxis(axis, _xAxis);
            var op = directionPlus ? ">" : "<";
            var offset = directionPlus ? position : -position;

            _lines.Add($"wait(StatusGetAxisItem({encoderAxis}, AxisStatusItem.AuxiliaryFeedback) {op} $encoder + ({Format(offset)}))");
        }

        public void SetWaitForEncoder(
            string axis,
            bool directionPlus,
            double position,
            double limit,
            double encoderScale = 1.0)
        {
            var encoderAxis = NormalizeAxis(axis, _xAxis);

            if (_scanPlannerStageEncoderMode)
            {
                _lines.Add($"wait(Abs(StatusGetAxisItem({_yAxis}, AxisStatusItem.AuxiliaryFeedback)) > $StageEncoder * {Format(Math.Abs(position))})");
                return;
            }

            if (Math.Abs(encoderScale) > 0.000001 &&
                Math.Abs(encoderScale - 1.0) > 0.000001)
            {
                _lines.Add($"wait(Abs(StatusGetAxisItem({_yAxis}, AxisStatusItem.AuxiliaryFeedback)) > {Format(Math.Abs(encoderScale))} * {Format(Math.Abs(position))})");
                return;
            }

            if (directionPlus)
            {
                var posEncoder = (position - limit) * encoderScale;
                _lines.Add($"wait(StatusGetAxisItem({encoderAxis}, AxisStatusItem.PositionFeedback) >= {Format(posEncoder)})");
            }
            else
            {
                var posEncoder = (position + limit) * encoderScale;
                _lines.Add($"wait(StatusGetAxisItem({encoderAxis}, AxisStatusItem.PositionFeedback) <= {Format(posEncoder)})");
            }
        }

        public void SetWaitForEncoder2Axis(
            string axisX,
            string axisY,
            bool inToOut,
            double posX,
            double posY,
            double limitX,
            double limitY)
        {
            var x = NormalizeAxis(axisX, _stageXAxis);
            var y = NormalizeAxis(axisY, _stageYAxis);

            if (inToOut)
            {
                _lines.Add($"while(StatusGetAxisItem({x}, AxisStatusItem.PositionFeedback) > {Format(posX - limitX)} && StatusGetAxisItem({x}, AxisStatusItem.PositionFeedback) < {Format(posX + limitX)} && StatusGetAxisItem({y}, AxisStatusItem.PositionFeedback) > {Format(posY - limitY)} && StatusGetAxisItem({y}, AxisStatusItem.PositionFeedback) < {Format(posY + limitY)})");
            }
            else
            {
                _lines.Add($"while(!(StatusGetAxisItem({x}, AxisStatusItem.PositionFeedback) > {Format(posX - limitX)} && StatusGetAxisItem({x}, AxisStatusItem.PositionFeedback) < {Format(posX + limitX)}) || !(StatusGetAxisItem({y}, AxisStatusItem.PositionFeedback) > {Format(posY - limitY)} && StatusGetAxisItem({y}, AxisStatusItem.PositionFeedback) < {Format(posY + limitY)}))");
            }

            _lines.Add("end");
        }

        public void SetWaitForStartAxis2(
            string axisX,
            string axisY,
            bool inToOut,
            double posX,
            double posY,
            double limitX,
            double limitY)
        {
            var x = NormalizeAxis(axisX, "NONE");
            var y = NormalizeAxis(axisY, "NONE");

            if (!IsUsableAxis(x) &&
                IsUsableAxis(y))
            {
                _lines.Add($"while(Abs(StatusGetAxisItem({y}, AxisStatusItem.PositionFeedback) - {Format(posY)}) < 0.005)");
                _lines.Add("end");
                return;
            }

            if (!IsUsableAxis(y) &&
                IsUsableAxis(x))
            {
                _lines.Add($"while(Abs(StatusGetAxisItem({x}, AxisStatusItem.PositionFeedback) - {Format(posX)}) < 0.005)");
                _lines.Add("end");
                return;
            }

            if (IsUsableAxis(x) &&
                IsUsableAxis(y))
            {
                _lines.Add($"while(Abs(StatusGetAxisItem({x}, AxisStatusItem.PositionFeedback) - {Format(posX)}) < 0.01 && Abs(StatusGetAxisItem({y}, AxisStatusItem.PositionFeedback) - {Format(posY)}) < 0.01)");
                _lines.Add("end");
            }
        }

        public void SetEncoderScaleFactor(
            string galvoAxis,
            string encoderAxis,
            int scale)
        {
            if (scale == 0)
            {
                return;
            }

            var galvo = NormalizeAxis(galvoAxis, _laserAxis);
            var encoder = NormalizeAxis(encoderAxis, _xAxis);

            _lines.Add($"GalvoEncoderScaleFactorSet({galvo}, ParameterGetAxisValue({encoder}, AxisParameter.CountsPerUnit) / {scale})");
        }

        public void SetEncoderScaleFactor(
            string galvoAxis,
            string encoderAxis,
            bool directionPlus)
        {
            var galvo = NormalizeAxis(galvoAxis, _laserAxis);
            var encoder = NormalizeAxis(encoderAxis, _stageXAxis);
            var sign = directionPlus ? "" : " * -1";

            _lines.Add($"$EncoderScale{galvo} = ParameterGetAxisValue({galvo},AxisParameter.CountsPerUnit) / (ParameterGetAxisValue({encoder},AxisParameter.CountsPerUnit) / ParameterGetAxisValue({encoder},AxisParameter.AuxiliaryEmulatedQuadratureDivider)){sign}");
            _lines.Add($"GalvoEncoderScaleFactorSet({galvo}, $EncoderScale{galvo})");
        }

        public void SetEncoderScaleFactor(
            string galvoAxis,
            string encoderAxis,
            double encoderX,
            double encoderY,
            bool directionPlus)
        {
            if (Math.Abs(encoderX) < 0.000001)
            {
                return;
            }

            var galvo = NormalizeAxis(galvoAxis, _laserAxis);
            var sign = directionPlus ? "" : "-";

            _lines.Add($"$EncoderScale{galvo} = ParameterGetAxisValue({galvo},AxisParameter.CountsPerUnit) / {Format(Math.Abs(encoderX))}");
            _lines.Add($"GalvoEncoderScaleFactorSet({galvo}, {sign}$EncoderScale{galvo})");
        }

        public void SetEncoderScaleFactorByPrimaryDivider(
            string galvoAxis,
            string encoderAxis,
            bool directionPlus)
        {
            var galvo = NormalizeAxis(galvoAxis, _laserAxis);
            var encoder = NormalizeAxis(encoderAxis, "NONE");

            if (!IsUsableAxis(encoder))
            {
                return;
            }

            var sign = directionPlus ? "" : " * -1";

            _lines.Add($"$EncoderScale{galvo} = ParameterGetAxisValue({galvo}, AxisParameter.CountsPerUnit) / (ParameterGetAxisValue({encoder}, AxisParameter.CountsPerUnit) / ParameterGetAxisValue({encoder}, AxisParameter.PrimaryEmulatedQuadratureDivider)){sign}");
            _lines.Add($"GalvoEncoderScaleFactorSet({galvo}, $EncoderScale{galvo})");
        }

        public void InitEncoderCount(string galvoAxis)
        {
            var galvo = NormalizeAxis(galvoAxis, _laserAxis);

            _lines.Add($"DriveSetAuxiliaryFeedback({galvo}, 0)");
        }

        public void EncoderNotFeedback(string axis)
        {
            var encoderAxis = NormalizeAxis(axis, _stageXAxis);

            _lines.Add($"DriveEncoderOutputOff({encoderAxis}, 0)");
        }

        public void ReleaseEncoderScaleFactor(string galvoAxis)
        {
            var galvo = NormalizeAxis(galvoAxis, _laserAxis);

            _lines.Add($"GalvoEncoderScaleFactorSet({galvo}, 0)");
        }

        public void LaserAuto()
        {
            _lines.Add($"GalvoLaserOutput({_laserAxis}, GalvoLaser.Auto)");
        }

        public void LaserOn()
        {
            _lines.Add($"GalvoLaserOutput({_laserAxis}, GalvoLaser.On)");
        }

        public void LaserOff()
        {
            _lines.Add($"GalvoLaserOutput({_laserAxis}, GalvoLaser.Off)");
        }

        public void PsoLaserControl(
            bool on,
            bool manual = false)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetPSOLaserControl.
        }

        public void LaserFire(bool on)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::ListLaserFire.
        }

        public void Jump(double x, double y)
        {
            if (!(x == 0.0 && y == 0.0) &&
                _currentX == x &&
                _currentY == y)
            {
                return;
            }

            _pointCount++;
            _lines.Add($"MoveRapid([{_xAxis}, {_yAxis}], [{Format(x)}, {Format(y)}])");
            AddMoveTactTime(x, y, _jumpSpeed);
            _currentX = x;
            _currentY = y;
        }

        public void Mark(double x, double y)
        {
            if (_currentX == x &&
                _currentY == y)
            {
                return;
            }

            _pointCount++;
            _lines.Add($"MoveLinear([{_xAxis}, {_yAxis}], [{Format(x)}, {Format(y)}])");
            AddMoveTactTime(x, y, _markSpeed);
            _currentX = x;
            _currentY = y;
        }

        public void JumpRel(double x, double y)
        {
            Jump(_currentX + x, _currentY + y);
        }

        public void MarkRel(double x, double y)
        {
            Mark(_currentX + x, _currentY + y);
        }

        public void Arc(
            double startX,
            double startY,
            double endX,
            double endY,
            double centerX,
            double centerY,
            double angle)
        {
            var i = centerX - startX;
            var j = centerY - startY;
            var command = angle >= 0.0 ? "MoveCw" : "MoveCcw";

            _pointCount++;
            _lines.Add($"{command}([{_xAxis}, {_yAxis}], [{Format(endX)}, {Format(endY)}], [{Format(i)}, {Format(j)}])");
            _currentX = endX;
            _currentY = endY;
        }

        public void JumpLinear(
            double x,
            double y)
        {
            _lines.Add($"SetupCoordinatedSpeed({Format(_jumpSpeed * 1000.0)})");
            LaserOff();
            _pointCount++;
            _lines.Add($"MoveLinear([{_xAxis}, {_yAxis}], [{Format(x)}, {Format(y)}])");
            _lines.Add($"SetupCoordinatedSpeed({Format(_markSpeed * 1000.0)})");
            _currentX = x;
            _currentY = y;
        }

        public void WaitMoveDone()
        {
            _lines.Add($"WaitForMotionDone([{_xAxis}, {_yAxis}])");
        }

        public void Dwell(double delay)
        {
            if (delay <= 0.0)
            {
                return;
            }

            _lines.Add($"Dwell({Format(delay)})");
        }

        public void EnableAxisPair()
        {
            _lines.Add($"Enable([{_xAxis}, {_yAxis}])");
        }

        public void DisableAxisPair()
        {
            _lines.Add($"Disable([{_xAxis}, {_yAxis}])");
        }

        public void FaultAckAxisPair()
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::ListFaultAckXY.
        }

        public void HomeAxisPair()
        {
            _lines.Add($"Home([{_xAxis}, {_yAxis}])");
        }

        public void OffsetClearAxisPair()
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::ListOffsetClearXY.
        }

        public void OffsetSetAxisPair(
            double x,
            double y)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::ListOffsetSetXY.
        }

        public void SetSignalLogTrigger(bool use)
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetSignalLogTrigger.
        }

        public void ProgramStart()
        {
            _lines.Add("program");
        }

        public void ProgramEnd()
        {
            _lines.Add("end");
        }

        public void BufferedEnd()
        {
            _lines.Add("CommandQueueStop()");
        }

        public void WaitInpos()
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetWaitInpos.
        }

        public void SetHomePos()
        {
            // Automation1 branch is empty in ScanMaster CMakeAerotech::SetHomePos.
        }

        public void SetGalvoPosZero()
        {
            _lines.Add($"MoveRapid([{_xAxis}, {_yAxis}], [0, 0])");
            _currentX = 0.0;
            _currentY = 0.0;
        }

        public void End(bool bufferedRun = false)
        {
            if (bufferedRun)
            {
                _lines.Add("CommandQueueStop()");
            }

            if (_lines.Count == 0 ||
                !_lines[^1].Trim().Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                _lines.Add("end");
            }
        }

        public async Task<ST_AUTOMATION1_SCRIPT> Save(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_lines.Count == 0 ||
                !_lines[^1].Trim().Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                End();
            }

            Directory.CreateDirectory(_scriptDirectory);
            await System.IO.File.WriteAllLinesAsync(FilePath, _lines, Encoding.UTF8, cancellationToken);

            return new ST_AUTOMATION1_SCRIPT(
                FileName,
                FilePath,
                _lines.ToArray(),
                _pointCount,
                _pointCount > 0 ? 1 : 0,
                _createdAt);
        }

        private void AddMoveTactTime(
            double nextX,
            double nextY,
            double speed)
        {
            if (speed <= 0.0)
            {
                return;
            }

            var distance = Math.Sqrt(
                Math.Pow(nextY - _currentY, 2) +
                Math.Pow(nextX - _currentX, 2));

            _tactTime += distance / (speed * 1000.0);
        }

        private static string NormalizeAxis(
            string? axis,
            string defaultAxis)
        {
            return string.IsNullOrWhiteSpace(axis)
                ? defaultAxis
                : axis.Trim();
        }

        private static bool IsUsableAxis(string? axis)
        {
            return !string.IsNullOrWhiteSpace(axis) &&
                !axis.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase);
        }
    }
}
