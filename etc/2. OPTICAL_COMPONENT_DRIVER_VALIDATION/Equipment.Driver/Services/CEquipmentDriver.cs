using System.Diagnostics;

namespace Equipment.Driver;

public sealed class CEquipmentDriver(ST_EQUIPMENT_PROFILE profile, IEquipmentTransport transport) : IAsyncDisposable
{
    private readonly Dictionary<string, ST_EQUIPMENT_STATUS_FIELD> _status = new(StringComparer.OrdinalIgnoreCase);
    public ST_EQUIPMENT_PROFILE Profile => profile;
    public IEquipmentTransport Transport => transport;
    public CEquipmentTrace Trace { get; } = [];
    public IReadOnlyList<ST_EQUIPMENT_STATUS_FIELD> Status => _status.Values.OrderBy(item => item.Name).ToArray();

    public Task Connect(CancellationToken cancellationToken = default) => transport.Connect(cancellationToken);
    public Task Disconnect(CancellationToken cancellationToken = default) => transport.Disconnect(cancellationToken);

    public async Task<string> Execute(ST_EQUIPMENT_COMMAND_SPEC spec, double? parameter = null, ST_EQUIPMENT_SAFETY? safety = null, CancellationToken cancellationToken = default)
    {
        if (spec.Equipment != profile.Type) throw new InvalidOperationException("선택 컴포넌트와 명령이 일치하지 않습니다.");
        if (spec.Risk != EN_EQUIPMENT_RISK.ReadOnly && !(safety?.Allows(spec.Risk) ?? false))
            throw new InvalidOperationException($"{spec.Risk} 명령의 안전 확인 조건이 충족되지 않았습니다.");

        var command = CEquipmentProtocol.Build(spec, parameter);
        var watch = Stopwatch.StartNew();
        try
        {
            var raw = await transport.Exchange(command, spec.ExpectsResponse, cancellationToken);
            var response = CEquipmentProtocol.Normalize(command, raw);
            var passed = CEquipmentProtocol.Validate(spec, response, out var expected);
            watch.Stop();
            Trace.AddBounded(new(DateTimeOffset.Now, profile.DisplayName, command, response, passed, watch.Elapsed.TotalMilliseconds, passed ? "정상" : expected));
            _status[spec.DisplayName] = new(spec.DisplayName, response, spec.Unit, DateTimeOffset.Now);
            if (!passed) throw new InvalidDataException($"응답 형식 오류: {expected}, actual={response}");
            return response;
        }
        catch (Exception ex)
        {
            watch.Stop();
            Trace.AddBounded(new(DateTimeOffset.Now, profile.DisplayName, command, "", false, watch.Elapsed.TotalMilliseconds, ex.Message));
            throw;
        }
    }

    public async Task<IReadOnlyList<ST_EQUIPMENT_VALIDATION>> RunReadOnlyValidation(CancellationToken cancellationToken = default)
    {
        var results = new List<ST_EQUIPMENT_VALIDATION>();
        foreach (var spec in CEquipmentCatalog.GetCommands(profile.Type).Where(item => item.Risk == EN_EQUIPMENT_RISK.ReadOnly))
        {
            var command = CEquipmentProtocol.Build(spec);
            try
            {
                var actual = await Execute(spec, cancellationToken: cancellationToken);
                var passed = CEquipmentProtocol.Validate(spec, actual, out var expected);
                results.Add(new(profile.DisplayName, spec.DisplayName, command, passed, actual, expected, passed ? "PASS" : "응답 형식 불일치"));
            }
            catch (Exception ex)
            {
                CEquipmentProtocol.Validate(spec, "", out var expected);
                results.Add(new(profile.DisplayName, spec.DisplayName, command, false, "", expected, ex.Message));
            }
        }
        return results;
    }

    public async ValueTask DisposeAsync() => await transport.DisposeAsync();
}
