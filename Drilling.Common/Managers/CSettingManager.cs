using Drilling.Common.Interface;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Managers;
using Drilling.Common.Motion;
using Drilling.Common.Station;

namespace Drilling.Common.Managers;

public enum EN_SETTING_TAB
{
    Option,
    Interface,
    Io,
    Motor,
    Position,
    Alarm
}

public sealed record ST_SYSTEM_PARAMETER(
    EN_SETTING_TAB Section,
    string Name,
    string Value,
    string Unit,
    string Description,
    string Group = "",
    string Key = "",
    string DefaultValue = "",
    EN_RECIPE_DATA_TYPE DataType = EN_RECIPE_DATA_TYPE.String,
    double Min = 0.0,
    double Max = 0.0,
    bool Show = true,
    bool Use = true,
    int DisplayOrder = 0,
    IReadOnlyDictionary<string, string>? Extra = null);

public sealed record ST_SETTING_HISTORY(
    DateTimeOffset ChangedAt,
    EN_SETTING_TAB Section,
    string ParameterName,
    string OldValue,
    string NewValue,
    string OperatorId,
    string Action);

public interface ISettingFile
{
    Task<IReadOnlyList<ST_SYSTEM_PARAMETER>> Load(
        EN_SETTING_TAB section,
        CancellationToken cancellationToken = default);

    Task Save(
        EN_SETTING_TAB section,
        IReadOnlyList<ST_SYSTEM_PARAMETER> parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ST_SETTING_HISTORY>> LoadHistory(
        EN_SETTING_TAB section,
        CancellationToken cancellationToken = default);
}

public interface IInterfaceFile
{
    Task<IReadOnlyList<ST_INTERFACE_DATA>> LoadAll(CancellationToken cancellationToken = default);

    Task SaveAll(
        IReadOnlyList<ST_INTERFACE_DATA> interfaces,
        CancellationToken cancellationToken = default);
}
public interface ISettingManager
{
    Task<IReadOnlyList<ST_SYSTEM_PARAMETER>> LoadSection(
        EN_SETTING_TAB section,
        CancellationToken cancellationToken = default);

    Task SaveSection(
        EN_SETTING_TAB section,
        IReadOnlyList<ST_SYSTEM_PARAMETER> parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ST_SETTING_HISTORY>> LoadHistory(
        EN_SETTING_TAB section,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ST_INTERFACE_DATA>> LoadInterfaceList(
        CancellationToken cancellationToken = default);

    Task SaveInterfaceList(
        IReadOnlyList<ST_INTERFACE_DATA> interfaces,
        CancellationToken cancellationToken = default);

    Task ConnectInterface(
        EN_EQP_MODULE module,
        int number,
        CancellationToken cancellationToken = default);

    Task DisconnectInterface(
        EN_EQP_MODULE module,
        int number,
        CancellationToken cancellationToken = default);

    Task ReconnectInterface(
        EN_EQP_MODULE module,
        int number,
        CancellationToken cancellationToken = default);
}

public sealed class CSettingManager(
    ISettingFile settingFile,
    IInterfaceFile interfaceFile,
    IInterfaceManager interfaceManager) : ISettingManager
{
    public Task<IReadOnlyList<ST_SYSTEM_PARAMETER>> LoadSection(
        EN_SETTING_TAB section,
        CancellationToken cancellationToken = default)
    {
        return settingFile.Load(section, cancellationToken);
    }

    public Task SaveSection(
        EN_SETTING_TAB section,
        IReadOnlyList<ST_SYSTEM_PARAMETER> parameters,
        CancellationToken cancellationToken = default)
    {
        return settingFile.Save(section, parameters, cancellationToken);
    }

    public Task<IReadOnlyList<ST_SETTING_HISTORY>> LoadHistory(
        EN_SETTING_TAB section,
        CancellationToken cancellationToken = default)
    {
        return settingFile.LoadHistory(section, cancellationToken);
    }

    public Task<IReadOnlyList<ST_INTERFACE_DATA>> LoadInterfaceList(
        CancellationToken cancellationToken = default)
    {
        return interfaceFile.LoadAll(cancellationToken);
    }

    public async Task SaveInterfaceList(
        IReadOnlyList<ST_INTERFACE_DATA> interfaces,
        CancellationToken cancellationToken = default)
    {
        await interfaceFile.SaveAll(interfaces, cancellationToken);
        await interfaceManager.Reload(interfaces, reconnect: true, cancellationToken);
    }

    public Task ConnectInterface(
        EN_EQP_MODULE module,
        int number,
        CancellationToken cancellationToken = default)
    {
        return interfaceManager.Connect(module, number, cancellationToken: cancellationToken);
    }

    public Task DisconnectInterface(
        EN_EQP_MODULE module,
        int number,
        CancellationToken cancellationToken = default)
    {
        return interfaceManager.Disconnect(module, number, cancellationToken);
    }

    public Task ReconnectInterface(
        EN_EQP_MODULE module,
        int number,
        CancellationToken cancellationToken = default)
    {
        return interfaceManager.Reconnect(module, number, cancellationToken);
    }
}

