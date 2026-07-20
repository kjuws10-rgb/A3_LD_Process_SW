using System.Globalization;
using System.Text.RegularExpressions;

namespace Talon.Driver;

public static partial class CTalonProtocol
{
    public const string CommandTerminator = "\r";
    public static readonly int[] SupportedBaudRates = [9600, 19200, 38400, 57600, 115200];

    public static string Build(EN_TALON_COMMAND command, double? parameter = null)
    {
        var spec = CTalonCommandCatalog.Get(command);

        if (!spec.RequiresParameter)
        {
            return string.IsNullOrWhiteSpace(spec.QueryText) ? spec.SetTemplate : spec.QueryText;
        }

        if (!parameter.HasValue)
        {
            throw new ArgumentException($"{spec.DisplayName} 명령에는 Parameter가 필요합니다.", nameof(parameter));
        }

        ValidateParameter(spec, parameter.Value);
        return string.Format(CultureInfo.InvariantCulture, spec.SetTemplate, parameter.Value);
    }

    public static string NormalizeResponse(string command, string response)
    {
        var lines = response
            .Replace("\0", "", StringComparison.Ordinal)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Some terminal configurations echo the command before the actual response.
        return lines
            .FirstOrDefault(line => !line.Equals(command, StringComparison.OrdinalIgnoreCase))?
            .Trim() ?? "";
    }

    public static double ReadDouble(string response)
    {
        var match = LeadingNumberRegex().Match(response.Trim());
        return match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new FormatException($"숫자 응답을 해석할 수 없습니다: '{response}'");
    }

    public static int ReadInt(string response) => checked((int)Math.Round(ReadDouble(response)));

    public static bool ReadBoolean(string response)
    {
        return response.Trim().ToUpperInvariant() switch
        {
            "1" or "ON" or "OPEN" or "TRUE" or "EMISSION" => true,
            "0" or "OFF" or "CLOSED" or "CLOSE" or "FALSE" or "STANDBY" => false,
            _ => throw new FormatException($"Boolean 응답을 해석할 수 없습니다: '{response}'")
        };
    }

    public static IReadOnlyList<int> ReadStatusHistory(string response)
    {
        var values = response
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new FormatException($"상태 코드 응답을 해석할 수 없습니다: '{token}'"))
            .ToArray();

        if (values.Length == 0)
        {
            throw new FormatException("상태 이력이 비어 있습니다.");
        }

        return values;
    }

    public static void ValidateParameter(ST_TALON_COMMAND_SPEC spec, double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Parameter는 유한한 숫자여야 합니다.");
        }

        if (spec.Minimum.HasValue && value < spec.Minimum.Value ||
            spec.Maximum.HasValue && value > spec.Maximum.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"{spec.DisplayName} 허용범위는 {spec.Minimum}~{spec.Maximum} {spec.Unit}입니다.");
        }

        if (spec.Command == EN_TALON_COMMAND.SetBaudRate &&
            !SupportedBaudRates.Contains((int)value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "매뉴얼에 정의된 Baud Rate만 사용할 수 있습니다.");
        }
    }

    [GeneratedRegex(@"^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?")]
    private static partial Regex LeadingNumberRegex();
}
