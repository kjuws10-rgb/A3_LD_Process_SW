using System.Globalization;
using System.Text.RegularExpressions;

namespace Equipment.Driver;

public static partial class CEquipmentProtocol
{
    public static string Build(ST_EQUIPMENT_COMMAND_SPEC spec, double? parameter = null)
    {
        if (!spec.RequiresParameter) return spec.Template;

        var value = parameter ?? spec.DefaultParameter ??
            throw new ArgumentException($"{spec.DisplayName} 명령에는 parameter가 필요합니다.");

        if (spec.Minimum is double min && value < min || spec.Maximum is double max && value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(parameter), value,
                $"허용 범위는 {spec.Minimum} ~ {spec.Maximum} {spec.Unit}입니다.");
        }

        return string.Format(CultureInfo.InvariantCulture, spec.Template, value);
    }

    public static string Normalize(string command, string response)
    {
        var lines = response.Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" ", lines.Where(line => !line.Equals(command, StringComparison.OrdinalIgnoreCase))).Trim();
    }

    public static bool Validate(ST_EQUIPMENT_COMMAND_SPEC spec, string response, out string expected)
    {
        expected = spec.ResponseKind switch
        {
            EN_EQUIPMENT_RESPONSE.Integer => "정수 또는 정수가 포함된 장비 응답",
            EN_EQUIPMENT_RESPONSE.FloatingPoint => "실수 또는 실수가 포함된 장비 응답",
            EN_EQUIPMENT_RESPONSE.XpsResult => "XPS error code 0으로 시작하는 API 결과",
            EN_EQUIPMENT_RESPONSE.Acknowledgement => "ACK(!/OK/SENT) 또는 error code 0",
            _ => "비어 있지 않고 ERR로 시작하지 않는 문자열"
        };

        if (string.IsNullOrWhiteSpace(response) || response.StartsWith("ERR", StringComparison.OrdinalIgnoreCase)) return false;
        return spec.ResponseKind switch
        {
            EN_EQUIPMENT_RESPONSE.Integer => NumberRegex().Matches(response).Any(match => long.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)),
            EN_EQUIPMENT_RESPONSE.FloatingPoint => NumberRegex().Matches(response).Any(match => double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)),
            EN_EQUIPMENT_RESPONSE.XpsResult => response.TrimStart().StartsWith("0", StringComparison.Ordinal),
            EN_EQUIPMENT_RESPONSE.Acknowledgement => response.Contains('!') || response.Equals("OK", StringComparison.OrdinalIgnoreCase) || response.Equals("SENT", StringComparison.OrdinalIgnoreCase) || response.TrimStart().StartsWith("0", StringComparison.Ordinal),
            _ => true
        };
    }

    [GeneratedRegex(@"[-+]?\d+(?:\.\d+)?(?:[Ee][-+]?\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();
}
