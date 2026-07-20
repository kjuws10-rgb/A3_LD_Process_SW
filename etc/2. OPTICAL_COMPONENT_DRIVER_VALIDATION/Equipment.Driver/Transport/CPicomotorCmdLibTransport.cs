using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Equipment.Driver;

/// <summary>
/// 제조사 CmdLib.dll을 compile-time dependency 없이 연결하는 adapter입니다.
/// 첨부 Programming Samples의 lifecycle을 따르며 DLL version별 method 차이는 명확한 예외로 보고합니다.
/// </summary>
public sealed partial class CPicomotorCmdLibTransport(string assemblyPath, int discoveryDelayMs = 5000) : IEquipmentTransport
{
    private object? _library;
    private Type? _libraryType;
    private string _deviceKey = "";
    private int _deviceAddress;

    public EN_EQUIPMENT_CONNECTION ConnectionState { get; private set; }
    public string Endpoint => string.IsNullOrWhiteSpace(_deviceKey) ? assemblyPath : $"{assemblyPath} [{_deviceKey}]";

    public Task Connect(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectionState = EN_EQUIPMENT_CONNECTION.Connecting;
        var fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("제조사 Picomotor CmdLib.dll을 찾을 수 없습니다.", fullPath);

        var assembly = Assembly.LoadFrom(fullPath);
        _libraryType = assembly.GetTypes().FirstOrDefault(type => type.Name.Equals("CmdLib8742", StringComparison.OrdinalIgnoreCase))
            ?? throw new TypeLoadException("CmdLib.dll에서 CmdLib8742 type을 찾지 못했습니다.");
        _library = CreateLibrary(_libraryType);
        _deviceAddress = Convert.ToInt32(Invoke("GetMasterDeviceAddress", _deviceKey), CultureInfo.InvariantCulture);
        var opened = Convert.ToBoolean(Invoke("Open", _deviceKey), CultureInfo.InvariantCulture);
        if (!opened) throw new IOException($"Picomotor 장비 Open 실패: {_deviceKey}");
        ConnectionState = EN_EQUIPMENT_CONNECTION.Online;
        return Task.CompletedTask;
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_library is not null && !string.IsNullOrWhiteSpace(_deviceKey)) TryInvoke("Close", _deviceKey);
            if (_library is not null) TryInvoke("Shutdown");
        }
        finally
        {
            _library = null; _libraryType = null; _deviceKey = "";
            ConnectionState = EN_EQUIPMENT_CONNECTION.Offline;
        }
        return Task.CompletedTask;
    }

    public async Task<string> Exchange(string command, bool expectResponse, CancellationToken cancellationToken = default)
    {
        if (_library is null) await Connect(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.Contains("DiscoverDevices", StringComparison.Ordinal))
        {
            TryInvoke("DiscoverDevices");
            return string.Join(';', ReadStringArray("GetDeviceKeys"));
        }
        if (command.Contains("IdentifyInstrument", StringComparison.Ordinal)) return Identify();
        if (command.Contains("GetPosition", StringComparison.Ordinal)) return InvokeRefValue("GetPosition", 1);
        if (command.Contains("GetMotionDone", StringComparison.Ordinal)) return InvokeRefValue("GetMotionDone", 1);
        if (command.Contains("GetError", StringComparison.Ordinal)) return InvokeRefValue("GetError");
        if (command.Contains("RelativeMove", StringComparison.Ordinal))
        {
            var steps = LastIntegerRegex().Matches(command).Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture)).Last();
            return Convert.ToBoolean(Invoke("RelativeMove", _deviceKey, _deviceAddress, 1, steps), CultureInfo.InvariantCulture) ? "OK" : "ERR:MOVE";
        }
        if (command.Contains("StopMotion", StringComparison.Ordinal))
            return Convert.ToBoolean(Invoke("StopMotion", _deviceKey, _deviceAddress, 1), CultureInfo.InvariantCulture) ? "OK" : "ERR:STOP";

        throw new NotSupportedException($"CmdLib logical operation을 지원하지 않습니다: {command}");
    }

    public async ValueTask DisposeAsync() => await Disconnect();

    private object CreateLibrary(Type type)
    {
        foreach (var constructor in type.GetConstructors().Where(item => item.GetParameters().Length == 3))
        {
            var third = constructor.GetParameters()[2].ParameterType;
            var args = third.GetElementType() == typeof(string[]) ? new object?[] { false, discoveryDelayMs, Array.Empty<string>() } : new object?[] { false, discoveryDelayMs, null };
            try
            {
                var instance = constructor.Invoke(args);
                _deviceKey = args[2] switch { string key => key, string[] keys when keys.Length > 0 => keys[0], _ => "" };
                if (!string.IsNullOrWhiteSpace(_deviceKey)) return instance;
            }
            catch (TargetInvocationException) { }
        }
        throw new InvalidOperationException("Picomotor를 검색하지 못했습니다. USB/Ethernet 연결과 discovery 시간을 확인하십시오.");
    }

    private string Identify()
    {
        var method = RequireMethod("IdentifyInstrument", 5);
        var args = new object?[] { _deviceKey, null, null, null, null };
        method.Invoke(_library, args);
        return string.Join(',', args.Skip(1).Select(value => value?.ToString() ?? ""));
    }

    private string InvokeRefValue(string name, params object[] prefix)
    {
        var fixedArgs = new List<object?> { _deviceKey, _deviceAddress };
        fixedArgs.AddRange(prefix.Cast<object?>());
        var method = _libraryType!.GetMethods().FirstOrDefault(item => item.Name == name && item.GetParameters().Length == fixedArgs.Count + 1)
            ?? throw new MissingMethodException(_libraryType.FullName, name);
        fixedArgs.Add(CreateDefault(method.GetParameters()[^1].ParameterType.GetElementType()));
        var result = method.Invoke(_library, fixedArgs.ToArray());
        if (result is bool success && !success) return $"ERR:{name}";
        return fixedArgs[^1]?.ToString() ?? "0";
    }

    private string[] ReadStringArray(string name) => Invoke(name) as string[] ?? [];
    private object? Invoke(string name, params object?[] args) => RequireMethod(name, args.Length).Invoke(_library, args);
    private void TryInvoke(string name, params object?[] args) { try { Invoke(name, args); } catch (MissingMethodException) { } }
    private MethodInfo RequireMethod(string name, int count) => _libraryType?.GetMethods().FirstOrDefault(item => item.Name == name && item.GetParameters().Length == count)
        ?? throw new MissingMethodException(_libraryType?.FullName, name);
    private static object? CreateDefault(Type? type) => type is null || !type.IsValueType ? null : Activator.CreateInstance(type);

    [GeneratedRegex(@"[-+]?\d+", RegexOptions.CultureInvariant)]
    private static partial Regex LastIntegerRegex();
}
