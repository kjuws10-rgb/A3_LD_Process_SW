using Drilling.Common.Log;

namespace Drilling.Common.Interface;

public enum EN_MELSEC_DATA_TYPE
{
    Bit,
    Word,
    DWord,
    Double,
    Float,
    String
}

public enum EN_MELSEC_DIRECTION
{
    In,
    Out,
    InOut
}

public enum EN_MELSEC_ACCESS
{
    Read,
    Write,
    ReadWrite
}

public sealed record ST_MELSEC_MAP_DATA(
    string Id,
    bool Use,
    string Group,
    string Name,
    int DeviceNo,
    string Address,
    EN_MELSEC_DATA_TYPE DataType,
    EN_MELSEC_DIRECTION Direction,
    EN_MELSEC_ACCESS Access,
    double Scale,
    int Length,
    int PollMs,
    string Description);

public interface IMelsecMapFile
{
    Task<IReadOnlyList<ST_MELSEC_MAP_DATA>> LoadAll(CancellationToken cancellationToken = default);
}

public interface IMelsec
{
    IReadOnlyList<ST_MELSEC_MAP_DATA> Map { get; }

    void ReloadMap(IReadOnlyList<ST_MELSEC_MAP_DATA> map);

    IReadOnlyList<ST_MELSEC_MAP_DATA> GetMapList(string group = "");

    ST_MELSEC_MAP_DATA GetMapData(string id);

    Task<bool> ReadBit(string id, CancellationToken cancellationToken = default);

    Task WriteBit(string id, bool value, CancellationToken cancellationToken = default);

    Task<int> ReadWord(string id, CancellationToken cancellationToken = default);

    Task WriteWord(string id, int value, CancellationToken cancellationToken = default);

    Task<double> ReadDouble(string id, CancellationToken cancellationToken = default);

    Task WriteDouble(string id, double value, CancellationToken cancellationToken = default);

    Task<string> ReadString(string id, CancellationToken cancellationToken = default);

    Task WriteString(string id, string value, CancellationToken cancellationToken = default);
}

public sealed class CMelsec : IMelsec
{
    private readonly IInterfaceManager _interfaceManager;
    private readonly ILogManager? _logManager;
    private Dictionary<string, ST_MELSEC_MAP_DATA> _map = new(StringComparer.OrdinalIgnoreCase);

    public CMelsec(
        IInterfaceManager interfaceManager,
        ILogManager? logManager = null,
        IReadOnlyList<ST_MELSEC_MAP_DATA>? map = null)
    {
        _interfaceManager = interfaceManager;
        _logManager = logManager;
        ReloadMap(map ?? []);
    }

    public IReadOnlyList<ST_MELSEC_MAP_DATA> Map => _map.Values
        .OrderBy(data => data.Group, StringComparer.OrdinalIgnoreCase)
        .ThenBy(data => data.DeviceNo)
        .ThenBy(data => data.Id, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public void ReloadMap(IReadOnlyList<ST_MELSEC_MAP_DATA> map)
    {
        _map = map
            .Where(data => data.Use)
            .ToDictionary(data => NormalizeId(data.Id), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ST_MELSEC_MAP_DATA> GetMapList(string group = "")
    {
        var normalizedGroup = group.Trim();

        return Map
            .Where(data => string.IsNullOrWhiteSpace(normalizedGroup) ||
                data.Group.Equals(normalizedGroup, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public ST_MELSEC_MAP_DATA GetMapData(string id)
    {
        var normalizedId = NormalizeId(id);

        if (_map.TryGetValue(normalizedId, out var data))
        {
            return data;
        }

        throw new InvalidOperationException(
            $"MELSEC map was not registered: {id}. Available={string.Join(", ", _map.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))}");
    }

    public Task<bool> ReadBit(string id, CancellationToken cancellationToken = default)
    {
        var data = PrepareRead(id, EN_MELSEC_DATA_TYPE.Bit, cancellationToken);
        return NotImplemented<bool>(data, CreateCommand("READ_BIT", data));
    }

    public Task WriteBit(string id, bool value, CancellationToken cancellationToken = default)
    {
        var data = PrepareWrite(id, EN_MELSEC_DATA_TYPE.Bit, cancellationToken);
        return NotImplemented(data, CreateCommand("WRITE_BIT", data, value ? "1" : "0"));
    }

    public Task<int> ReadWord(string id, CancellationToken cancellationToken = default)
    {
        var data = PrepareRead(id, [EN_MELSEC_DATA_TYPE.Word, EN_MELSEC_DATA_TYPE.DWord], cancellationToken);
        return NotImplemented<int>(data, CreateCommand("READ_WORD", data));
    }

    public Task WriteWord(string id, int value, CancellationToken cancellationToken = default)
    {
        var data = PrepareWrite(id, [EN_MELSEC_DATA_TYPE.Word, EN_MELSEC_DATA_TYPE.DWord], cancellationToken);
        return NotImplemented(data, CreateCommand("WRITE_WORD", data, value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    public Task<double> ReadDouble(string id, CancellationToken cancellationToken = default)
    {
        var data = PrepareRead(id, [EN_MELSEC_DATA_TYPE.Double, EN_MELSEC_DATA_TYPE.Float], cancellationToken);
        return NotImplemented<double>(data, CreateCommand("READ_DOUBLE", data));
    }

    public Task WriteDouble(string id, double value, CancellationToken cancellationToken = default)
    {
        var data = PrepareWrite(id, [EN_MELSEC_DATA_TYPE.Double, EN_MELSEC_DATA_TYPE.Float], cancellationToken);
        return NotImplemented(data, CreateCommand("WRITE_DOUBLE", data, value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    public Task<string> ReadString(string id, CancellationToken cancellationToken = default)
    {
        var data = PrepareRead(id, EN_MELSEC_DATA_TYPE.String, cancellationToken);
        return NotImplemented<string>(data, CreateCommand("READ_STRING", data));
    }

    public Task WriteString(string id, string value, CancellationToken cancellationToken = default)
    {
        var data = PrepareWrite(id, EN_MELSEC_DATA_TYPE.String, cancellationToken);
        return NotImplemented(data, CreateCommand("WRITE_STRING", data, value));
    }

    private ST_MELSEC_MAP_DATA PrepareRead(
        string id,
        EN_MELSEC_DATA_TYPE dataType,
        CancellationToken cancellationToken)
    {
        return PrepareRead(id, [dataType], cancellationToken);
    }

    private ST_MELSEC_MAP_DATA PrepareRead(
        string id,
        IReadOnlyList<EN_MELSEC_DATA_TYPE> dataTypes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var data = GetMapData(id);
        EnsureDataType(data, dataTypes);

        if (data.Access == EN_MELSEC_ACCESS.Write)
        {
            throw new InvalidOperationException($"MELSEC map is write only: {FormatMap(data)}");
        }

        return data;
    }

    private ST_MELSEC_MAP_DATA PrepareWrite(
        string id,
        EN_MELSEC_DATA_TYPE dataType,
        CancellationToken cancellationToken)
    {
        return PrepareWrite(id, [dataType], cancellationToken);
    }

    private ST_MELSEC_MAP_DATA PrepareWrite(
        string id,
        IReadOnlyList<EN_MELSEC_DATA_TYPE> dataTypes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var data = GetMapData(id);
        EnsureDataType(data, dataTypes);

        if (data.Access == EN_MELSEC_ACCESS.Read)
        {
            throw new InvalidOperationException($"MELSEC map is read only: {FormatMap(data)}");
        }

        return data;
    }

    private Task NotImplemented(
        ST_MELSEC_MAP_DATA data,
        string command)
    {
        WriteNotReadyLog(data, command);
        throw CreateNotImplementedException(data, command);
    }

    private Task<T> NotImplemented<T>(
        ST_MELSEC_MAP_DATA data,
        string command)
    {
        WriteNotReadyLog(data, command);
        throw CreateNotImplementedException(data, command);
    }

    private void WriteNotReadyLog(
        ST_MELSEC_MAP_DATA data,
        string command)
    {
        _logManager?.WriteInterfaceError(
            EN_EQP_MODULE.Melsec,
            $"MELSEC_{data.DeviceNo}",
            command,
            "MELSEC live command is not implemented yet.");
    }

    private static NotSupportedException CreateNotImplementedException(
        ST_MELSEC_MAP_DATA data,
        string command)
    {
        return new NotSupportedException(
            $"MELSEC live read/write is not implemented yet. Command={command}, Map={FormatMap(data)}");
    }

    private static string CreateCommand(
        string operation,
        ST_MELSEC_MAP_DATA data,
        string value = "")
    {
        var fields = new[]
        {
            "MELSEC",
            operation,
            data.Id,
            data.Address,
            data.DataType.ToString().ToUpperInvariant(),
            data.Scale.ToString(System.Globalization.CultureInfo.InvariantCulture),
            data.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            value
        };

        return string.Join(":", fields);
    }

    private static void EnsureDataType(
        ST_MELSEC_MAP_DATA data,
        IReadOnlyList<EN_MELSEC_DATA_TYPE> dataTypes)
    {
        if (dataTypes.Contains(data.DataType))
        {
            return;
        }

        throw new InvalidOperationException(
            $"MELSEC map data type mismatch: {FormatMap(data)}. Expected={string.Join("/", dataTypes)}");
    }

    private static string FormatMap(ST_MELSEC_MAP_DATA data)
    {
        return $"{data.Id}({data.Address}, {data.DataType}, MELSEC_{data.DeviceNo})";
    }

    private static string NormalizeId(string id)
    {
        return id.Trim().ToUpperInvariant();
    }
}
