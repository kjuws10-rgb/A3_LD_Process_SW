# A3 LD Process SW Class Inventory

Generated from Debug build assemblies. Compiler generated backing fields are hidden; explicit private fields, properties, constructors, events, enum values, and declared methods are listed.

## Drilling.Common

Type count: 154

### Drilling.Common.Alarm.CAlarmManager

- Kind: class
- Constructors:
  - public CAlarmManager()
- Fields:
  - private static readonly Collections.Generic.IReadOnlyDictionary<String, Drilling.Common.Alarm.CAlarmManager+ST_ALARM_RULE> InterLockAlarmRules
  - private readonly Collections.Generic.Dictionary<Int32, DateTimeOffset> _activeSince
- Methods:
  - private static Void AddInterLockAlarms(Collections.Generic.ICollection<Drilling.Common.Alarm.CAlarmManager+ST_ALARM_CANDIDATE> alarms, Drilling.Common.InterLock.ST_INTERLOCK_SUMMARY interLock)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Alarm.ST_ALARM_DATA> Build(Drilling.Common.Managers.ST_DEVICE_STATUS status, Drilling.Common.InterLock.ST_INTERLOCK_SUMMARY interLock)
  - private static String GetStationNameFromAxis(String axisId)
  - public Void Reset()
  - private Drilling.Common.Alarm.ST_ALARM_DATA ToAlarmData(Drilling.Common.Alarm.CAlarmManager+ST_ALARM_CANDIDATE candidate, DateTimeOffset now)

### Drilling.Common.Alarm.EN_ALARM_LEVEL

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Info = 0
  - Warning = 1
  - Critical = 2

### Drilling.Common.Alarm.EN_ALARM_STATE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Clear = 0
  - Occur = 1

### Drilling.Common.Alarm.CAlarmManager+ST_ALARM_CANDIDATE

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Alarm.CAlarmManager+ST_ALARM_CANDIDATE>
- Constructors:
  - public ST_ALARM_CANDIDATE(Int32 Code, Drilling.Common.Alarm.EN_ALARM_LEVEL Severity, String Message, String RecoveryAction, String Device, String StationName)
  - private ST_ALARM_CANDIDATE(Drilling.Common.Alarm.CAlarmManager+ST_ALARM_CANDIDATE original)
- Properties:
  - public Int32 Code (get/set)
  - public String Device (get/set)
  - private Type EqualityContract (get)
  - public String Message (get/set)
  - public String RecoveryAction (get/set)
  - public Drilling.Common.Alarm.EN_ALARM_LEVEL Severity (get/set)
  - public String StationName (get/set)

### Drilling.Common.Alarm.ST_ALARM_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Alarm.ST_ALARM_DATA>
- Constructors:
  - public ST_ALARM_DATA(Int32 Code, Drilling.Common.Alarm.EN_ALARM_LEVEL Severity, String Message, String RecoveryAction, DateTimeOffset OccurredAt, String Device = SYSTEM, String StationName = COMMON)
- Properties:
  - public Int32 Code (get/set)
  - public String Device (get/set)
  - private Type EqualityContract (get)
  - public String Message (get/set)
  - public DateTimeOffset OccurredAt (get/set)
  - public String RecoveryAction (get/set)
  - public Drilling.Common.Alarm.EN_ALARM_LEVEL Severity (get/set)
  - public String StationName (get/set)

### Drilling.Common.Alarm.CAlarmManager+ST_ALARM_RULE

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Alarm.CAlarmManager+ST_ALARM_RULE>
- Constructors:
  - public ST_ALARM_RULE(Int32 Code, Drilling.Common.Alarm.EN_ALARM_LEVEL Severity, String Device, String StationName, String RecoveryAction)
  - private ST_ALARM_RULE(Drilling.Common.Alarm.CAlarmManager+ST_ALARM_RULE original)
- Properties:
  - public Int32 Code (get/set)
  - public String Device (get/set)
  - private Type EqualityContract (get)
  - public String RecoveryAction (get/set)
  - public Drilling.Common.Alarm.EN_ALARM_LEVEL Severity (get/set)
  - public String StationName (get/set)

### Drilling.Common.Interface.CBeamExpander

- Kind: class
- Base: Drilling.Common.Interface.CSerialComm
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CBeamExpander(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Methods:
  - public static Drilling.Common.Interface.ST_BET_STATUS Apply(Drilling.Common.Interface.EN_BET_COMMAND command, Double magnification, Double divergence, String response, Drilling.Common.Interface.ST_BET_STATUS current, Boolean simulation)
  - public static String Build(Drilling.Common.Interface.EN_BET_COMMAND command, Double magnification = 0, Double divergence = 0)
  - private static String CreateSimulationResponse(Drilling.Common.Interface.EN_BET_COMMAND command, Double magnification, Double divergence, Drilling.Common.Interface.ST_BET_STATUS current)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task<String> ExecuteBeamExpander(String function, Threading.CancellationToken cancellationToken)
  - private String ExecuteBeamExpander(String function)
  - public static Boolean IsSuccessResponse(String response)
  - private static Double ReadBeamPosition(String response)
  - private String ReadDeviceResponse()
  - private static Drilling.Common.Interface.EN_BET_ERROR ReadError(String response)
  - private static Double ReadTaggedDouble(String response, String tag, Double defaultValue)
  - private Boolean SendAndWaitAck(String command)
  - private String SendMove(Double magnification, Double divergence)
  - private String SendPolling(String command)
  - private String SendSelecting(String command)
  - private static Int32 ToMotorStep(Double value)

### Drilling.Common.Interface.CComm

- Kind: static class
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<Drilling.Common.Interface.CComm+CCommRegistration> CommTypes
- Methods:
  - public static Drilling.Common.Interface.IComm Create(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Interface.CComm+CCommRegistration> LoadCommTypes()

### Drilling.Common.Interface.CCommBase

- Kind: class
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - protected CCommBase(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Fields:
  - protected readonly Drilling.Common.Interface.ST_INTERFACE_DATA Data
  - protected readonly Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION Option
- Properties:
  - public Drilling.Common.Interface.EN_COMM_STATE ConnectionState (get/set)
  - public String Endpoint (get)
  - public Nullable<DateTimeOffset> LastChangedAt (get/set)
  - public String LastError (get/set)
  - public String LastReceived (get/set)
  - public String LastSent (get/set)
- Methods:
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)
  - protected Void SetError(Exception ex)
  - protected Void SetError(String message)
  - protected Void SetState(Drilling.Common.Interface.EN_COMM_STATE state)

### Drilling.Common.Interface.CComm+CCommRegistration

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.CComm+CCommRegistration>
- Constructors:
  - public CCommRegistration(String InterfaceType, Collections.Generic.IReadOnlyList<String> DeviceNames, Type CommType)
  - private CCommRegistration(Drilling.Common.Interface.CComm+CCommRegistration original)
- Properties:
  - public Type CommType (get/set)
  - public Collections.Generic.IReadOnlyList<String> DeviceNames (get/set)
  - private Type EqualityContract (get)
  - public String InterfaceType (get/set)
- Methods:
  - public Boolean IsMatch(String interfaceType, String deviceName)

### Drilling.Common.Interface.CCommTypeAttribute

- Kind: class
- Base: System.Attribute
- Constructors:
  - public CCommTypeAttribute(String interfaceType, String[] deviceNames)
- Properties:
  - public Collections.Generic.IReadOnlyList<String> DeviceNames (get)
  - public String InterfaceType (get)
- Methods:
  - public static String NormalizeName(String value)

### Drilling.Common.Interface.CConex_AGP

- Kind: class
- Base: Drilling.Common.Interface.CSerialComm
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CConex_AGP(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Methods:
  - public static Drilling.Common.Interface.ST_ATTENUATOR_STATUS Apply(Drilling.Common.Interface.EN_ATTENUATOR_COMMAND command, Double parameter, String response, Drilling.Common.Interface.ST_ATTENUATOR_STATUS current, Boolean simulation)
  - public static String Build(Drilling.Common.Interface.EN_ATTENUATOR_COMMAND command, Double parameter = 0)
  - private static String CreateSimulationResponse(Drilling.Common.Interface.EN_ATTENUATOR_COMMAND command, Double parameter, Drilling.Common.Interface.ST_ATTENUATOR_STATUS current)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task<String> ExecuteConex(String function, Threading.CancellationToken cancellationToken)
  - private String ExecuteConex(String function)
  - private String FormatDeviceCommand(String command, String value)
  - private static Boolean IsMovingState(String value)
  - public static Boolean IsSuccessResponse(String response)
  - private static String NormalizeControllerState(String value)
  - private Int32 ReadControllerAddress()
  - private static String ReadControllerState(String response, String defaultState)
  - private static Drilling.Common.Interface.EN_CONEX_AGP_ERROR ReadError(String response)
  - private static Double ReadTaggedDouble(String response, String tag, Double defaultValue)
  - private static String ReadTsError(String value)
  - private static String ReadTsState(String value)
  - private String SendPolling(String command)
  - private String SendSelecting(String command, String value)
  - private static Boolean TryParseResponse(String response, Int32& address, String& command, String& value)

### Drilling.Common.Interface.CInterfaceConnectOption

- Kind: static class
- Methods:
  - private static Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION CreateAcsOption(Collections.Generic.IReadOnlyList<String> args)
  - private static Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION CreateOpcUaOption(Collections.Generic.IReadOnlyList<String> args)
  - private static Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION CreateSerialOption(Drilling.Common.Interface.ST_INTERFACE_DATA data, Collections.Generic.IReadOnlyList<String> args)
  - private static Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION CreateSocketOption(Collections.Generic.IReadOnlyList<String> args)
  - private static String DefaultIfBlank(String value, String defaultValue)
  - public static Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION Parse(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private static String ReadExtra(Drilling.Common.Interface.ST_INTERFACE_DATA data, String[] names)
  - private static Int32 ReadInt(String value, Int32 defaultValue)

### Drilling.Common.Interface.CInterfaceDevice

- Kind: class
- Interfaces: Drilling.Common.Interface.IInterfaceDevice
- Constructors:
  - public CInterfaceDevice(Drilling.Common.Interface.ST_INTERFACE_DATA data, Boolean simulationMode)
- Fields:
  - private readonly Drilling.Common.Interface.IComm _comm
  - private Nullable<DateTimeOffset> _simulationLastChangedAt
  - private String _simulationLastError
  - private String _simulationLastReceived
  - private String _simulationLastSent
  - private Boolean _simulationMode
- Properties:
  - public Drilling.Common.Interface.EN_COMM_STATE ConnectionState (get)
  - public Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION ConnectOption (get)
  - public Drilling.Common.Interface.ST_INTERFACE_DATA Data (get)
  - public Boolean IsSimulation (get)
- Methods:
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> ExecuteFunction(String function, Threading.CancellationToken cancellationToken = null)
  - public Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS GetCommunicationStatus()
  - public Void SetSimulationMode(Boolean enabled)
  - private Void TouchSimulationState()

### Drilling.Common.Interface.CInterfaceManager

- Kind: class
- Interfaces: Drilling.Common.Interface.IInterfaceManager
- Constructors:
  - public CInterfaceManager(Nullable<Boolean> simulationMode = null, Drilling.Common.Log.ILogManager logManager = null, Drilling.Common.Interface.IBETFile betFile = null, Drilling.Common.Interface.IPowerMeterFile powerMeterFile = null)
- Fields:
  - private readonly Collections.Generic.Dictionary<Int32, Drilling.Common.Interface.ST_ATTENUATOR_STATUS> _attenuatorStatuses
  - private readonly Drilling.Common.Interface.IBETFile _betFile
  - private readonly Collections.Generic.Dictionary<Int32, Drilling.Common.Interface.ST_BET_STATUS> _betStatuses
  - private readonly Collections.Generic.Dictionary<Int32, Drilling.Common.Interface.ST_ORION_CHILLER_STATUS> _chillerStatuses
  - private readonly Collections.Generic.Dictionary<String, Drilling.Common.Interface.CInterfaceDevice> _devices
  - private readonly Collections.Generic.Dictionary<Int32, Collections.Generic.HashSet<Int32>> _laserOnHeads
  - private readonly Drilling.Common.Log.ILogManager _logManager
  - private readonly Drilling.Common.Interface.IPowerMeterFile _powerMeterFile
  - private readonly Collections.Generic.Dictionary<Int32, Drilling.Common.Managers.ST_POWER_METER_STATUS> _powerMeterStatuses
  - private Nullable<Boolean> _simulationMode
  - private readonly Collections.Generic.Dictionary<Int32, Drilling.Common.Interface.ST_TALON_STATUS> _talonStatuses
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.IInterfaceDevice> Devices (get)
  - public Boolean IsSimulation (get)
- Methods:
  - private Void ClearDeviceStateMaps()
  - private static Drilling.Common.Interface.EN_COMM_STATE CollapseConnectionState(Collections.Generic.IEnumerable<Drilling.Common.Interface.EN_COMM_STATE> states)
  - public Threading.Tasks.Task<Int32> Connect(Boolean init = False, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Connect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Boolean autoConnection = True, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Connect(String nickName, Boolean autoConnection = True, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task ConnectDevice(Drilling.Common.Interface.CInterfaceDevice device, Boolean autoConnection, Threading.CancellationToken cancellationToken)
  - private static Drilling.Common.Interface.ST_ATTENUATOR_STATUS CreateDefaultAttenuatorStatus()
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA> CreateDefaultBETData()
  - private static Drilling.Common.Interface.ST_BET_STATUS CreateDefaultBETStatus()
  - private static Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA CreateDefaultPowerMeterData(String processFile)
  - private static String CreateDeviceKey(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Threading.Tasks.Task Destroy(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Int32> Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(String nickName, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task DisconnectDevice(Drilling.Common.Interface.CInterfaceDevice device, Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteAttenuatorCommand(Drilling.Common.Interface.EN_ATTENUATOR_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteAttenuatorCommand(Int32 number, Drilling.Common.Interface.EN_ATTENUATOR_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteBETCommand(Drilling.Common.Interface.EN_BET_COMMAND command, Double parameter1 = 0, Double parameter2 = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteBETCommand(Int32 number, Drilling.Common.Interface.EN_BET_COMMAND command, Double parameter1 = 0, Double parameter2 = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteChillerCommand(Drilling.Common.Interface.EN_CHILLER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteChillerCommand(Int32 number, Drilling.Common.Interface.EN_CHILLER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task<String> ExecuteDeviceFunction(Drilling.Common.Interface.CInterfaceDevice device, String function, Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task<String> ExecuteFunction(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, String function, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> ExecuteFunction(String nickName, String function, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecutePowerMeterCommand(Drilling.Common.Interface.EN_POWER_METER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecutePowerMeterCommand(Int32 number, Drilling.Common.Interface.EN_POWER_METER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteTalonLaserCommand(Drilling.Common.Interface.EN_TALON_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteTalonLaserCommand(Int32 number, Drilling.Common.Interface.EN_TALON_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - private static String FormatConnectionState(Drilling.Common.Interface.EN_COMM_STATE state)
  - private static String FormatDeviceName(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private static String FormatDeviceName(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetAttenuatorInterfaceData()
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetAttenuatorInterfaceData(Int32 number)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> GetAttenuatorStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> GetAttenuatorStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Interface.ST_ATTENUATOR_STATUS GetAttenuatorStatusValue(Int32 number)
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetBETInterfaceData()
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetBETInterfaceData(Int32 number)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> GetBETStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> GetBETStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Interface.ST_BET_STATUS GetBETStatusValue(Int32 number)
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetChillerInterfaceData()
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetChillerInterfaceData(Int32 number)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_CHILLER_STATUS> GetChillerStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_CHILLER_STATUS> GetChillerStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Interface.ST_ORION_CHILLER_STATUS GetChillerStatusValue(Int32 number)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS>> GetCommunicationStatus(Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Interface.CInterfaceDevice GetDeviceByNickNameOrThrow(String nickName)
  - private Drilling.Common.Interface.CInterfaceDevice GetDeviceOrThrow(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS> GetInterfaceCommunicationList(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null)
  - public Drilling.Common.Interface.ST_INTERFACE_DATA GetInterfaceData(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Drilling.Common.Interface.ST_INTERFACE_DATA GetInterfaceData(String nickName)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> GetInterfaceList(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null)
  - private Collections.Generic.HashSet<Int32> GetLaserOnHeads(Int32 number)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_LASER_STATUS> GetLaserStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_LASER_STATUS> GetLaserStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetPowerMeterInterfaceData()
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetPowerMeterInterfaceData(Int32 number)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> GetPowerMeterStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> GetPowerMeterStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Managers.ST_POWER_METER_STATUS GetPowerMeterStatusValue(Int32 number)
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetTalonInterfaceData()
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetTalonInterfaceData(Int32 number)
  - private Drilling.Common.Interface.ST_TALON_STATUS GetTalonStatus(Int32 number)
  - public Threading.Tasks.Task Initialize(Threading.CancellationToken cancellationToken = null)
  - public Boolean IsConnect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Boolean IsConnect(String nickName)
  - private Boolean IsInterfaceSimulation(Drilling.Common.Interface.ST_INTERFACE_DATA interfaceData)
  - public Boolean IsSimul(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Boolean IsSimul(String nickName)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA>> LoadBETData(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA> LoadPowerMeterData(String processFile = , Threading.CancellationToken cancellationToken = null)
  - private static String NormalizeNickName(String nickName)
  - private Void PruneDeviceStateMap(Collections.Generic.Dictionary<Int32, T> statusMap, Drilling.Common.Interface.EN_EQP_MODULE module)
  - private Void PruneDeviceStateMaps()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_HISTORY>> ReadInterfaceHistory(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null, String nickName = , Int32 maxRows = 100, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Reconnect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Reconnect(String nickName, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> RefreshAttenuatorStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> RefreshAttenuatorStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> RefreshBETStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> RefreshBETStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ORION_CHILLER_STATUS> RefreshChillerStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ORION_CHILLER_STATUS> RefreshChillerStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> RefreshPowerMeterStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> RefreshPowerMeterStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_TALON_STATUS> RefreshTalonLaserStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_TALON_STATUS> RefreshTalonLaserStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Void Register(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - public Threading.Tasks.Task Reload(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces, Boolean reconnect = True, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SavePowerMeterData(String processFile, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> steps, Threading.CancellationToken cancellationToken = null)
  - private Void SetAttenuatorStatus(Int32 number, Drilling.Common.Interface.ST_ATTENUATOR_STATUS status)
  - private Void SetBETStatus(Int32 number, Drilling.Common.Interface.ST_BET_STATUS status)
  - private Void SetChillerStatus(Int32 number, Drilling.Common.Interface.ST_ORION_CHILLER_STATUS status)
  - public Threading.Tasks.Task SetLaser(Int32 headNo, Boolean enabled, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SetLaser(Int32 number, Int32 headNo, Boolean enabled, Threading.CancellationToken cancellationToken = null)
  - private Void SetPowerMeterStatus(Int32 number, Drilling.Common.Managers.ST_POWER_METER_STATUS status)
  - public Void SetSimulationMode(Boolean enabled)
  - private Void SetSimulLaserHead(Int32 number, Int32 headNo, Boolean enabled)
  - private Void SetTalonStatus(Int32 number, Drilling.Common.Interface.ST_TALON_STATUS status)
  - private Boolean TryGetDeviceByNickName(String nickName, Drilling.Common.Interface.CInterfaceDevice& device, Boolean throwIfAmbiguous = False)
  - private Void WriteCommandLog(Drilling.Common.Interface.CInterfaceDevice device, String command, String response)
  - private Void WriteConnectionLog(String action, Drilling.Common.Interface.CInterfaceDevice device, Drilling.Common.Interface.EN_COMM_STATE beforeState)
  - private Void WriteErrorLog(Drilling.Common.Interface.CInterfaceDevice device, String command, String detail)

### Drilling.Common.Interface.COrionChiller

- Kind: class
- Base: Drilling.Common.Interface.CSerialComm
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public COrionChiller(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Fields:
  - private static Byte Ack
  - private static Int32 DataLength
  - private static Int32 DeviceAddress
  - private static Byte Enq
  - private static Byte Eot
  - private static Byte Etx
  - private static Byte Nak
  - private static Byte Stx
- Methods:
  - public static Drilling.Common.Interface.ST_ORION_CHILLER_STATUS Apply(Drilling.Common.Interface.EN_CHILLER_COMMAND command, Double parameter, String response, Drilling.Common.Interface.ST_ORION_CHILLER_STATUS current, Boolean simulation)
  - public static String Build(Drilling.Common.Interface.EN_CHILLER_COMMAND command, Double parameter)
  - private static Byte CalcBcc(Collections.Generic.IEnumerable<Byte> bytes)
  - private static String CreateRunData(Int32 state)
  - private static String CreateSimulationResponse(Drilling.Common.Interface.EN_CHILLER_COMMAND command, Double parameter, Drilling.Common.Interface.ST_ORION_CHILLER_STATUS current)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task<String> ExecuteOrion(String function, Threading.CancellationToken cancellationToken)
  - private String ExecuteOrion(String function)
  - private static String FormatAddress()
  - private static String FormatTemperatureData(Double celsius)
  - public static Boolean IsSuccessResponse(String response)
  - private static String NormalizeId(String id)
  - private static Drilling.Common.Interface.EN_CHILLER_ERROR ReadError(String response)
  - private Byte[] ReadFrame()
  - private static String ReadPollingData(String response, String id)
  - private static Double ReadPollingDouble(String response, String id)
  - private static Drilling.Common.Interface.EN_CHILLER_RUN_STATE ReadRunState(String response)
  - private String SendPolling(String id)
  - private String SendSelecting(String id, String data)
  - private static Boolean TryParsePollingFrame(Collections.Generic.IReadOnlyList<Byte> frame, String& id, String& data)
  - private Int32 WaitAck()

### Drilling.Common.Interface.CPowerMeter

- Kind: class
- Base: Drilling.Common.Interface.CSerialComm
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CPowerMeter(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Properties:
  - protected String CommandNewLine (get)
- Methods:
  - public static Drilling.Common.Managers.ST_POWER_METER_STATUS Apply(Drilling.Common.Interface.EN_POWER_METER_COMMAND command, Double parameter, String response, Drilling.Common.Managers.ST_POWER_METER_STATUS current, Boolean simulation)
  - private static Drilling.Common.Managers.ST_POWER_METER_STATUS ApplyBeamPosition(Drilling.Common.Managers.ST_POWER_METER_STATUS current, String value)
  - private static Drilling.Common.Managers.ST_POWER_METER_STATUS ApplyPowerValue(Drilling.Common.Managers.ST_POWER_METER_STATUS current, Double power)
  - public static String Build(Drilling.Common.Interface.EN_POWER_METER_COMMAND command, Double parameter = 0)
  - private static String CreateSimulationResponse(Drilling.Common.Interface.EN_POWER_METER_COMMAND command, Double parameter, Drilling.Common.Managers.ST_POWER_METER_STATUS current)
  - protected String FormatCommand(String function)
  - public static Boolean IsSuccessResponse(String response)
  - private static Double ReadDouble(String value)
  - private static String ReadLeadingNumber(String value)
  - private static Double ReadWaveLengthNm(String value)
  - private static Double ToMeter(Double waveLengthNm)

### Drilling.Common.Interface.CReadyOnlyComm

- Kind: class
- Base: Drilling.Common.Interface.CCommBase
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CReadyOnlyComm(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Methods:
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Interface.CSerialComm

- Kind: class
- Base: Drilling.Common.Interface.CCommBase
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CSerialComm(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Fields:
  - protected readonly Threading.SemaphoreSlim SerialLock
  - protected IO.Ports.SerialPort SerialPort
- Properties:
  - protected String CommandNewLine (get)
- Methods:
  - protected Void CloseSerialPort()
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task<String> ExecuteSerial(String function, Threading.CancellationToken cancellationToken)
  - protected String FormatCommand(String function)
  - private static String NormalizeEnumValue(String value)
  - private static IO.Ports.Handshake ParseHandshake(String value)
  - private static IO.Ports.Parity ParseParity(String value)
  - private static IO.Ports.StopBits ParseStopBits(String value)

### Drilling.Common.Interface.CSocketComm

- Kind: class
- Base: Drilling.Common.Interface.CCommBase
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CSocketComm(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Fields:
  - private Net.Sockets.TcpClient _client
- Methods:
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task DisconnectSocket()
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task<String> ReadResponse(Net.Sockets.NetworkStream stream, Threading.CancellationToken cancellationToken)

### Drilling.Common.Interface.CTalonLaser

- Kind: class
- Base: Drilling.Common.Interface.CSerialComm
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CTalonLaser(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Properties:
  - protected String CommandNewLine (get)
- Methods:
  - public static Drilling.Common.Interface.ST_TALON_STATUS Apply(Drilling.Common.Interface.EN_TALON_COMMAND command, Double parameter, String response, Drilling.Common.Interface.ST_TALON_STATUS current, Boolean simulation)
  - public static String Build(Drilling.Common.Interface.EN_TALON_COMMAND command, Double parameter)
  - private static String CreateSimulationResponse(Drilling.Common.Interface.EN_TALON_COMMAND command, Double parameter, Drilling.Common.Interface.ST_TALON_STATUS current)
  - protected String FormatCommand(String function)
  - public static Boolean IsValidResponse(String response)
  - private static Boolean ReadBool(String value)
  - private static Double ReadDouble(String value)
  - private static Int32 ReadInt(String value)
  - private static Boolean ReadLaserEmission(String value)
  - private static String ReadLeadingNumber(String value)

### Drilling.Common.Interface.EN_ATTENUATOR_COMMAND

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - MoveAbs = 0
  - MoveRel = 1
  - Home = 2
  - Stop = 3
  - ResetAlarm = 4
  - Refresh = 5
  - PollCurrentPosition = 6
  - PollTargetPosition = 7
  - PollState = 8

### Drilling.Common.Interface.EN_BET_COMMAND

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - InitMotor = 0
  - MoveManual = 1
  - MoveTable = 2
  - Stop = 3
  - ResetAlarm = 4
  - Refresh = 5
  - PollMagnificationPosition = 6
  - PollDivergencePosition = 7

### Drilling.Common.Interface.EN_BET_ERROR

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ok = 0
  - Error = 1
  - NotSupported = -99
  - InvalidResponse = -2
  - Timeout = -1

### Drilling.Common.Interface.EN_CHILLER_COMMAND

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Run = 0
  - Stop = 1
  - PumpOnly = 2
  - SetTemperature = 3
  - ResetAlarm = 4
  - PollLiquidTemp = 5
  - PollSetTemp = 6
  - PollRunState = 7
  - PollAlarmCode = 8

### Drilling.Common.Interface.EN_CHILLER_ERROR

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ok = 0
  - Error = 1
  - NotSupported = -99
  - InvalidResponse = -2
  - Timeout = -1

### Drilling.Common.Interface.EN_CHILLER_RUN_STATE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Stop = 0
  - Run = 1
  - PumpOnly = 2

### Drilling.Common.Interface.EN_COMM_STATE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Offline = 0
  - Simulation = 1
  - Online = 2

### Drilling.Common.Interface.EN_CONEX_AGP_ERROR

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ok = 0
  - Error = 1
  - NotSupported = -99
  - InvalidResponse = -2
  - Timeout = -1

### Drilling.Common.Interface.EN_EQP_MODULE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - WonikCtrl = 0
  - Vision = 1
  - Automation1 = 2
  - Motion = 3
  - TalonLaser = 4
  - Chiller = 5
  - Attenuator = 6
  - Bet = 7
  - PowerMeter = 8

### Drilling.Common.Interface.EN_INTERFACE_TYPE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - OpcUa = 0
  - ModbusSerial = 1
  - ModbusTcp = 2
  - Serial = 3
  - SocketClient = 4
  - SocketServer = 5
  - SocketClientUdp = 6
  - SocketServerUdp = 7
  - AcsNet = 8

### Drilling.Common.Interface.EN_POWER_METER_COMMAND

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - ReadPower = 0
  - QueryHardwareDescription = 1
  - QuerySerialNumber = 2
  - QueryWaveLength = 3
  - SetWaveLength = 4
  - QueryBeamPosition = 5
  - StartStreaming = 6
  - StopStreaming = 7
  - Reset = 8
  - Refresh = 9

### Drilling.Common.Interface.EN_POWER_METER_ERROR

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ok = 0
  - Error = 1
  - NotSupported = -99
  - InvalidResponse = -2
  - Timeout = -1

### Drilling.Common.Interface.EN_TALON_COMMAND

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - SetDiodeCurrent = 0
  - SetQsw = 1
  - SetEprf = 2
  - SetLaserOnOff = 3
  - SetShutterOpenClose = 4
  - SetGateOpenClose = 5
  - SetExtGateEnableDisable = 6
  - SetShg = 7
  - SetShgAutotune = 8
  - SetQMode = 9
  - GetDiodeCurrent = 10
  - GetQsw = 11
  - GetEprf = 12
  - GetShutterOpenClose = 13
  - GetGateOpenClose = 14
  - GetExtGateEnableDisable = 15
  - GetOutputPower = 16
  - GetShg = 17
  - GetShgAutotune = 18
  - GetThgSpot = 19
  - GetThgHour = 20
  - GetQMode = 21
  - GetDiodeTemp = 22
  - GetTowerTemp = 23
  - GetLaserOnOff = 24
  - RequestStatusString = 25
  - RequestStatusCode = 26
  - RequestSave = 27

### Drilling.Common.Interface.EN_TALON_ERROR

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ok = 0
  - Warning = 1
  - Error = 2
  - InvalidResponse = -2
  - Timeout = -1

### Drilling.Common.Interface.IBETFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA>> Load(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA> table, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Interface.IComm

- Kind: interface
- Properties:
  - public Drilling.Common.Interface.EN_COMM_STATE ConnectionState (get)
  - public String Endpoint (get)
  - public Nullable<DateTimeOffset> LastChangedAt (get)
  - public String LastError (get)
  - public String LastReceived (get)
  - public String LastSent (get)
- Methods:
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Interface.IInterfaceDevice

- Kind: interface
- Properties:
  - public Drilling.Common.Interface.EN_COMM_STATE ConnectionState (get)
  - public Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION ConnectOption (get)
  - public Drilling.Common.Interface.ST_INTERFACE_DATA Data (get)
  - public Boolean IsSimulation (get)
- Methods:
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> ExecuteFunction(String function, Threading.CancellationToken cancellationToken = null)
  - public Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS GetCommunicationStatus()

### Drilling.Common.Interface.IInterfaceManager

- Kind: interface
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.IInterfaceDevice> Devices (get)
  - public Boolean IsSimulation (get)
- Methods:
  - public Threading.Tasks.Task<Int32> Connect(Boolean init = False, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Connect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Boolean autoConnection = True, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Connect(String nickName, Boolean autoConnection = True, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Destroy(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Int32> Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Disconnect(String nickName, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteAttenuatorCommand(Drilling.Common.Interface.EN_ATTENUATOR_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteAttenuatorCommand(Int32 number, Drilling.Common.Interface.EN_ATTENUATOR_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteBETCommand(Drilling.Common.Interface.EN_BET_COMMAND command, Double parameter1 = 0, Double parameter2 = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteBETCommand(Int32 number, Drilling.Common.Interface.EN_BET_COMMAND command, Double parameter1 = 0, Double parameter2 = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteChillerCommand(Drilling.Common.Interface.EN_CHILLER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteChillerCommand(Int32 number, Drilling.Common.Interface.EN_CHILLER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> ExecuteFunction(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, String function, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> ExecuteFunction(String nickName, String function, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecutePowerMeterCommand(Drilling.Common.Interface.EN_POWER_METER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecutePowerMeterCommand(Int32 number, Drilling.Common.Interface.EN_POWER_METER_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteTalonLaserCommand(Drilling.Common.Interface.EN_TALON_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteTalonLaserCommand(Int32 number, Drilling.Common.Interface.EN_TALON_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> GetAttenuatorStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> GetAttenuatorStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> GetBETStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> GetBETStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_CHILLER_STATUS> GetChillerStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_CHILLER_STATUS> GetChillerStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS>> GetCommunicationStatus(Threading.CancellationToken cancellationToken = null)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS> GetInterfaceCommunicationList(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null)
  - public Drilling.Common.Interface.ST_INTERFACE_DATA GetInterfaceData(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Drilling.Common.Interface.ST_INTERFACE_DATA GetInterfaceData(String nickName)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> GetInterfaceList(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_LASER_STATUS> GetLaserStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_LASER_STATUS> GetLaserStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> GetPowerMeterStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> GetPowerMeterStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Initialize(Threading.CancellationToken cancellationToken = null)
  - public Boolean IsConnect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Boolean IsConnect(String nickName)
  - public Boolean IsSimul(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number)
  - public Boolean IsSimul(String nickName)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA>> LoadBETData(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA> LoadPowerMeterData(String processFile = , Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_HISTORY>> ReadInterfaceHistory(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null, String nickName = , Int32 maxRows = 100, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Reconnect(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Reconnect(String nickName, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> RefreshAttenuatorStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ATTENUATOR_STATUS> RefreshAttenuatorStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> RefreshBETStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_BET_STATUS> RefreshBETStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ORION_CHILLER_STATUS> RefreshChillerStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_ORION_CHILLER_STATUS> RefreshChillerStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> RefreshPowerMeterStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_POWER_METER_STATUS> RefreshPowerMeterStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_TALON_STATUS> RefreshTalonLaserStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_TALON_STATUS> RefreshTalonLaserStatus(Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Void Register(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - public Threading.Tasks.Task Reload(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces, Boolean reconnect = True, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SavePowerMeterData(String processFile, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> steps, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SetLaser(Int32 headNo, Boolean enabled, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SetLaser(Int32 number, Int32 headNo, Boolean enabled, Threading.CancellationToken cancellationToken = null)
  - public Void SetSimulationMode(Boolean enabled)

### Drilling.Common.Interface.IPowerMeterFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<String>> List(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA> Load(String processFile = , Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(String processFile, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> steps, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Interface.ST_ATTENUATOR_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_ATTENUATOR_STATUS>
- Constructors:
  - public ST_ATTENUATOR_STATUS(Double CurrentPosition, Double TargetPosition, String CommandState, Boolean CommOk = True, Drilling.Common.Interface.EN_CONEX_AGP_ERROR LastError = Ok, Nullable<DateTimeOffset> UpdatedAt = null)
- Properties:
  - public String CommandState (get/set)
  - public Boolean CommOk (get/set)
  - public Double CurrentPosition (get/set)
  - private Type EqualityContract (get)
  - public Drilling.Common.Interface.EN_CONEX_AGP_ERROR LastError (get/set)
  - public Double TargetPosition (get/set)
  - public Nullable<DateTimeOffset> UpdatedAt (get/set)

### Drilling.Common.Interface.ST_BET_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_BET_STATUS>
- Constructors:
  - public ST_BET_STATUS(Double CurrentMagnification, Double TargetMagnification, Double CurrentDivergence, Double TargetDivergence, Double MagnificationAxisPosition, Double DivergenceAxisPosition, Boolean IsMoving, Boolean MagHomeCompleted, Boolean DivHomeCompleted, Boolean AlarmOn, Boolean CommOk = True, Drilling.Common.Interface.EN_BET_ERROR LastError = Ok, Nullable<DateTimeOffset> UpdatedAt = null, String LastCommand = )
- Properties:
  - public Boolean AlarmOn (get/set)
  - public Boolean CommOk (get/set)
  - public Double CurrentDivergence (get/set)
  - public Double CurrentMagnification (get/set)
  - public Double DivergenceAxisPosition (get/set)
  - public Boolean DivHomeCompleted (get/set)
  - private Type EqualityContract (get)
  - public Boolean IsMoving (get/set)
  - public String LastCommand (get/set)
  - public Drilling.Common.Interface.EN_BET_ERROR LastError (get/set)
  - public Boolean MagHomeCompleted (get/set)
  - public Double MagnificationAxisPosition (get/set)
  - public Double TargetDivergence (get/set)
  - public Double TargetMagnification (get/set)
  - public Nullable<DateTimeOffset> UpdatedAt (get/set)

### Drilling.Common.Interface.ST_BET_TABLE_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_BET_TABLE_DATA>
- Constructors:
  - public ST_BET_TABLE_DATA(Int32 Index, Boolean Use, Double Magnification, Double Divergence, Double RowBeamSize, Double SpotSizeOffset, String Description)
- Properties:
  - public String Description (get/set)
  - public Double Divergence (get/set)
  - private Type EqualityContract (get)
  - public Int32 Index (get/set)
  - public Double Magnification (get/set)
  - public Double RowBeamSize (get/set)
  - public Double SpotSize (get)
  - public Double SpotSizeOffset (get/set)
  - public Boolean Use (get/set)

### Drilling.Common.Interface.ST_CHILLER_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_CHILLER_STATUS>
- Constructors:
  - public ST_CHILLER_STATUS(Boolean Running, Double Temperature, Double Flow, Double Pressure, Boolean AlarmOn)
- Properties:
  - public Boolean AlarmOn (get/set)
  - private Type EqualityContract (get)
  - public Double Flow (get/set)
  - public Double Pressure (get/set)
  - public Boolean Running (get/set)
  - public Double Temperature (get/set)

### Drilling.Common.Interface.ST_DEVICE_COMM_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS>
- Constructors:
  - public ST_DEVICE_COMM_STATUS(Drilling.Common.Interface.EN_EQP_MODULE Module, Drilling.Common.Interface.EN_COMM_STATE ConnectionState)
- Properties:
  - public Drilling.Common.Interface.EN_COMM_STATE ConnectionState (get/set)
  - private Type EqualityContract (get)
  - public Drilling.Common.Interface.EN_EQP_MODULE Module (get/set)

### Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT>
- Constructors:
  - public ST_DEVICE_COMMAND_RESULT(Boolean IsSuccess, String Message)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean IsSuccess (get/set)
  - public String Message (get/set)

### Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS>
- Constructors:
  - public ST_INTERFACE_COMM_STATUS(Drilling.Common.Interface.EN_EQP_MODULE Module, String NickName, Drilling.Common.Interface.EN_INTERFACE_TYPE InterfaceType, Int32 Number, Boolean AutoConnection, Drilling.Common.Interface.EN_COMM_STATE ConnectionState, Boolean IsSimulation, String Endpoint, String LastSent, String LastReceived, String LastError, Nullable<DateTimeOffset> LastChangedAt)
- Properties:
  - public Boolean AutoConnection (get/set)
  - public Drilling.Common.Interface.EN_COMM_STATE ConnectionState (get/set)
  - public String Endpoint (get/set)
  - private Type EqualityContract (get)
  - public String InstanceKey (get)
  - public Drilling.Common.Interface.EN_INTERFACE_TYPE InterfaceType (get/set)
  - public Boolean IsSimulation (get/set)
  - public Nullable<DateTimeOffset> LastChangedAt (get/set)
  - public String LastError (get/set)
  - public String LastReceived (get/set)
  - public String LastSent (get/set)
  - public Drilling.Common.Interface.EN_EQP_MODULE Module (get/set)
  - public String NickName (get/set)
  - public Int32 Number (get/set)

### Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION>
- Constructors:
  - public ST_INTERFACE_CONNECT_OPTION(String Endpoint, String LocalAddress, String RemoteAddress, Int32 Port, Int32 TimeoutMs, Int32 RetryCount, String SerialPort, Int32 BaudRate, String Parity, Int32 DataBits, String StopBits, String Handshake)
- Properties:
  - public Int32 BaudRate (get/set)
  - public Int32 DataBits (get/set)
  - public String Endpoint (get/set)
  - private Type EqualityContract (get)
  - public String Handshake (get/set)
  - public String LocalAddress (get/set)
  - public String Parity (get/set)
  - public Int32 Port (get/set)
  - public String RemoteAddress (get/set)
  - public Int32 RetryCount (get/set)
  - public String SerialPort (get/set)
  - public String StopBits (get/set)
  - public Int32 TimeoutMs (get/set)

### Drilling.Common.Interface.ST_INTERFACE_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_INTERFACE_DATA>
- Constructors:
  - public ST_INTERFACE_DATA(Drilling.Common.Interface.EN_INTERFACE_TYPE InterfaceType, Drilling.Common.Interface.EN_EQP_MODULE Device, Int32 Number, String NickName, String SystemSection, Boolean AutoConnection, Boolean IsSimulation, Collections.Generic.IReadOnlyList<String> Arguments, Collections.Generic.IReadOnlyDictionary<String, String> Extra = null)
- Properties:
  - public Collections.Generic.IReadOnlyList<String> Arguments (get/set)
  - public Boolean AutoConnection (get/set)
  - public Drilling.Common.Interface.EN_EQP_MODULE Device (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Extra (get/set)
  - public String InstanceKey (get)
  - public Drilling.Common.Interface.EN_INTERFACE_TYPE InterfaceType (get/set)
  - public Boolean IsSimulation (get/set)
  - public String NickName (get/set)
  - public Int32 Number (get/set)
  - public String SystemSection (get/set)

### Drilling.Common.Interface.ST_INTERFACE_HISTORY

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_INTERFACE_HISTORY>
- Constructors:
  - public ST_INTERFACE_HISTORY(DateTimeOffset OccurredAt, Drilling.Common.Interface.EN_EQP_MODULE Module, String NickName, String Action, String BeforeState, String AfterState, String Detail)
- Properties:
  - public String Action (get/set)
  - public String AfterState (get/set)
  - public String BeforeState (get/set)
  - public String Detail (get/set)
  - private Type EqualityContract (get)
  - public Drilling.Common.Interface.EN_EQP_MODULE Module (get/set)
  - public String NickName (get/set)
  - public DateTimeOffset OccurredAt (get/set)

### Drilling.Common.Interface.ST_LASER_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_LASER_STATUS>
- Constructors:
  - public ST_LASER_STATUS(Boolean PowerOn, Boolean ShutterOpen, Boolean GateOn, Double OutputPower)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean GateOn (get/set)
  - public Double OutputPower (get/set)
  - public Boolean PowerOn (get/set)
  - public Boolean ShutterOpen (get/set)

### Drilling.Common.Interface.ST_ORION_CHILLER_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_ORION_CHILLER_STATUS>
- Constructors:
  - public ST_ORION_CHILLER_STATUS(Double LiquidTempC, Double SetTempC, Drilling.Common.Interface.EN_CHILLER_RUN_STATE RunState, String AlarmCode, Boolean CommOk, Drilling.Common.Interface.EN_CHILLER_ERROR LastError, Nullable<DateTimeOffset> UpdatedAt)
- Properties:
  - public String AlarmCode (get/set)
  - public Boolean CommOk (get/set)
  - public Drilling.Common.Interface.ST_ORION_CHILLER_STATUS Empty (get)
  - private Type EqualityContract (get)
  - public Drilling.Common.Interface.EN_CHILLER_ERROR LastError (get/set)
  - public Double LiquidTempC (get/set)
  - public Drilling.Common.Interface.EN_CHILLER_RUN_STATE RunState (get/set)
  - public Double SetTempC (get/set)
  - public Nullable<DateTimeOffset> UpdatedAt (get/set)

### Drilling.Common.Interface.ST_POWER_METER_PROCESS_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_POWER_METER_PROCESS_DATA>
- Constructors:
  - public ST_POWER_METER_PROCESS_DATA(String FileName, Boolean IsSelected = False)
- Properties:
  - private Type EqualityContract (get)
  - public String FileName (get/set)
  - public Boolean IsSelected (get/set)

### Drilling.Common.Interface.ST_POWER_METER_STEP_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA>
- Constructors:
  - public ST_POWER_METER_STEP_DATA(Int32 StepNo, String OptionName, Boolean PowerOut, String PowerUnit, Double SettingAtt, Double SettingPower, Double SettingFreq, Int32 MeasureCycle, Int32 MeasureTimeMs, Int32 MeasureIntervalMs, Int32 StartDelayMs, Int32 CoolingTimeMs, Double Rotator, Nullable<Double> MeasurePower, String State)
- Properties:
  - public Int32 CoolingTimeMs (get/set)
  - private Type EqualityContract (get)
  - public Int32 MeasureCycle (get/set)
  - public Int32 MeasureIntervalMs (get/set)
  - public Nullable<Double> MeasurePower (get/set)
  - public Int32 MeasureTimeMs (get/set)
  - public String OptionName (get/set)
  - public Boolean PowerOut (get/set)
  - public String PowerUnit (get/set)
  - public Double Rotator (get/set)
  - public Double SettingAtt (get/set)
  - public Double SettingFreq (get/set)
  - public Double SettingPower (get/set)
  - public Int32 StartDelayMs (get/set)
  - public String State (get/set)
  - public Int32 StepNo (get/set)

### Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA>
- Constructors:
  - public ST_POWER_METER_TABLE_DATA(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_PROCESS_DATA> Processes, String SelectedFileName, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> Steps)
- Properties:
  - public Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA Empty (get)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_PROCESS_DATA> Processes (get/set)
  - public String SelectedFileName (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> Steps (get/set)

### Drilling.Common.Interface.ST_TALON_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Interface.ST_TALON_STATUS>
- Constructors:
  - public ST_TALON_STATUS(Double DiodeCurrent, Int32 Qsw, Int32 Eprf, Double DiodeTemp, Double TowerTemp, Double OutputPower, Boolean LaserOn, Boolean ShutterOpen, Boolean GateOpen, Boolean ExtGateEnable, Boolean ShgAutoTuneActive, Int32 ThgSpot, Double ThgHour, Int32 QMode, UInt32 ShgReadBackCount, String StatusMessage, Int32 StatusCode, Drilling.Common.Interface.EN_TALON_ERROR LastError)
- Properties:
  - public Double DiodeCurrent (get/set)
  - public Double DiodeTemp (get/set)
  - public Drilling.Common.Interface.ST_TALON_STATUS Empty (get)
  - public Int32 Eprf (get/set)
  - private Type EqualityContract (get)
  - public Boolean ExtGateEnable (get/set)
  - public Boolean GateOpen (get/set)
  - public Boolean LaserOn (get/set)
  - public Drilling.Common.Interface.EN_TALON_ERROR LastError (get/set)
  - public Double OutputPower (get/set)
  - public Int32 QMode (get/set)
  - public Int32 Qsw (get/set)
  - public Boolean ShgAutoTuneActive (get/set)
  - public UInt32 ShgReadBackCount (get/set)
  - public Boolean ShutterOpen (get/set)
  - public Int32 StatusCode (get/set)
  - public String StatusMessage (get/set)
  - public Double ThgHour (get/set)
  - public Int32 ThgSpot (get/set)
  - public Double TowerTemp (get/set)

### Drilling.Common.InterLock.CInterLockManager

- Kind: class
- Constructors:
  - public CInterLockManager()
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<Drilling.Common.InterLock.ST_INTERLOCK_RULE> Rules
- Methods:
  - public Drilling.Common.InterLock.ST_INTERLOCK_SUMMARY Evaluate(Drilling.Common.Managers.ST_DEVICE_STATUS status)
  - private static Drilling.Common.InterLock.CInterLockManager+ST_INTERLOCK_CHECK EvaluateRule(Drilling.Common.InterLock.ST_INTERLOCK_RULE rule, Drilling.Common.Managers.ST_DEVICE_STATUS status)
  - private static Boolean IsTargetOk(Collections.Generic.IEnumerable<Drilling.Common.InterLock.CInterLockManager+ST_INTERLOCK_CHECK> checks, Drilling.Common.InterLock.EN_INTERLOCK_TARGET target)
  - private static Drilling.Common.InterLock.ST_INTERLOCK_RULE Rule(String signal, String ioId, Boolean expectedOn, String okState, String ngDetail, Drilling.Common.InterLock.EN_INTERLOCK_LEVEL ngLevel, Drilling.Common.InterLock.EN_INTERLOCK_TARGET targets)

### Drilling.Common.InterLock.EN_INTERLOCK_LEVEL

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ok = 0
  - Warn = 1
  - Error = 2

### Drilling.Common.InterLock.EN_INTERLOCK_TARGET

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - None = 0
  - AutoRun = 1
  - ManualMove = 2
  - LaserOn = 4

### Drilling.Common.InterLock.CInterLockManager+ST_INTERLOCK_CHECK

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.InterLock.CInterLockManager+ST_INTERLOCK_CHECK>
- Constructors:
  - public ST_INTERLOCK_CHECK(Drilling.Common.InterLock.ST_INTERLOCK_RULE Rule, Boolean IsOk, Drilling.Common.InterLock.ST_INTERLOCK_ITEM Item)
  - private ST_INTERLOCK_CHECK(Drilling.Common.InterLock.CInterLockManager+ST_INTERLOCK_CHECK original)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean IsOk (get/set)
  - public Drilling.Common.InterLock.ST_INTERLOCK_ITEM Item (get/set)
  - public Drilling.Common.InterLock.ST_INTERLOCK_RULE Rule (get/set)

### Drilling.Common.InterLock.ST_INTERLOCK_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.InterLock.ST_INTERLOCK_ITEM>
- Constructors:
  - public ST_INTERLOCK_ITEM(String Signal, Drilling.Common.InterLock.EN_INTERLOCK_LEVEL Level, String State, String Detail)
- Properties:
  - public String Detail (get/set)
  - private Type EqualityContract (get)
  - public Drilling.Common.InterLock.EN_INTERLOCK_LEVEL Level (get/set)
  - public String Signal (get/set)
  - public String State (get/set)

### Drilling.Common.InterLock.ST_INTERLOCK_RULE

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.InterLock.ST_INTERLOCK_RULE>
- Constructors:
  - public ST_INTERLOCK_RULE(String Signal, String IoId, Boolean ExpectedOn, String OkState, String NgDetail, Drilling.Common.InterLock.EN_INTERLOCK_LEVEL NgLevel, Drilling.Common.InterLock.EN_INTERLOCK_TARGET Targets)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean ExpectedOn (get/set)
  - public String IoId (get/set)
  - public String NgDetail (get/set)
  - public Drilling.Common.InterLock.EN_INTERLOCK_LEVEL NgLevel (get/set)
  - public String OkState (get/set)
  - public String Signal (get/set)
  - public Drilling.Common.InterLock.EN_INTERLOCK_TARGET Targets (get/set)

### Drilling.Common.InterLock.ST_INTERLOCK_SUMMARY

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.InterLock.ST_INTERLOCK_SUMMARY>
- Constructors:
  - public ST_INTERLOCK_SUMMARY(Boolean CanAutoRun, Boolean CanManualMove, Boolean CanLaserOn, Boolean HasError, Collections.Generic.IReadOnlyList<Drilling.Common.InterLock.ST_INTERLOCK_ITEM> Items)
- Properties:
  - public Boolean CanAutoRun (get/set)
  - public Boolean CanLaserOn (get/set)
  - public Boolean CanManualMove (get/set)
  - private Type EqualityContract (get)
  - public Boolean HasError (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.InterLock.ST_INTERLOCK_ITEM> Items (get/set)

### Drilling.Common.Log.CLogManager

- Kind: class
- Interfaces: Drilling.Common.Log.ILogManager
- Constructors:
  - public CLogManager(String configRoot)
- Fields:
  - private static Int32 DefaultInterfaceReadRows
  - private static Int32 DefaultReadDays
  - private static Int32 DefaultRecipeReadRows
  - private static Int32 DefaultSettingReadRows
  - private static readonly Object FileLock
  - private readonly String _interfaceLogRoot
  - private readonly String _productLogRoot
  - private readonly String _recipeLogRoot
  - private readonly String _settingLogRoot
  - private readonly String _stationLogRoot
- Methods:
  - private static Void AppendLine(String directory, String path, String line)
  - private static Drilling.Common.Managers.ST_RECIPE_HISTORY CreateRecipeHistory(DateTimeOffset changedAt, String recipeName, String action, String itemName, String oldValue, String newValue, String tab = , String group = )
  - private Collections.Generic.IEnumerable<String> EnumerateInterfaceLogFiles(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module, Int32 days)
  - private static Collections.Generic.IEnumerable<String> EnumerateRecentLogFiles(String logRoot, Int32 days)
  - private static String EscapeField(String value)
  - private static String ModuleLogName(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private static String NormalizeModuleName(String value)
  - private static Drilling.Common.Interface.ST_INTERFACE_HISTORY ParseInterfaceLine(String line)
  - private static Drilling.Common.Managers.ST_RECIPE_HISTORY ParseRecipeLine(String line)
  - private static Drilling.Common.Managers.ST_SETTING_HISTORY ParseSettingLine(String line)
  - private static DateTimeOffset ParseTimestamp(String value)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_HISTORY> ReadInterfaceRecent(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null, String nickName = , Int32 maxRows = 100, Int32 days = 14)
  - private static Collections.Generic.IEnumerable<String> ReadLogFile(String path)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_HISTORY> ReadRecipeRecent(String recipeName, String recipeId, Int32 maxRows = 10, Int32 days = 14)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY> ReadSettingRecent(Drilling.Common.Managers.EN_SETTING_TAB section, Int32 maxRows = 20, Int32 days = 14)
  - private static String[] SplitFields(String line)
  - private static Boolean TryReadModuleLogName(String value, Drilling.Common.Interface.EN_EQP_MODULE& module)
  - private static Boolean TryReadSection(String value, Drilling.Common.Managers.EN_SETTING_TAB& section)
  - private static String UnescapeField(String value)
  - private Void WriteInterfaceAction(Drilling.Common.Interface.EN_EQP_MODULE module, String action, String nickName, String oldState, String newState)
  - public Void WriteInterfaceCommand(Drilling.Common.Interface.EN_EQP_MODULE module, String nickName, String command, String response, String detail = )
  - public Void WriteInterfaceConnection(Drilling.Common.Interface.EN_EQP_MODULE module, String action, String nickName, String oldState, String newState)
  - public Void WriteInterfaceError(Drilling.Common.Interface.EN_EQP_MODULE module, String nickName, String command, String detail)
  - public Void WriteProductEvent(String productId, String action, String state, String result, String detail)
  - private Void WriteRecipeAction(String recipeName, String action, String itemName, String oldValue, String newValue)
  - private Void WriteRecipeAction(String recipeName, String action, String tab, String group, String itemName, String oldValue, String newValue)
  - public Void WriteRecipeCreate(String recipeName)
  - public Void WriteRecipeDelete(String recipeName)
  - public Void WriteRecipeModify(String recipeName, String itemName, String oldValue, String newValue)
  - public Void WriteRecipeModify(String recipeName, String tab, String group, String itemName, String oldValue, String newValue)
  - public Void WriteRecipeRename(String oldRecipeName, String newRecipeName)
  - public Void WriteRecipeSave(String recipeName)
  - private Void WriteSettingAction(Drilling.Common.Managers.EN_SETTING_TAB section, String action, String parameterName, String oldValue, String newValue)
  - public Void WriteSettingModify(Drilling.Common.Managers.EN_SETTING_TAB section, String parameterName, String oldValue, String newValue)
  - public Void WriteSettingSave(Drilling.Common.Managers.EN_SETTING_TAB section)
  - public Void WriteStationState(String stationName, String stateName, String action, String detail)

### Drilling.Common.Log.CProgramOpenLog

- Kind: static class
- Properties:
  - private String LogDirectory (get)
  - public String LogPath (get)
- Methods:
  - private static String FindProjectRoot()
  - public static Void Write(String title, Exception exception)
  - public static Void Write(String title, String message)

### Drilling.Common.Log.ILogManager

- Kind: interface
- Methods:
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_HISTORY> ReadInterfaceRecent(Nullable<Drilling.Common.Interface.EN_EQP_MODULE> module = null, String nickName = , Int32 maxRows = 100, Int32 days = 14)
  - public Void WriteInterfaceCommand(Drilling.Common.Interface.EN_EQP_MODULE module, String nickName, String command, String response, String detail = )
  - public Void WriteInterfaceConnection(Drilling.Common.Interface.EN_EQP_MODULE module, String action, String nickName, String oldState, String newState)
  - public Void WriteInterfaceError(Drilling.Common.Interface.EN_EQP_MODULE module, String nickName, String command, String detail)
  - public Void WriteProductEvent(String productId, String action, String state, String result, String detail)
  - public Void WriteStationState(String stationName, String stateName, String action, String detail)

### Drilling.Common.Managers.CManager

- Kind: class
- Constructors:
  - public CManager(String configRoot, Drilling.Common.Managers.IRecipeFile recipeFile, Drilling.Common.Managers.ISettingFile settingFile, Drilling.Common.Managers.IManualScanFile manualScanFile, Drilling.Common.Managers.IInterfaceFile interfaceFile, Drilling.Common.Interface.IBETFile betFile, Drilling.Common.Interface.IPowerMeterFile powerMeterFile, Drilling.Common.Motion.IMotorFile motorFile, Drilling.Common.Motion.IIoFile ioFile, Drilling.Common.Product.IProductFile productFile, Drilling.Common.Log.ILogManager logManager, Drilling.Common.Station.IAutomationScriptFile automationScriptFile, Nullable<Boolean> simulationMode = null, Drilling.Common.Managers.IConfigStructureFile configStructureFile = null)
- Fields:
  - private Boolean _activeProductLoaded
  - private readonly Drilling.Common.Alarm.CAlarmManager _alarmManager
  - private readonly Drilling.Common.Station.IAutomationScriptFile _automationScriptFile
  - private readonly Drilling.Common.Interface.IBETFile _betFile
  - private readonly String _configRoot
  - private readonly Drilling.Common.Managers.IConfigStructureFile _configStructureFile
  - private readonly Drilling.Common.Managers.IInterfaceFile _interfaceFile
  - private readonly Drilling.Common.Interface.CInterfaceManager _interfaceManager
  - private readonly Drilling.Common.InterLock.CInterLockManager _interLockManager
  - private readonly Drilling.Common.Motion.IIoFile _ioFile
  - private Int32 _loadedInterfaceCount
  - private Int32 _loadedIoCount
  - private Int32 _loadedMotorCount
  - private readonly Drilling.Common.Log.ILogManager _logManager
  - private readonly Drilling.Common.Managers.IManualScanFile _manualScanFile
  - private readonly Drilling.Common.Motion.CMotionManager _motionManager
  - private readonly Drilling.Common.Motion.IMotorFile _motorFile
  - private readonly Drilling.Common.Interface.IPowerMeterFile _powerMeterFile
  - private readonly Drilling.Common.Product.IProductFile _productFile
  - private readonly Drilling.Common.Product.IProductManager _productManager
  - private readonly Drilling.Common.Managers.IRecipeFile _recipeFile
  - private readonly Drilling.Common.Managers.IRecipeManager _recipeManager
  - private readonly Drilling.Common.Managers.ISettingFile _settingFile
  - private readonly Drilling.Common.Managers.ISettingManager _settingManager
  - private readonly Object _startupLock
  - private readonly Collections.Generic.List<String> _startupMessages
  - private Int32 _startupStepNo
  - private readonly Collections.Generic.List<Drilling.Common.Managers.ST_MANAGER_STARTUP_STEP> _startupSteps
  - private readonly Drilling.Common.Station.CStationManager _stationManager
- Properties:
  - public String ConfigRoot (get)
  - public Collections.Generic.IReadOnlyList<String> StartupMessages (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_MANAGER_STARTUP_STEP> StartupSteps (get)
- Methods:
  - private Void AddStartupFailure(String stepName, Exception exception)
  - private Void AddStartupStep(String stepName, Drilling.Common.Managers.EN_MANAGER_STARTUP_RESULT result, String message)
  - public Drilling.Common.Alarm.CAlarmManager Alarm()
  - public Drilling.Common.Interface.IBETFile BETFile()
  - private Void CheckConfigRoot()
  - public Drilling.Common.Managers.ST_CONFIG_LOAD_STATUS ConfigStatus()
  - public Threading.Tasks.Task<Int32> ConnectInterface(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Destroy(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Int32> DisconnectInterface(Threading.CancellationToken cancellationToken = null)
  - private static String FormatConfigStatusMessage(Drilling.Common.Managers.ST_CONFIG_FILE_STATUS status)
  - private Boolean GetMotionSimulationMode(Nullable<Boolean> simulationMode)
  - private String GetScriptDirectory()
  - public Threading.Tasks.Task Initialize(Threading.CancellationToken cancellationToken = null)
  - public Drilling.Common.Interface.IInterfaceManager Interface()
  - public Drilling.Common.Managers.IInterfaceFile InterfaceFile()
  - public Drilling.Common.InterLock.CInterLockManager InterLock()
  - public Drilling.Common.Motion.IIoFile IoFile()
  - public Boolean IsNotSimul(Int32 systemId = 0)
  - public Boolean IsSimul(Int32 systemId = 0)
  - private static Boolean IsStartupDataException(Exception exception)
  - private static Boolean IsStartupRuntimeException(Exception exception)
  - private Void LoadActiveProduct()
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> LoadInterfaceList()
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_DATA> LoadIoList()
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> LoadMotorList()
  - public Drilling.Common.Log.ILogManager Log()
  - public Drilling.Common.Managers.IManualScanFile ManualScanFile()
  - public Drilling.Common.Motion.IMotionManager Motion()
  - public Drilling.Common.Motion.IMotorFile MotorFile()
  - public Drilling.Common.Interface.IPowerMeterFile PowerMeterFile()
  - public Drilling.Common.Product.IProductManager Product()
  - public Drilling.Common.Product.IProductFile ProductFile()
  - public Drilling.Common.Managers.IRecipeManager Recipe()
  - public Drilling.Common.Managers.IRecipeFile RecipeFile()
  - public Threading.Tasks.Task ReconnectInterface(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - private Void RegisterInterfaceList(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaceData)
  - private Threading.Tasks.Task RunInitializeStep(String stepName, Func<Threading.Tasks.Task> action, Threading.CancellationToken cancellationToken)
  - public Void SetSimul(Boolean enabled)
  - public Void SetSimul(Int32 systemId, Boolean enabled)
  - public Drilling.Common.Managers.ISettingManager Setting()
  - public Drilling.Common.Managers.ISettingFile SettingFile()
  - public Drilling.Common.Station.IStationManager Station()
  - private static Drilling.Common.Managers.EN_MANAGER_STARTUP_RESULT ToStartupResult(Drilling.Common.Managers.ST_CONFIG_FILE_STATUS status)
  - private Void ValidateConfigStructure()

### Drilling.Common.Managers.CRecipeManager

- Kind: class
- Interfaces: Drilling.Common.Managers.IRecipeManager
- Constructors:
  - public CRecipeManager(Drilling.Common.Managers.IRecipeFile recipeFile)
- Methods:
  - public Threading.Tasks.Task DeleteRecipe(String recipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA>> LoadRecipes(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task RenameRecipe(String oldRecipeId, String newRecipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveRecipe(Drilling.Common.Managers.ST_RECIPE_DATA recipe, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.CSettingManager

- Kind: class
- Interfaces: Drilling.Common.Managers.ISettingManager
- Constructors:
  - public CSettingManager(Drilling.Common.Managers.ISettingFile settingFile, Drilling.Common.Managers.IInterfaceFile interfaceFile, Drilling.Common.Interface.IInterfaceManager interfaceManager)
- Methods:
  - public Threading.Tasks.Task ConnectInterface(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task DisconnectInterface(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY>> LoadHistory(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA>> LoadInterfaceList(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER>> LoadSection(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ReconnectInterface(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveInterfaceList(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveSection(Drilling.Common.Managers.EN_SETTING_TAB section, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER> parameters, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.EN_MANAGER_STARTUP_RESULT

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ready = 0
  - Warning = 1
  - Failed = 2

### Drilling.Common.Managers.EN_PM_LOCK_STATE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Released = 0
  - Locked = 1

### Drilling.Common.Managers.EN_RECIPE_DATA_TYPE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - String = 0
  - Int = 1
  - Double = 2
  - Bool = 3

### Drilling.Common.Managers.EN_SETTING_TAB

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Option = 0
  - Interface = 1
  - Io = 2
  - Motor = 3
  - Position = 4
  - Alarm = 5

### Drilling.Common.Managers.EN_SYSTEM_MODE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Simulation = 0
  - Auto = 1
  - Manual = 2

### Drilling.Common.Managers.IConfigStructureFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_CONFIG_FILE_STATUS>> Validate(Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.IInterfaceFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveAll(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.IManualScanFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task Delete(String settingName, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<String>> List(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM> Load(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM> Load(String settingName, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Rename(String oldSettingName, String newSettingName, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM settings, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(String settingName, Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM settings, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.IRecipeFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task Delete(String recipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_RECIPE_DATA> Find(String recipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Rename(String oldRecipeId, String newRecipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(Drilling.Common.Managers.ST_RECIPE_DATA recipe, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.IRecipeManager

- Kind: interface
- Methods:
  - public Threading.Tasks.Task DeleteRecipe(String recipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA>> LoadRecipes(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task RenameRecipe(String oldRecipeId, String newRecipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveRecipe(Drilling.Common.Managers.ST_RECIPE_DATA recipe, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.ISettingFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER>> Load(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY>> LoadHistory(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(Drilling.Common.Managers.EN_SETTING_TAB section, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER> parameters, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.ISettingManager

- Kind: interface
- Methods:
  - public Threading.Tasks.Task ConnectInterface(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task DisconnectInterface(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY>> LoadHistory(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA>> LoadInterfaceList(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER>> LoadSection(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ReconnectInterface(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 number, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveInterfaceList(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveSection(Drilling.Common.Managers.EN_SETTING_TAB section, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER> parameters, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Managers.ST_CONFIG_FILE_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_CONFIG_FILE_STATUS>
- Constructors:
  - public ST_CONFIG_FILE_STATUS(String ItemName, String Path, Boolean Required, Boolean Exists, Boolean IsValid, String Message)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean Exists (get/set)
  - public Boolean IsValid (get/set)
  - public String ItemName (get/set)
  - public String Message (get/set)
  - public String Path (get/set)
  - public Boolean Required (get/set)

### Drilling.Common.Managers.ST_CONFIG_LOAD_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_CONFIG_LOAD_STATUS>
- Constructors:
  - public ST_CONFIG_LOAD_STATUS(String ConfigRoot, Int32 InterfaceCount, Int32 MotorCount, Int32 IoCount, Boolean ActiveProductLoaded, Collections.Generic.IReadOnlyList<String> StartupMessages, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_MANAGER_STARTUP_STEP> StartupSteps)
- Properties:
  - public Boolean ActiveProductLoaded (get/set)
  - public String ConfigRoot (get/set)
  - private Type EqualityContract (get)
  - public Int32 InterfaceCount (get/set)
  - public Int32 IoCount (get/set)
  - public Int32 MotorCount (get/set)
  - public Collections.Generic.IReadOnlyList<String> StartupMessages (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_MANAGER_STARTUP_STEP> StartupSteps (get/set)

### Drilling.Common.Managers.ST_DEVICE_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_DEVICE_STATUS>
- Constructors:
  - public ST_DEVICE_STATUS(Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_STATUS> Io, Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS> Motors, Drilling.Common.Interface.ST_LASER_STATUS Laser, Drilling.Common.Interface.ST_CHILLER_STATUS Chiller, Drilling.Common.Interface.ST_ATTENUATOR_STATUS Attenuator, Drilling.Common.Interface.ST_BET_STATUS Bet, Drilling.Common.Managers.ST_POWER_METER_STATUS PowerMeter)
- Properties:
  - public Drilling.Common.Interface.ST_ATTENUATOR_STATUS Attenuator (get/set)
  - public Drilling.Common.Interface.ST_BET_STATUS Bet (get/set)
  - public Drilling.Common.Interface.ST_CHILLER_STATUS Chiller (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_STATUS> Io (get/set)
  - public Drilling.Common.Interface.ST_LASER_STATUS Laser (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS> Motors (get/set)
  - public Drilling.Common.Managers.ST_POWER_METER_STATUS PowerMeter (get/set)

### Drilling.Common.Managers.ST_MANAGER_STARTUP_STEP

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_MANAGER_STARTUP_STEP>
- Constructors:
  - public ST_MANAGER_STARTUP_STEP(Int32 Order, String StepName, Drilling.Common.Managers.EN_MANAGER_STARTUP_RESULT Result, String Message)
- Properties:
  - private Type EqualityContract (get)
  - public String Message (get/set)
  - public Int32 Order (get/set)
  - public Drilling.Common.Managers.EN_MANAGER_STARTUP_RESULT Result (get/set)
  - public String StepName (get/set)

### Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM>
- Constructors:
  - public ST_MANUAL_SCAN_PARAM(Double ShapeSize, Double OffsetX, Double OffsetY, String Direction, String ShapeName)
- Properties:
  - public String Direction (get/set)
  - private Type EqualityContract (get)
  - public Double OffsetX (get/set)
  - public Double OffsetY (get/set)
  - public String ShapeName (get/set)
  - public Double ShapeSize (get/set)

### Drilling.Common.Managers.ST_PM_LOCK_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_PM_LOCK_STATUS>
- Constructors:
  - public ST_PM_LOCK_STATUS(Boolean IsLocked, Nullable<DateTimeOffset> LockedAt)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean IsLocked (get/set)
  - public Nullable<DateTimeOffset> LockedAt (get/set)

### Drilling.Common.Managers.ST_POWER_METER_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_POWER_METER_STATUS>
- Constructors:
  - public ST_POWER_METER_STATUS(Double MeasuredPower, String Unit, DateTimeOffset MeasuredAt, Double AveragePower = 0, Double MinPower = 0, Double MaxPower = 0, Double WaveLengthNm = 355, Double BeamPositionX = 0, Double BeamPositionY = 0, Int32 SampleCount = 0, Boolean IsMeasuring = False, String ModelName = PowerMax, String SerialNumber = -, String LastCommand = , Drilling.Common.Interface.EN_POWER_METER_ERROR LastError = Ok)
- Properties:
  - public Double AveragePower (get/set)
  - public Double BeamPositionX (get/set)
  - public Double BeamPositionY (get/set)
  - public Drilling.Common.Managers.ST_POWER_METER_STATUS Empty (get)
  - private Type EqualityContract (get)
  - public Boolean IsMeasuring (get/set)
  - public String LastCommand (get/set)
  - public Drilling.Common.Interface.EN_POWER_METER_ERROR LastError (get/set)
  - public Double MaxPower (get/set)
  - public DateTimeOffset MeasuredAt (get/set)
  - public Double MeasuredPower (get/set)
  - public Double MinPower (get/set)
  - public String ModelName (get/set)
  - public Int32 SampleCount (get/set)
  - public String SerialNumber (get/set)
  - public String Unit (get/set)
  - public Double WaveLengthNm (get/set)

### Drilling.Common.Managers.ST_RECIPE_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_RECIPE_DATA>
- Constructors:
  - public ST_RECIPE_DATA(String Id, String Name, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_PARAM> Parameters, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_HISTORY> History)
- Properties:
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_HISTORY> History (get/set)
  - public String Id (get/set)
  - public String Name (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_PARAM> Parameters (get/set)

### Drilling.Common.Managers.ST_RECIPE_FORM_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_RECIPE_FORM_ITEM>
- Constructors:
  - public ST_RECIPE_FORM_ITEM(String Tab, String Group, String Name, String DisplayName, String CimName, Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType, String Unit, Boolean Show, Boolean Use, String DefaultValue, Double Scale, Double ChangeLimit, Double Min, Double Max, String Description, Int32 DisplayOrder, Collections.Generic.IReadOnlyDictionary<String, String> Extra = null)
- Properties:
  - public Double ChangeLimit (get/set)
  - public String CimName (get/set)
  - public Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType (get/set)
  - public String DefaultValue (get/set)
  - public String Description (get/set)
  - public String DisplayName (get/set)
  - public Int32 DisplayOrder (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Extra (get/set)
  - public String Group (get/set)
  - public Double Max (get/set)
  - public Double Min (get/set)
  - public String Name (get/set)
  - public Double Scale (get/set)
  - public Boolean Show (get/set)
  - public String Tab (get/set)
  - public String Unit (get/set)
  - public Boolean Use (get/set)

### Drilling.Common.Managers.ST_RECIPE_HISTORY

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_RECIPE_HISTORY>
- Constructors:
  - public ST_RECIPE_HISTORY(DateTimeOffset ChangedAt, String ItemName, String OldValue, String NewValue, String OperatorId, String RecipeName = , String Action = , String Tab = , String Group = )
- Properties:
  - public String Action (get/set)
  - public DateTimeOffset ChangedAt (get/set)
  - private Type EqualityContract (get)
  - public String Group (get/set)
  - public String ItemName (get/set)
  - public String NewValue (get/set)
  - public String OldValue (get/set)
  - public String OperatorId (get/set)
  - public String RecipeName (get/set)
  - public String Tab (get/set)

### Drilling.Common.Managers.ST_RECIPE_PARAM

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_RECIPE_PARAM>
- Constructors:
  - public ST_RECIPE_PARAM(String Name, String Value, String Unit, String Range, String DefaultValue, String Tab = , String Group = , String Key = , String Description = , Boolean Show = True, Boolean Use = True, Int32 DisplayOrder = 0, Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType = String, Double ChangeLimit = 0, Double Min = 0, Double Max = 0, Collections.Generic.IReadOnlyDictionary<String, String> Extra = null)
- Properties:
  - public Double ChangeLimit (get/set)
  - public Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType (get/set)
  - public String DefaultValue (get/set)
  - public String Description (get/set)
  - public Int32 DisplayOrder (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Extra (get/set)
  - public String Group (get/set)
  - public String Key (get/set)
  - public Double Max (get/set)
  - public Double Min (get/set)
  - public String Name (get/set)
  - public String Range (get/set)
  - public Boolean Show (get/set)
  - public String Tab (get/set)
  - public String Unit (get/set)
  - public Boolean Use (get/set)
  - public String Value (get/set)

### Drilling.Common.Managers.ST_RECIPE_VALUE

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_RECIPE_VALUE>
- Constructors:
  - public ST_RECIPE_VALUE(String Tab, String Name, String Value, Collections.Generic.IReadOnlyList<String> Extra = null)
- Properties:
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<String> Extra (get/set)
  - public String Name (get/set)
  - public String Tab (get/set)
  - public String Value (get/set)

### Drilling.Common.Managers.ST_SETTING_HISTORY

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_SETTING_HISTORY>
- Constructors:
  - public ST_SETTING_HISTORY(DateTimeOffset ChangedAt, Drilling.Common.Managers.EN_SETTING_TAB Section, String ParameterName, String OldValue, String NewValue, String OperatorId, String Action)
- Properties:
  - public String Action (get/set)
  - public DateTimeOffset ChangedAt (get/set)
  - private Type EqualityContract (get)
  - public String NewValue (get/set)
  - public String OldValue (get/set)
  - public String OperatorId (get/set)
  - public String ParameterName (get/set)
  - public Drilling.Common.Managers.EN_SETTING_TAB Section (get/set)

### Drilling.Common.Managers.ST_SYSTEM_PARAMETER

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_SYSTEM_PARAMETER>
- Constructors:
  - public ST_SYSTEM_PARAMETER(Drilling.Common.Managers.EN_SETTING_TAB Section, String Name, String Value, String Unit, String Description, String Group = , String Key = , String DefaultValue = , Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType = String, Double Min = 0, Double Max = 0, Boolean Show = True, Boolean Use = True, Int32 DisplayOrder = 0, Collections.Generic.IReadOnlyDictionary<String, String> Extra = null)
- Properties:
  - public Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType (get/set)
  - public String DefaultValue (get/set)
  - public String Description (get/set)
  - public Int32 DisplayOrder (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Extra (get/set)
  - public String Group (get/set)
  - public String Key (get/set)
  - public Double Max (get/set)
  - public Double Min (get/set)
  - public String Name (get/set)
  - public Drilling.Common.Managers.EN_SETTING_TAB Section (get/set)
  - public Boolean Show (get/set)
  - public String Unit (get/set)
  - public Boolean Use (get/set)
  - public String Value (get/set)

### Drilling.Common.Managers.ST_SYSTEM_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Managers.ST_SYSTEM_STATUS>
- Constructors:
  - public ST_SYSTEM_STATUS(String CurrentRecipeId, Drilling.Common.Managers.EN_SYSTEM_MODE OperationMode, Drilling.Common.Alarm.EN_ALARM_STATE AlarmState, Drilling.Common.Managers.EN_PM_LOCK_STATE PMLockState, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS> Modules)
- Properties:
  - public Drilling.Common.Alarm.EN_ALARM_STATE AlarmState (get/set)
  - public String CurrentRecipeId (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS> Modules (get/set)
  - public Drilling.Common.Managers.EN_SYSTEM_MODE OperationMode (get/set)
  - public Drilling.Common.Managers.EN_PM_LOCK_STATE PMLockState (get/set)
- Methods:
  - public Drilling.Common.Interface.ST_DEVICE_COMM_STATUS GetModule(Drilling.Common.Interface.EN_EQP_MODULE module)

### Drilling.Common.Motion.CA3200Motion

- Kind: class
- Base: Drilling.Common.Motion.CMotionController
- Constructors:
  - public CA3200Motion(Drilling.Common.Interface.IInterfaceManager interfaceManager, Int32 deviceNo = 0)
- Properties:
  - protected String CommandPrefix (get)

### Drilling.Common.Motion.CACSComm

- Kind: class
- Base: Drilling.Common.Interface.CCommBase
- Interfaces: Drilling.Common.Interface.IComm
- Constructors:
  - public CACSComm(Drilling.Common.Interface.ST_INTERFACE_DATA data, Drilling.Common.Interface.ST_INTERFACE_CONNECT_OPTION option)
- Fields:
  - private ACS.SPiiPlusNET.Api _api
  - private readonly Threading.SemaphoreSlim _commLock
- Methods:
  - private Void CloseApi()
  - public Threading.Tasks.Task Connect(Threading.CancellationToken cancellationToken = null)
  - private Void ConnectLocked()
  - public Threading.Tasks.Task Disconnect(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<String> Execute(String function, Threading.CancellationToken cancellationToken = null)
  - private String ExecuteACSFunction(ACS.SPiiPlusNET.Api api, String function)
  - private static String ExecuteAxisFunction(ACS.SPiiPlusNET.Api api, Collections.Generic.IReadOnlyList<String> tokens)
  - private static String ExecuteIoFunction(ACS.SPiiPlusNET.Api api, Collections.Generic.IReadOnlyList<String> tokens)
  - private static String ExecuteRawACSCommand(ACS.SPiiPlusNET.Api api, String function)
  - private Boolean IsSimulatorEndpoint()
  - private static ValueTuple<Int32, Int32> ParseIoAddress(String address)
  - private static Double ReadDouble(Collections.Generic.IReadOnlyList<String> tokens, Int32 index, String command)
  - private static Int32 ReadInt(String value, String fieldName)
  - private static ACS.SPiiPlusNET.Axis ToAcsAxis(Int32 axisNo)

### Drilling.Common.Motion.CACSMotion

- Kind: class
- Base: Drilling.Common.Motion.CMotionController
- Constructors:
  - public CACSMotion(Drilling.Common.Interface.IInterfaceManager interfaceManager, Int32 deviceNo = 0)
- Properties:
  - protected String CommandPrefix (get)

### Drilling.Common.Motion.CAjinMotion

- Kind: class
- Base: Drilling.Common.Motion.CMotionController
- Constructors:
  - public CAjinMotion(Drilling.Common.Interface.IInterfaceManager interfaceManager, Int32 deviceNo = 0)
- Properties:
  - protected String CommandPrefix (get)

### Drilling.Common.Motion.CAutomation1Motion

- Kind: class
- Base: Drilling.Common.Motion.CMotionController
- Constructors:
  - public CAutomation1Motion(Drilling.Common.Interface.IInterfaceManager interfaceManager, Int32 deviceNo = 0)
- Properties:
  - protected String CommandPrefix (get)
  - protected Drilling.Common.Interface.EN_EQP_MODULE PrimaryModule (get)

### Drilling.Common.Motion.CMotionController

- Kind: class
- Constructors:
  - protected CMotionController(String controller, Drilling.Common.Interface.IInterfaceManager interfaceManager, Int32 deviceNo = 0)
- Fields:
  - private readonly Drilling.Common.Interface.IInterfaceManager _interfaceManager
- Properties:
  - protected String CommandPrefix (get)
  - public String Controller (get)
  - public Int32 DeviceNo (get)
  - protected Drilling.Common.Interface.EN_EQP_MODULE PrimaryModule (get)
- Methods:
  - protected String BuildAxisCommand(Drilling.Common.Motion.ST_MOTOR_DATA axis, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter)
  - public Threading.Tasks.Task Destroy(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ExecuteAxisCommand(Drilling.Common.Motion.ST_MOTOR_DATA axis, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter, Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Interface.ST_INTERFACE_DATA GetInterfaceData()
  - public Threading.Tasks.Task Initialize(Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> axes, Threading.CancellationToken cancellationToken = null)
  - public Boolean IsSimulation()
  - public Threading.Tasks.Task<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS> ReadAxisStatus(Drilling.Common.Motion.ST_MOTOR_DATA axis, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Nullable<Boolean>> ReadIo(String address, Boolean isOutput, Threading.CancellationToken cancellationToken = null)
  - protected Threading.Tasks.Task<String> Send(String command, Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task SetOutput(String address, Boolean isOn, Threading.CancellationToken cancellationToken = null)
  - private static Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS TryParseAxisStatus(Drilling.Common.Motion.ST_MOTOR_DATA axis, String response)

### Drilling.Common.Motion.CMotionControllerTypeAttribute

- Kind: class
- Base: System.Attribute
- Constructors:
  - public CMotionControllerTypeAttribute(String[] controllerNames)
- Properties:
  - public Collections.Generic.IReadOnlyList<String> ControllerNames (get)
- Methods:
  - private static String NormalizeControllerName(String value)

### Drilling.Common.Motion.CMotionManager

- Kind: class
- Interfaces: Drilling.Common.Motion.IMotionManager
- Constructors:
  - public CMotionManager(Boolean isSimulation = True)
  - public CMotionManager(Drilling.Common.Interface.IInterfaceManager interfaceManager, Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> motors = null, Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_DATA> ioData = null, Boolean isSimulation = True)
- Fields:
  - private static String DefaultControllerName
  - private static readonly Collections.Generic.IReadOnlyDictionary<String, Type> MotionControllerTypes
  - private readonly Collections.Generic.Dictionary<String, Drilling.Common.Motion.CMotionManager+ST_AXIS_STATE> _axes
  - private readonly Collections.Generic.Dictionary<String, Drilling.Common.Motion.ST_MOTOR_DATA> _axisData
  - private readonly Collections.Generic.Dictionary<String, Drilling.Common.Motion.CMotionController> _controllers
  - private readonly Drilling.Common.Interface.IInterfaceManager _interfaceManager
  - private readonly Collections.Generic.Dictionary<String, Drilling.Common.Motion.CMotionManager+ST_IO_STATE> _io
  - private readonly Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> _motors
  - private Boolean _simulationMode
- Properties:
  - public Boolean IsSimulation (get)
- Methods:
  - private Void ApplyAxisCommand(String axisId, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter)
  - private Void ApplyAxisStatus(Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS status)
  - private static Collections.Generic.Dictionary<String, Drilling.Common.Motion.CMotionManager+ST_AXIS_STATE> CreateAxes(Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> motors)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> CreateDefaultMotorData()
  - private static Collections.Generic.Dictionary<String, Drilling.Common.Motion.CMotionManager+ST_IO_STATE> CreateIo(Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_DATA> ioData)
  - private Drilling.Common.Motion.CMotionController CreateMotionController(String controller, Int32 deviceNo)
  - private static InvalidOperationException CreateMotionControllerNotRegisteredException(String controller, Int32 deviceNo)
  - public Threading.Tasks.Task Destroy(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ExecuteAxisCommand(String axisId, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteMotionCommand(String axisId, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - private static String FormatIoReference(Drilling.Common.Motion.CMotionManager+ST_IO_STATE channel)
  - private static String FormatMotionCommand(Drilling.Common.Motion.EN_MOTION_COMMAND command)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS>> GetAxisStatus(Threading.CancellationToken cancellationToken = null)
  - private static String GetControllerKey(String controller, Int32 deviceNo)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTION_CONTROLLER_STATUS>> GetControllerStatus(Threading.CancellationToken cancellationToken = null)
  - private static Double GetInitialPosition(String axisName)
  - private Drilling.Common.Motion.CMotionManager+ST_IO_STATE GetIoChannelOrThrow(String ioName)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_STATUS>> GetIoStatus(Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Motion.CMotionController GetMotionController(String devType, Int32 devNo)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTION_STATION_STATUS>> GetStationStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Home(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Initialize(Threading.CancellationToken cancellationToken = null)
  - private static Boolean IsOnText(String value)
  - private static Collections.Generic.IReadOnlyDictionary<String, Type> LoadMotionControllerTypes()
  - private Void MarkAxisAlarm(String axisId)
  - private static Drilling.Common.Motion.ST_MOTOR_DATA Motor(String name, Int32 axis, String displayName, Double initialPosition, String unit)
  - public Threading.Tasks.Task Move(String axisId, Double targetPosition, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task MoveAxis(String axisId, Double targetPosition, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task MoveRel(String axisId, Double distance, Threading.CancellationToken cancellationToken = null)
  - private static String NormalizeAddress(String address)
  - private static String NormalizeAxisId(String axisId)
  - internal static String NormalizeControllerName(String value)
  - private static String NormalizeIoName(String value)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> NormalizeMotors(Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> motors)
  - private static String NormalizeStationName(String stationName)
  - private Threading.Tasks.Task RefreshAxisStatus(Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task RefreshIoStatus(Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task RefreshStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ResetAlarm(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ServoOff(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ServoOn(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SetOutput(String ioName, Boolean isOn, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> SetOutputCommand(String ioName, Boolean isOn, Threading.CancellationToken cancellationToken = null)
  - public Void SetSimulationMode(Boolean enabled)
  - private static Collections.Generic.IEnumerable<Drilling.Common.Motion.CMotionManager+ST_PRE_CHECK_IO> SplitPreCheckIo(String preCheckIo)
  - public Threading.Tasks.Task Stop(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task StopMotion(String axisId, Threading.CancellationToken cancellationToken = null)
  - private static Void UpdateAxisPosition(Drilling.Common.Motion.CMotionManager+ST_AXIS_STATE axis, Double targetPosition)
  - private Void ValidateAxisCommand(Drilling.Common.Motion.ST_MOTOR_DATA axisData, Drilling.Common.Motion.CMotionManager+ST_AXIS_STATE axisState, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter)
  - private Void ValidatePreCheckIo(Drilling.Common.Motion.ST_MOTOR_DATA axisData)

### Drilling.Common.Motion.CPmacMotion

- Kind: class
- Base: Drilling.Common.Motion.CMotionController
- Constructors:
  - public CPmacMotion(Drilling.Common.Interface.IInterfaceManager interfaceManager, Int32 deviceNo = 0)
- Properties:
  - protected String CommandPrefix (get)

### Drilling.Common.Motion.CUmacMotion

- Kind: class
- Base: Drilling.Common.Motion.CMotionController
- Constructors:
  - public CUmacMotion(Drilling.Common.Interface.IInterfaceManager interfaceManager, Int32 deviceNo = 0)
- Properties:
  - protected String CommandPrefix (get)

### Drilling.Common.Motion.EN_MOTION_COMMAND

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - ServoOn = 0
  - ServoOff = 1
  - Home = 2
  - MoveAbs = 3
  - MoveRel = 4
  - Stop = 5
  - ResetAlarm = 6
  - Refresh = 7

### Drilling.Common.Motion.IIoFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Motion.IMotionManager

- Kind: interface
- Properties:
  - public Boolean IsSimulation (get)
- Methods:
  - public Threading.Tasks.Task Destroy(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ExecuteAxisCommand(String axisId, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteMotionCommand(String axisId, Drilling.Common.Motion.EN_MOTION_COMMAND command, Double parameter = 0, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS>> GetAxisStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTION_CONTROLLER_STATUS>> GetControllerStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_STATUS>> GetIoStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTION_STATION_STATUS>> GetStationStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Home(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Initialize(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Move(String axisId, Double targetPosition, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task MoveAxis(String axisId, Double targetPosition, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task MoveRel(String axisId, Double distance, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task RefreshStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ResetAlarm(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ServoOff(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ServoOn(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SetOutput(String ioName, Boolean isOn, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> SetOutputCommand(String ioName, Boolean isOn, Threading.CancellationToken cancellationToken = null)
  - public Void SetSimulationMode(Boolean enabled)
  - public Threading.Tasks.Task Stop(String axisId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task StopMotion(String axisId, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Motion.IMotorFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Motion.CMotionManager+ST_AXIS_STATE

- Kind: class
- Constructors:
  - public ST_AXIS_STATE(String axisId, String name, Double currentPosition, Double targetPosition, Double commandPosition, Boolean servoOn, Boolean homeCompleted, Boolean limitPlusOn, Boolean limitMinusOn, Boolean alarmOn, Int32 displayOrder)
- Properties:
  - public Boolean AlarmOn (get/set)
  - public String AxisId (get)
  - public Double CommandPosition (get/set)
  - public Double CurrentPosition (get/set)
  - public Int32 DisplayOrder (get)
  - public Boolean HomeCompleted (get/set)
  - public Boolean LimitMinusOn (get/set)
  - public Boolean LimitPlusOn (get/set)
  - public String Name (get)
  - public Boolean ServoOn (get/set)
  - public Double TargetPosition (get/set)

### Drilling.Common.Motion.ST_IO_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Motion.ST_IO_DATA>
- Constructors:
  - public ST_IO_DATA(String Id, Boolean Use, String Address, String Name, Boolean IsOutput, String DevType, Int32 DevNo, Boolean InitialState, Int32 DisplayOrder, String Description)
- Properties:
  - public String Address (get/set)
  - public String Description (get/set)
  - public Int32 DevNo (get/set)
  - public String DevType (get/set)
  - public Int32 DisplayOrder (get/set)
  - private Type EqualityContract (get)
  - public String Id (get/set)
  - public Boolean InitialState (get/set)
  - public Boolean IsOutput (get/set)
  - public String Name (get/set)
  - public Boolean Use (get/set)

### Drilling.Common.Motion.CMotionManager+ST_IO_STATE

- Kind: class
- Constructors:
  - public ST_IO_STATE(String id, String address, String name, Boolean isOn, Boolean isOutput, String devType, Int32 devNo, Int32 displayOrder, String description)
- Properties:
  - public String Address (get)
  - public String Description (get)
  - public Int32 DevNo (get)
  - public String DevType (get)
  - public Int32 DisplayOrder (get)
  - public String Id (get)
  - public Boolean IsOn (get/set)
  - public Boolean IsOutput (get)
  - public String Name (get)

### Drilling.Common.Motion.ST_IO_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Motion.ST_IO_STATUS>
- Constructors:
  - public ST_IO_STATUS(String Id, String Address, String Name, Boolean IsOn, Boolean IsOutput)
- Properties:
  - public String Address (get/set)
  - private Type EqualityContract (get)
  - public String Id (get/set)
  - public Boolean IsOn (get/set)
  - public Boolean IsOutput (get/set)
  - public String Name (get/set)

### Drilling.Common.Motion.ST_MOTION_CONTROLLER_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Motion.ST_MOTION_CONTROLLER_STATUS>
- Constructors:
  - public ST_MOTION_CONTROLLER_STATUS(String DevType, Int32 DevNo, Boolean IsRegistered, Boolean IsSimulation, Int32 AxisCount, Collections.Generic.IReadOnlyList<String> AxisIds)
- Properties:
  - public Int32 AxisCount (get/set)
  - public Collections.Generic.IReadOnlyList<String> AxisIds (get/set)
  - public Int32 DevNo (get/set)
  - public String DevType (get/set)
  - private Type EqualityContract (get)
  - public Boolean IsRegistered (get/set)
  - public Boolean IsSimulation (get/set)

### Drilling.Common.Motion.ST_MOTION_STATION_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Motion.ST_MOTION_STATION_STATUS>
- Constructors:
  - public ST_MOTION_STATION_STATUS(String StationName, String SystemName, Boolean HasAlarm, Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS> Axes)
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS> Axes (get/set)
  - private Type EqualityContract (get)
  - public Boolean HasAlarm (get/set)
  - public String StationName (get/set)
  - public String SystemName (get/set)

### Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Motion.ST_MOTOR_AXIS_STATUS>
- Constructors:
  - public ST_MOTOR_AXIS_STATUS(String AxisId, String Name, Double CurrentPosition, Double TargetPosition, Double CommandPosition, Boolean ServoOn, Boolean HomeCompleted, Boolean LimitPlusOn, Boolean LimitMinusOn, Boolean AlarmOn)
- Properties:
  - public Boolean AlarmOn (get/set)
  - public String AxisId (get/set)
  - public Double CommandPosition (get/set)
  - public Double CurrentPosition (get/set)
  - private Type EqualityContract (get)
  - public Boolean HomeCompleted (get/set)
  - public Boolean LimitMinusOn (get/set)
  - public Boolean LimitPlusOn (get/set)
  - public String Name (get/set)
  - public Boolean ServoOn (get/set)
  - public Double TargetPosition (get/set)

### Drilling.Common.Motion.ST_MOTOR_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Motion.ST_MOTOR_DATA>
- Constructors:
  - public ST_MOTOR_DATA(String Name, Boolean Use, Int32 Axis, Int32 VirtureAxis, String DevType, Int32 DevNo, Int32 CoordinateNo, Int32 MotorType, Double Scale, String System, String StationName, String Subordinate, String DisplayName, String AxisDir, Boolean AlignReverse, Boolean ProcessReverse, String Dir, String ProductIndex, String AxisColor, Boolean ReverseDir, Double CorrectionAngle, Double OffsetX, Double OffsetY, Double OffsetZ, Double OffsetXT, Double OffsetYT, Double OffsetZT, String Unit, Double MaxVel, Double InterlockMaxVel, Double MaxAcc, Double Min, Double Max, Int32 HomePlc, Int32 HomeTimeout, String HomePlcFlag, String Description, Double LoadAlarmValue, String PreCheckIo)
- Properties:
  - public Boolean AlignReverse (get/set)
  - public Int32 Axis (get/set)
  - public String AxisColor (get/set)
  - public String AxisDir (get/set)
  - public Int32 CoordinateNo (get/set)
  - public Double CorrectionAngle (get/set)
  - public String Description (get/set)
  - public Int32 DevNo (get/set)
  - public String DevType (get/set)
  - public String Dir (get/set)
  - public String DisplayName (get/set)
  - private Type EqualityContract (get)
  - public Int32 HomePlc (get/set)
  - public String HomePlcFlag (get/set)
  - public Int32 HomeTimeout (get/set)
  - public Double InterlockMaxVel (get/set)
  - public Double LoadAlarmValue (get/set)
  - public Double Max (get/set)
  - public Double MaxAcc (get/set)
  - public Double MaxVel (get/set)
  - public Double Min (get/set)
  - public Int32 MotorType (get/set)
  - public String Name (get/set)
  - public Double OffsetX (get/set)
  - public Double OffsetXT (get/set)
  - public Double OffsetY (get/set)
  - public Double OffsetYT (get/set)
  - public Double OffsetZ (get/set)
  - public Double OffsetZT (get/set)
  - public String PreCheckIo (get/set)
  - public Boolean ProcessReverse (get/set)
  - public String ProductIndex (get/set)
  - public Boolean ReverseDir (get/set)
  - public Double Scale (get/set)
  - public String StationName (get/set)
  - public String Subordinate (get/set)
  - public String System (get/set)
  - public String Unit (get/set)
  - public Boolean Use (get/set)
  - public Int32 VirtureAxis (get/set)

### Drilling.Common.Motion.CMotionManager+ST_PRE_CHECK_IO

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Motion.CMotionManager+ST_PRE_CHECK_IO>
- Constructors:
  - public ST_PRE_CHECK_IO(String IoName, Boolean ExpectedOn)
  - private ST_PRE_CHECK_IO(Drilling.Common.Motion.CMotionManager+ST_PRE_CHECK_IO original)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean ExpectedOn (get/set)
  - public String IoName (get/set)

### Drilling.Common.Product.CProductManager

- Kind: class
- Interfaces: Drilling.Common.Product.IProductManager
- Constructors:
  - public CProductManager(Drilling.Common.Product.IProductFile productFile, Drilling.Common.Log.ILogManager logManager = null)
- Fields:
  - private Drilling.Common.Product.ST_PRODUCT_DATA _current
- Properties:
  - public Drilling.Common.Product.ST_PRODUCT_DATA Current (get)
- Methods:
  - private static String ChooseProductId(String processId, String productId, String panelId, Collections.Generic.IReadOnlyDictionary<String, String> parameters)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> CompleteProduct(String productId, Boolean isOk, String message, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> CreateProduct(String processId, String productId, String panelId, String lotId, String recipeId, Collections.Generic.IReadOnlyDictionary<String, String> parameters, Collections.Generic.IReadOnlyDictionary<Int32, Int32> headPointCounts, Threading.CancellationToken cancellationToken = null)
  - private Drilling.Common.Product.ST_PRODUCT_DATA GetCurrent(String productId)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> LoadActive(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HISTORY>> LoadHistory(Int32 maxRows = 100, Int32 days = 14, Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task SaveAndLog(String action, String detail, Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> ScrapProduct(String productId, String reason, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> SetError(String productId, String message, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> SetHeadResult(String productId, Int32 headNo, Boolean isOk, String errorCode = , String message = , Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> SetHeadRunning(String productId, Int32 headNo, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> StartProduct(String productId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> StopProduct(String productId, String message, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Product.EN_PRODUCT_HEAD_STATE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ready = 0
  - Running = 1
  - Completed = 2
  - Error = 3
  - Disabled = 4

### Drilling.Common.Product.EN_PRODUCT_RESULT

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Pending = 0
  - OK = 1
  - NG = 2

### Drilling.Common.Product.EN_PRODUCT_STATE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Created = 0
  - Running = 1
  - Completed = 2
  - Stopped = 3
  - Error = 4
  - Scrapped = 5

### Drilling.Common.Product.IProductFile

- Kind: interface
- Methods:
  - public Threading.Tasks.Task AppendHeadResults(Drilling.Common.Product.ST_PRODUCT_DATA product, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task AppendHistory(Drilling.Common.Product.ST_PRODUCT_HISTORY history, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task ClearActive(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> LoadActive(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HISTORY>> LoadHistory(Int32 maxRows = 100, Int32 days = 14, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task SaveActive(Drilling.Common.Product.ST_PRODUCT_DATA product, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Product.IProductManager

- Kind: interface
- Properties:
  - public Drilling.Common.Product.ST_PRODUCT_DATA Current (get)
- Methods:
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> CompleteProduct(String productId, Boolean isOk, String message, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> CreateProduct(String processId, String productId, String panelId, String lotId, String recipeId, Collections.Generic.IReadOnlyDictionary<String, String> parameters, Collections.Generic.IReadOnlyDictionary<Int32, Int32> headPointCounts, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> LoadActive(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HISTORY>> LoadHistory(Int32 maxRows = 100, Int32 days = 14, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> ScrapProduct(String productId, String reason, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> SetError(String productId, String message, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> SetHeadResult(String productId, Int32 headNo, Boolean isOk, String errorCode = , String message = , Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> SetHeadRunning(String productId, Int32 headNo, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> StartProduct(String productId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> StopProduct(String productId, String message, Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Product.ST_PRODUCT_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Product.ST_PRODUCT_DATA>
- Constructors:
  - public ST_PRODUCT_DATA(String ProductId, String PanelId, String LotId, String ProcessId, String RecipeId, Drilling.Common.Product.EN_PRODUCT_STATE State, Drilling.Common.Product.EN_PRODUCT_RESULT Result, DateTimeOffset CreatedAt, Nullable<DateTimeOffset> StartedAt, Nullable<DateTimeOffset> CompletedAt, Collections.Generic.IReadOnlyDictionary<String, String> Parameters, Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HEAD_RESULT> Heads)
- Properties:
  - public Nullable<DateTimeOffset> CompletedAt (get/set)
  - public DateTimeOffset CreatedAt (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HEAD_RESULT> Heads (get/set)
  - public String LotId (get/set)
  - public String PanelId (get/set)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Parameters (get/set)
  - public String ProcessId (get/set)
  - public String ProductId (get/set)
  - public String RecipeId (get/set)
  - public Drilling.Common.Product.EN_PRODUCT_RESULT Result (get/set)
  - public Nullable<DateTimeOffset> StartedAt (get/set)
  - public Drilling.Common.Product.EN_PRODUCT_STATE State (get/set)

### Drilling.Common.Product.ST_PRODUCT_HEAD_RESULT

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Product.ST_PRODUCT_HEAD_RESULT>
- Constructors:
  - public ST_PRODUCT_HEAD_RESULT(Int32 HeadNo, Drilling.Common.Product.EN_PRODUCT_HEAD_STATE State, Int32 TotalPoints, Int32 CompletedPoints, Drilling.Common.Product.EN_PRODUCT_RESULT Result, String ErrorCode, String Message, Nullable<DateTimeOffset> StartedAt, Nullable<DateTimeOffset> CompletedAt)
- Properties:
  - public Nullable<DateTimeOffset> CompletedAt (get/set)
  - public Int32 CompletedPoints (get/set)
  - private Type EqualityContract (get)
  - public String ErrorCode (get/set)
  - public Int32 HeadNo (get/set)
  - public String Message (get/set)
  - public Drilling.Common.Product.EN_PRODUCT_RESULT Result (get/set)
  - public Nullable<DateTimeOffset> StartedAt (get/set)
  - public Drilling.Common.Product.EN_PRODUCT_HEAD_STATE State (get/set)
  - public Int32 TotalPoints (get/set)

### Drilling.Common.Product.ST_PRODUCT_HISTORY

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Product.ST_PRODUCT_HISTORY>
- Constructors:
  - public ST_PRODUCT_HISTORY(DateTimeOffset OccurredAt, String ProductId, String ProcessId, String RecipeId, String Action, String State, String Result, String Detail)
- Properties:
  - public String Action (get/set)
  - public String Detail (get/set)
  - private Type EqualityContract (get)
  - public DateTimeOffset OccurredAt (get/set)
  - public String ProcessId (get/set)
  - public String ProductId (get/set)
  - public String RecipeId (get/set)
  - public String Result (get/set)
  - public String State (get/set)

### Drilling.Common.Station.CStationManager

- Kind: class
- Interfaces: Drilling.Common.Station.IStationManager
- Constructors:
  - public CStationManager(Drilling.Common.Interface.IInterfaceManager interfaceManager, Drilling.Common.Motion.IMotionManager motionManager, Drilling.Common.InterLock.CInterLockManager interLockManager, Drilling.Common.Station.IAutomationScriptFile automationScriptFile, Drilling.Common.Product.IProductManager productManager = null, Drilling.Common.Log.ILogManager logManager = null, String scriptDirectory = null)
- Fields:
  - private readonly Drilling.Common.Station.CStationProcess _processStation
- Methods:
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_STATION_PROCESS_FLOW_ITEM> GetProcessFlow()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_STATION_STATUS>> GetStationStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> GetStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> PrepareProcessPlan(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Reset(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Start(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Stop(Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Station.CStationProcess

- Kind: class
- Constructors:
  - public CStationProcess(Drilling.Common.Interface.IInterfaceManager interfaceManager, Drilling.Common.Motion.IMotionManager motionManager, Drilling.Common.InterLock.CInterLockManager interLockManager, Drilling.Common.Station.IAutomationScriptFile automationScriptFile, Drilling.Common.Product.IProductManager productManager = null, Drilling.Common.Log.ILogManager logManager = null, String stationName = PROCESS, String scriptDirectory = null)
- Fields:
  - private static String AutoStepComplete
  - private static String AutoStepDevice
  - private static String AutoStepDone
  - private static String AutoStepError
  - private static readonly Collections.Generic.IReadOnlyList<Drilling.Common.Station.CStationProcess+ST_AUTO_STEP_INFO> AutoStepInfos
  - private static String AutoStepInterLock
  - private static String AutoStepOk
  - private static String AutoStepParameter
  - private static String AutoStepPlan
  - private static String AutoStepRunning
  - private static String AutoStepScript
  - private static String AutoStepStop
  - private static String AutoStepTask
  - private static String AutoStepWait
  - private static String AutoStepWaitDone
  - private static readonly Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_STATION_PROCESS_FLOW_ITEM> ProcessFlowItems
  - private readonly Drilling.Common.Station.IAutomationScriptFile _automationScriptFile
  - private readonly Collections.Generic.Dictionary<String, String> _autoStepStates
  - private readonly Drilling.Common.Interface.IInterfaceManager _interfaceManager
  - private readonly Drilling.Common.InterLock.CInterLockManager _interLockManager
  - private Collections.Generic.IReadOnlyList<Drilling.Common.InterLock.ST_INTERLOCK_ITEM> _lastInterLockItems
  - private Drilling.Common.Station.ST_AUTOMATION1_SCRIPT _lastScript
  - private readonly Drilling.Common.Log.ILogManager _logManager
  - private readonly Drilling.Common.Motion.IMotionManager _motionManager
  - private readonly Collections.Generic.List<Drilling.Common.Station.ST_PROCESS_LOG_ITEM> _processLogs
  - private Drilling.Common.Station.ST_PROCESS_MODEL _processModel
  - private readonly Drilling.Common.Product.IProductManager _productManager
  - private readonly Threading.SemaphoreSlim _runLock
  - private Nullable<DateTimeOffset> _scriptCompletedAt
  - private Nullable<DateTimeOffset> _scriptCreatedAt
  - private Nullable<DateTimeOffset> _scriptStartedAt
  - private Drilling.Common.Station.ST_STATION_PROCESS_STATUS _snapshot
  - private Drilling.Common.Station.ST_STATION_STATUS _stationStatus
  - private Drilling.Common.Station.ST_PROCESS_STATISTICS _statistics
- Properties:
  - public Drilling.Common.Station.ST_STATION_PROCESS_STATUS Current (get)
  - public Drilling.Common.Station.ST_STATION_STATUS Status (get)
- Methods:
  - private Void AddProcessLog(String level, String source, String message)
  - private Threading.Tasks.Task BuildAutomationScript(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Threading.CancellationToken cancellationToken)
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> BuildCurrentStepDetails(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, Drilling.Common.Station.EN_PROCESS_STEP processStep)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PATH_POINT> BuildHeadShape(Int32 headNo, Drilling.Common.Station.ST_PROCESS_PLAN processPlan)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> BuildPreview(Drilling.Common.Station.ST_PROCESS_MODEL processModel, Drilling.Common.Station.EN_HEAD_PROCESS_STATUS status)
  - private static Drilling.Common.Station.ST_PROCESS_MODEL BuildProcessModel(Drilling.Common.Station.ST_PROCESS_PLAN processPlan)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> BuildProcessSequence(Drilling.Common.Station.EN_PROCESS_STEP processStep)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> BuildProcessSummary(Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> preview, Drilling.Common.Station.ST_PROCESS_STATISTICS statistics)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> BuildScriptLifecycleItems(Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, Drilling.Common.Station.EN_PROCESS_STEP processStep)
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> BuildScriptStatusItems(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, Drilling.Common.Station.ST_PROCESS_RESULT result)
  - private static Drilling.Common.Station.ST_PROCESS_STATISTICS BuildStatistics(Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> preview, Double progressPercent, TimeSpan elapsedTime)
  - private Threading.Tasks.Task CheckInterLock(Threading.CancellationToken cancellationToken)
  - private Drilling.Common.Station.ST_PROCESS_PLAN CheckProcessPlan()
  - private Threading.Tasks.Task CompleteProcess(Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task CompleteProduct(Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> preview, Drilling.Common.Station.ST_PROCESS_RESULT result, Threading.CancellationToken cancellationToken)
  - private static Collections.Generic.Dictionary<String, String> CreateAutoStepStateMap()
  - private static String[] CreateHeadKeys(Int32 headNo, String[] names)
  - private Threading.Tasks.Task CreateProduct(Drilling.Common.Station.ST_PROCESS_MODEL processModel, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> preview, Threading.CancellationToken cancellationToken)
  - private Drilling.Common.Station.ST_STATION_PROCESS_STATUS CreateSnapshot(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> preview, Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, Drilling.Common.Station.EN_PROCESS_STEP processStep, Drilling.Common.Station.ST_PROCESS_RESULT result)
  - private static Drilling.Common.Station.ST_PROCESS_STATISTICS EmptyStatistics()
  - private Void EnsureStartAllowed()
  - private Threading.Tasks.Task ExecuteAutoStep(String stepKey, Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Func<Drilling.Common.Station.ST_PROCESS_PLAN, Threading.CancellationToken, Threading.Tasks.Task> action, Threading.CancellationToken cancellationToken)
  - private static String FormatDateTime(Nullable<DateTimeOffset> value)
  - private static String FormatDuration(TimeSpan value)
  - private static String FormatExecuteState(Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, Drilling.Common.Station.ST_PROCESS_RESULT result)
  - private static String FormatInterLockBlockedMessage(Drilling.Common.InterLock.ST_INTERLOCK_SUMMARY interLock)
  - private static String FormatProcessResultDetail(Drilling.Common.Station.ST_PROCESS_RESULT result, String processId, String recipeId, String productId)
  - private static String FormatScriptStatus(Drilling.Common.Station.EN_SCRIPT_STATUS status)
  - private static String GetAutoStepName(String stepKey)
  - private String GetCurrentProcessProductId()
  - private Threading.Tasks.Task<Drilling.Common.Managers.ST_DEVICE_STATUS> GetDeviceStatus(Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task<Drilling.Common.InterLock.ST_INTERLOCK_SUMMARY> GetInterLockSummary(Threading.CancellationToken cancellationToken)
  - public static Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_STATION_PROCESS_FLOW_ITEM> GetProcessFlow()
  - private Drilling.Common.Station.ST_PROCESS_MODEL GetProcessModel()
  - private static String LifecycleState(Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, Drilling.Common.Station.EN_PROCESS_STEP processStep, Int32 stepNo)
  - private Void LoadProcessParameter(Drilling.Common.Station.ST_PROCESS_PLAN processPlan)
  - private Void MarkRunningAutoSteps(String state)
  - private Threading.Tasks.Task PrepareProcessDevices(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> PrepareProcessPlan(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Threading.CancellationToken cancellationToken = null)
  - private static String ReadAnyParameter(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, String defaultValue, String[] keys)
  - private String ReadAutoStepState(String stepKey, Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, Drilling.Common.Station.EN_PROCESS_STEP processStep)
  - private static Boolean ReadBool(Collections.Generic.IReadOnlyDictionary<String, String> parameters, String key, Boolean defaultValue)
  - private static Boolean ReadBoolAny(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Boolean defaultValue, String[] keys)
  - private static Double ReadDouble(Collections.Generic.IReadOnlyDictionary<String, String> parameters, String key, Double defaultValue)
  - private static Double ReadDoubleAny(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Double defaultValue, String[] keys)
  - private static Int32 ReadInt(Collections.Generic.IReadOnlyDictionary<String, String> parameters, String key, Int32 defaultValue)
  - private static Int32 ReadIntAny(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Int32 defaultValue, String[] keys)
  - private static String ReadParameter(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, String key, String defaultValue)
  - private static String ReadText(Collections.Generic.IReadOnlyDictionary<String, String> parameters, String key, String defaultValue)
  - private static String ReadTextAny(Collections.Generic.IReadOnlyDictionary<String, String> parameters, String defaultValue, String[] keys)
  - private Void RefreshSnapshot()
  - private Threading.Tasks.Task ReportProcessResult(Drilling.Common.Station.ST_PROCESS_RESULT result, String action, Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Reset(Threading.CancellationToken cancellationToken = null)
  - private Void ResetAutoStepStates()
  - private static String SequenceState(Drilling.Common.Station.EN_PROCESS_STEP processStep, Int32 sequenceNo)
  - private Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> SetAlarm(String message, Threading.CancellationToken cancellationToken)
  - private Void SetAutoStepState(String stepKey, String state)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> SetHeadStatus(Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> preview, Drilling.Common.Station.EN_PROCESS_STEP processStep)
  - private Threading.Tasks.Task SetProductError(String message, Threading.CancellationToken cancellationToken)
  - private Void SetStationState(Drilling.Common.Station.EN_STATION_STATE state, Drilling.Common.Station.EN_PROCESS_STEP processStep, Drilling.Common.Station.EN_SCRIPT_STATUS scriptStatus, String message)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Start(Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task StartAutomationTask(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task StartProduct(Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> preview, Threading.CancellationToken cancellationToken)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Stop(Threading.CancellationToken cancellationToken = null)
  - private Threading.Tasks.Task StopProduct(String message, Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task WaitProcessDone(Threading.CancellationToken cancellationToken)

### Drilling.Common.Station.EN_AEROTECH_MODE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Mof = 0
  - Ifov = 1
  - Scanner = 2

### Drilling.Common.Station.EN_AEROTECH_PSO_MODE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Unused = 0
  - WindowMask = 1
  - ExtSync = 2
  - LaserMask = 3
  - ExtSyncGalvo = 4

### Drilling.Common.Station.EN_HEAD_PROCESS_STATUS

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Ready = 0
  - Running = 1
  - Completed = 2
  - Error = 3
  - Disabled = 4

### Drilling.Common.Station.EN_PROCESS_STEP

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Idle = 0
  - ProcessPlanned = 1
  - ReadyToRun = 2
  - Running = 3
  - Completed = 4
  - Stopped = 5
  - Error = 6

### Drilling.Common.Station.EN_SCRIPT_STATUS

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - NotCreated = 0
  - Created = 1
  - Running = 2
  - Completed = 3
  - Error = 4

### Drilling.Common.Station.EN_STATION_ID

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Process = 0

### Drilling.Common.Station.EN_STATION_STATE

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Idle = 0
  - Check = 1
  - Process = 2
  - Complete = 3
  - Alarm = 4
  - Stopped = 5

### Drilling.Common.Station.IAutomation1Script

- Kind: interface
- Properties:
  - public String FileName (get)
  - public String FilePath (get)
  - public Collections.Generic.IReadOnlyList<String> Lines (get)
- Methods:
  - public Void AddLine(String line)
  - public Void Arc(Double startX, Double startY, Double endX, Double endY, Double centerX, Double centerY, Double angle)
  - public Void BufferedEnd()
  - public Void Clear()
  - public Void DeclareEncoderVariable(String axis = , Boolean useFeedback = False)
  - public Void DefaultSetting(Double scannerAcc = 500000, Int32 motionUpdateRate = 0, Int32 executeLineCount = 110, Boolean resetPso = True)
  - public Void DisableAxisPair()
  - public Void Dwell(Double delay)
  - public Void EnableAxisPair()
  - public Void EncoderNotFeedback(String axis)
  - public Void End(Boolean bufferedRun = False)
  - public Void FaultAckAxisPair()
  - public Void HomeAxisPair()
  - public Void InitDeclareVariable()
  - public Void InitDeclareVariableIFOV()
  - public Void InitEncoderCount(String galvoAxis)
  - public Void Jump(Double x, Double y)
  - public Void JumpLinear(Double x, Double y)
  - public Void JumpRel(Double x, Double y)
  - public Void LaserAuto()
  - public Void LaserFire(Boolean on)
  - public Void LaserOff()
  - public Void LaserOn()
  - public Void Mark(Double x, Double y)
  - public Void MarkRel(Double x, Double y)
  - public Void OffsetClearAxisPair()
  - public Void OffsetSetAxisPair(Double x, Double y)
  - public Void ProgramEnd()
  - public Void ProgramStart()
  - public Void PsoLaserControl(Boolean on, Boolean manual = False)
  - public Void ReleaseEncoderScaleFactor(String galvoAxis)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_AUTOMATION1_SCRIPT> Save(Threading.CancellationToken cancellationToken = null)
  - public Void SetAbsoluteMode()
  - public Void SetAerotechEncoderReset(String axisX, String axisY)
  - public Void SetAxis(String xAxis, String yAxis, String laserAxis = null)
  - public Void SetCoordinatedAccelLimit(Int64 acc, Int64 arcAcc)
  - public Void SetDeviceNo(Int32 deviceNo)
  - public Void SetEmulatedQuadratureDividerX(Int32 value)
  - public Void SetEmulatedQuadratureDividerY(Int32 value)
  - public Void SetEncoderScaleFactor(String galvoAxis, String encoderAxis, Int32 scale)
  - public Void SetEncoderScaleFactor(String galvoAxis, String encoderAxis, Boolean directionPlus)
  - public Void SetEncoderScaleFactor(String galvoAxis, String encoderAxis, Double encoderX, Double encoderY, Boolean directionPlus)
  - public Void SetEncoderScaleFactorByPrimaryDivider(String galvoAxis, String encoderAxis, Boolean directionPlus)
  - public Void SetExecuteLineCount(Int32 lineCount)
  - public Void SetFrequency(Double frequencyKhz)
  - public Void SetGalvoPosZero()
  - public Void SetGearing(String masterAxis, String slaveAxis)
  - public Void SetGearingOff(String slaveAxis = AUTO)
  - public Void SetHomePos()
  - public Void SetIFOV(Boolean use)
  - public Void SetIFOVEmulatedQuadratureDivider()
  - public Void SetIFOVIO(Boolean use = True)
  - public Void SetIFOVPair(String xStageAxis, String yStageAxis, Boolean xDirection, Boolean yDirection)
  - public Void SetIFOVScaleXY()
  - public Void SetIFOVSize(Double size)
  - public Void SetIFOVSyncAxis()
  - public Void SetIFOVTime(Int64 time)
  - public Void SetIFOVTrackingAccel(Int64 acc)
  - public Void SetIFOVTrackingSpeed(Int64 speed)
  - public Void SetIncrementalMode()
  - public Void SetJumpSpeed(Double speedMmPerSec)
  - public Void SetJumpSpeedRate(Double speedMmPerSec, Double rate = 1)
  - public Void SetLaserDelay(Double onDelay, Double offDelay)
  - public Void SetLaserMode(Int32 mode)
  - public Void SetLaserPower(Double powerPercent, Double outputRate = 100, Boolean analogOutputUse = False)
  - public Void SetMarkAcc(Double acc)
  - public Void SetMarkSpeed(Double speedMmPerSec)
  - public Void SetMoveBlending(Boolean use)
  - public Void SetMoveDelay(Double delay, Boolean addTactTime = True)
  - public Void SetMoveUpdateRate(Int32 rate)
  - public Void SetNMarkDriveLaserControl(Boolean use)
  - public Void SetProjection(String axis, Double offsetX, Double offsetY, Double offsetT)
  - public Void SetProjectionOff(String axis)
  - public Void SetPSO(Double pulseDistance, Double totalTime, Double laserOnTime, Double delay, Drilling.Common.Station.EN_AEROTECH_MODE mode, Drilling.Common.Station.EN_AEROTECH_PSO_MODE psoMode, Double frequencyKhz, Double powerPercent, Int32 windowMaskDirection, Double markSpeed, Boolean manual = False)
  - public Void SetPSOChangePower(Double frequencyKhz, Double powerPercent)
  - public Void SetPSODistance(Double pulseDistance)
  - public Void SetPSOFire(Double totalTime, Double laserOnTime, Int32 count, Double delay, Drilling.Common.Station.EN_AEROTECH_MODE mode)
  - public Void SetPSOLaserWindowMask(Boolean on, Double windowStartRange = 0, Double windowEndRange = 0)
  - public Void SetPSOOnOff(Boolean on)
  - public Void SetPulseOnTimeLaserPower(Double powerPercent, Double dutyPercent, Double outputRate = 100)
  - public Void SetScannerAcc(Double acc)
  - public Void SetScannerRotate(Double angle)
  - public Void SetScannerRotate(String laserAxis, Double angle)
  - public Void SetScanPlannerStageEncoder(String stageAxis)
  - public Void SetScanPlannerStageEncoderMode(Boolean use)
  - public Void SetScanTrajectoryFIRFilterX(Int64 delay)
  - public Void SetScanTrajectoryFIRFilterY(Int64 delay)
  - public Void SetSignalLogTrigger(Boolean use)
  - public Void SetSoftwareLimitSetup(Boolean use = True)
  - public Void SetStageAxis(String xAxis, String yAxis)
  - public Void SetStageEmulatedQuadratureDivider(Int32 xValue, Int32 yValue)
  - public Void SetStageSpeed(Double speedX, Double speedY)
  - public Void SetStageTrajectoryFIRFilterX(Int64 delay)
  - public Void SetStageTrajectoryFIRFilterY(Int64 delay)
  - public Void SetTaskAccelLimit(Int64 acc, Int64 arcAcc)
  - public Void SetWaitForEncoder(String axis, Double position, Boolean directionPlus = True)
  - public Void SetWaitForEncoder(String axis, Boolean directionPlus, Double position, Double limit, Double encoderScale = 1)
  - public Void SetWaitForEncoder2Axis(String axisX, String axisY, Boolean inToOut, Double posX, Double posY, Double limitX, Double limitY)
  - public Void SetWaitForStartAxis2(String axisX, String axisY, Boolean inToOut, Double posX, Double posY, Double limitX, Double limitY)
  - public Void SetWaitModeAuto()
  - public Void Start(String title = )
  - public Void WaitInpos()
  - public Void WaitMoveDone()

### Drilling.Common.Station.IAutomationScriptFile

- Kind: interface
- Properties:
  - public String ScriptFileName (get)
- Methods:
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_AUTOMATION1_SCRIPT> Build(Drilling.Common.Station.ST_PROCESS_MODEL processModel, Threading.CancellationToken cancellationToken = null)
  - public Drilling.Common.Station.IAutomation1Script Create(String fileName = null)

### Drilling.Common.Station.IStationManager

- Kind: interface
- Methods:
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_STATION_PROCESS_FLOW_ITEM> GetProcessFlow()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_STATION_STATUS>> GetStationStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> GetStatus(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> PrepareProcessPlan(Drilling.Common.Station.ST_PROCESS_PLAN processPlan, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Reset(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Start(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_STATION_PROCESS_STATUS> Stop(Threading.CancellationToken cancellationToken = null)

### Drilling.Common.Station.CStationProcess+ST_AUTO_STEP_INFO

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.CStationProcess+ST_AUTO_STEP_INFO>
- Constructors:
  - public ST_AUTO_STEP_INFO(String Key, String DisplayName)
  - private ST_AUTO_STEP_INFO(Drilling.Common.Station.CStationProcess+ST_AUTO_STEP_INFO original)
- Properties:
  - public String DisplayName (get/set)
  - private Type EqualityContract (get)
  - public String Key (get/set)

### Drilling.Common.Station.ST_AUTOMATION1_SCRIPT

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_AUTOMATION1_SCRIPT>
- Constructors:
  - public ST_AUTOMATION1_SCRIPT(String FileName, String FilePath, Collections.Generic.IReadOnlyList<String> Lines, Int32 TotalPoints, Int32 HeadCount, DateTimeOffset CreatedAt)
- Properties:
  - public DateTimeOffset CreatedAt (get/set)
  - private Type EqualityContract (get)
  - public String FileName (get/set)
  - public String FilePath (get/set)
  - public Int32 HeadCount (get/set)
  - public Collections.Generic.IReadOnlyList<String> Lines (get/set)
  - public Int32 TotalPoints (get/set)

### Drilling.Common.Station.ST_HEAD_PATH_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_HEAD_PATH_DATA>
- Constructors:
  - public ST_HEAD_PATH_DATA(Int32 HeadNo, Drilling.Common.Station.EN_HEAD_PROCESS_STATUS Status, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PATH_POINT> Points)
- Properties:
  - private Type EqualityContract (get)
  - public Int32 HeadNo (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PATH_POINT> Points (get/set)
  - public Drilling.Common.Station.EN_HEAD_PROCESS_STATUS Status (get/set)

### Drilling.Common.Station.ST_HEAD_PROCESS_DATA

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_HEAD_PROCESS_DATA>
- Constructors:
  - public ST_HEAD_PROCESS_DATA(Int32 HeadNo, Boolean Use, String Shape, Double LaserPower, Double FrequencyKhz, Int32 ShotCount, Double MarkSpeed, Double JumpSpeed, Double OffsetX, Double OffsetY, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PATH_POINT> Path)
- Properties:
  - private Type EqualityContract (get)
  - public Double FrequencyKhz (get/set)
  - public Int32 HeadNo (get/set)
  - public Double JumpSpeed (get/set)
  - public Double LaserPower (get/set)
  - public Double MarkSpeed (get/set)
  - public Double OffsetX (get/set)
  - public Double OffsetY (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PATH_POINT> Path (get/set)
  - public String Shape (get/set)
  - public Int32 ShotCount (get/set)
  - public Boolean Use (get/set)

### Drilling.Common.Station.ST_PATH_POINT

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_PATH_POINT>
- Constructors:
  - public ST_PATH_POINT(Double X, Double Y, Boolean LaserOn = True)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean LaserOn (get/set)
  - public Double X (get/set)
  - public Double Y (get/set)

### Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM>
- Constructors:
  - public ST_PROCESS_DISPLAY_ITEM(String Name, String Value, String Detail = )
- Properties:
  - public String Detail (get/set)
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String Value (get/set)

### Drilling.Common.Station.ST_PROCESS_LOG_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_PROCESS_LOG_ITEM>
- Constructors:
  - public ST_PROCESS_LOG_ITEM(DateTimeOffset OccurredAt, String Level, String Source, String Message)
- Properties:
  - private Type EqualityContract (get)
  - public String Level (get/set)
  - public String Message (get/set)
  - public DateTimeOffset OccurredAt (get/set)
  - public String Source (get/set)

### Drilling.Common.Station.ST_PROCESS_MODEL

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_PROCESS_MODEL>
- Constructors:
  - public ST_PROCESS_MODEL(Drilling.Common.Station.ST_PROCESS_PLAN Plan, Drilling.Common.Product.ST_PRODUCT_DATA Product, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PROCESS_DATA> Heads, Collections.Generic.IReadOnlyDictionary<String, String> Parameters, DateTimeOffset CreatedAt)
- Properties:
  - public DateTimeOffset CreatedAt (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PROCESS_DATA> Heads (get/set)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Parameters (get/set)
  - public Drilling.Common.Station.ST_PROCESS_PLAN Plan (get/set)
  - public Drilling.Common.Product.ST_PRODUCT_DATA Product (get/set)

### Drilling.Common.Station.ST_PROCESS_PLAN

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_PROCESS_PLAN>
- Constructors:
  - public ST_PROCESS_PLAN(String ProcessId, String RecipeId, String ProductId, String PanelId, String LotId, DateTimeOffset CreatedAt, Collections.Generic.IReadOnlyDictionary<String, String> Parameters)
- Properties:
  - public DateTimeOffset CreatedAt (get/set)
  - private Type EqualityContract (get)
  - public String LotId (get/set)
  - public String PanelId (get/set)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Parameters (get/set)
  - public String ProcessId (get/set)
  - public String ProductId (get/set)
  - public String RecipeId (get/set)

### Drilling.Common.Station.ST_PROCESS_RESULT

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_PROCESS_RESULT>
- Constructors:
  - public ST_PROCESS_RESULT(Boolean IsSuccess, String Message, DateTimeOffset CompletedAt)
- Properties:
  - public DateTimeOffset CompletedAt (get/set)
  - private Type EqualityContract (get)
  - public Boolean IsSuccess (get/set)
  - public String Message (get/set)

### Drilling.Common.Station.ST_PROCESS_STATISTICS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_PROCESS_STATISTICS>
- Constructors:
  - public ST_PROCESS_STATISTICS(Int32 TotalPoints, Int32 MoveCount, Int32 LaserOnSegments, TimeSpan EstimatedTime, TimeSpan ElapsedTime, Double ProgressPercent)
- Properties:
  - public TimeSpan ElapsedTime (get/set)
  - private Type EqualityContract (get)
  - public TimeSpan EstimatedTime (get/set)
  - public Int32 LaserOnSegments (get/set)
  - public Int32 MoveCount (get/set)
  - public Double ProgressPercent (get/set)
  - public Int32 TotalPoints (get/set)

### Drilling.Common.Station.ST_STATION_PROCESS_FLOW_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_STATION_PROCESS_FLOW_ITEM>
- Constructors:
  - public ST_STATION_PROCESS_FLOW_ITEM(Int32 Order, String StepKey, String StepName, Drilling.Common.Station.EN_STATION_STATE RunningState, Drilling.Common.Station.EN_PROCESS_STEP RunningStep, Drilling.Common.Station.EN_SCRIPT_STATUS ScriptStatus, String OnSuccess, String OnFail)
- Properties:
  - private Type EqualityContract (get)
  - public String OnFail (get/set)
  - public String OnSuccess (get/set)
  - public Int32 Order (get/set)
  - public Drilling.Common.Station.EN_STATION_STATE RunningState (get/set)
  - public Drilling.Common.Station.EN_PROCESS_STEP RunningStep (get/set)
  - public Drilling.Common.Station.EN_SCRIPT_STATUS ScriptStatus (get/set)
  - public String StepKey (get/set)
  - public String StepName (get/set)

### Drilling.Common.Station.ST_STATION_PROCESS_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_STATION_PROCESS_STATUS>
- Constructors:
  - public ST_STATION_PROCESS_STATUS(Drilling.Common.Station.ST_PROCESS_PLAN ProcessPlan, Drilling.Common.Station.ST_PROCESS_MODEL ProcessModel, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> HeadPreviews, Drilling.Common.Station.EN_SCRIPT_STATUS ScriptStatus, Drilling.Common.Station.EN_PROCESS_STEP ProcessStep, Drilling.Common.Station.ST_PROCESS_RESULT Result, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ProcessSequence, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> CurrentStepDetails, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ProcessSummary, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_LOG_ITEM> ProcessLogs, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ScriptStatusItems, Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ScriptLifecycleItems, Collections.Generic.IReadOnlyList<Drilling.Common.InterLock.ST_INTERLOCK_ITEM> InterlockItems, Drilling.Common.Station.ST_PROCESS_STATISTICS Statistics)
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> CurrentStepDetails (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_HEAD_PATH_DATA> HeadPreviews (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.InterLock.ST_INTERLOCK_ITEM> InterlockItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_LOG_ITEM> ProcessLogs (get/set)
  - public Drilling.Common.Station.ST_PROCESS_MODEL ProcessModel (get/set)
  - public Drilling.Common.Station.ST_PROCESS_PLAN ProcessPlan (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ProcessSequence (get/set)
  - public Drilling.Common.Station.EN_PROCESS_STEP ProcessStep (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ProcessSummary (get/set)
  - public Drilling.Common.Station.ST_PROCESS_RESULT Result (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ScriptLifecycleItems (get/set)
  - public Drilling.Common.Station.EN_SCRIPT_STATUS ScriptStatus (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM> ScriptStatusItems (get/set)
  - public Drilling.Common.Station.ST_PROCESS_STATISTICS Statistics (get/set)

### Drilling.Common.Station.ST_STATION_STATUS

- Kind: class
- Interfaces: System.IEquatable<Drilling.Common.Station.ST_STATION_STATUS>
- Constructors:
  - public ST_STATION_STATUS(Drilling.Common.Station.EN_STATION_ID StationId, String StationName, Drilling.Common.Station.EN_STATION_STATE State, Drilling.Common.Station.EN_PROCESS_STEP ProcessStep, Drilling.Common.Station.EN_SCRIPT_STATUS ScriptStatus, String LastMessage, DateTimeOffset ChangedAt)
- Properties:
  - public DateTimeOffset ChangedAt (get/set)
  - private Type EqualityContract (get)
  - public String LastMessage (get/set)
  - public Drilling.Common.Station.EN_PROCESS_STEP ProcessStep (get/set)
  - public Drilling.Common.Station.EN_SCRIPT_STATUS ScriptStatus (get/set)
  - public Drilling.Common.Station.EN_STATION_STATE State (get/set)
  - public Drilling.Common.Station.EN_STATION_ID StationId (get/set)
  - public String StationName (get/set)

## Drilling.File

Type count: 17

### Drilling.File.IPS.CBETFile

- Kind: class
- Interfaces: Drilling.Common.Interface.IBETFile
- Constructors:
  - public CBETFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<String> Headers
  - private readonly String _betDirectory
- Methods:
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA> CreateDefaultTable()
  - private Void EnsureFiles()
  - private String GetFormPath()
  - private String GetValuePath()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA>> Load(Threading.CancellationToken cancellationToken = null)
  - private static Boolean ReadBool(String value, Boolean defaultValue)
  - private static Double ReadDouble(String value, Double defaultValue)
  - private static Int32 ReadInt(String value, Int32 defaultValue)
  - private Collections.Generic.List<Drilling.Common.Interface.ST_BET_TABLE_DATA> ReadTable(String path)
  - public Threading.Tasks.Task Save(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA> table, Threading.CancellationToken cancellationToken = null)
  - private static Void WriteTable(String path, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA> table)

### Drilling.File.IPS.CConfigStructureFile

- Kind: class
- Interfaces: Drilling.Common.Managers.IConfigStructureFile
- Constructors:
  - public CConfigStructureFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<Drilling.File.IPS.CConfigStructureFile+ST_VALUE_CSV> OptionalValueFiles
  - private static readonly Collections.Generic.IReadOnlyList<Drilling.File.IPS.CConfigStructureFile+ST_REQUIRED_CSV> RequiredCsvFiles
- Methods:
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckCsv(Drilling.File.IPS.CConfigStructureFile+ST_REQUIRED_CSV csvFile)
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckCsvValueFiles(String itemName, String relativeDirectory, String pattern, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> requiredHeaderGroups, Action<String, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>>> rowValidator, Threading.CancellationToken cancellationToken)
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckLineValueFiles(String itemName, String relativeDirectory, String pattern, Func<Collections.Generic.IReadOnlyList<String>, Boolean> lineValidator, String invalidMessage, Threading.CancellationToken cancellationToken)
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckManualValueFiles(Threading.CancellationToken cancellationToken)
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckPowerMeterValueFiles(Threading.CancellationToken cancellationToken)
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckRecipeValueFiles(Threading.CancellationToken cancellationToken)
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckRoot()
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS CheckValueCsv(Drilling.File.IPS.CConfigStructureFile+ST_VALUE_CSV valueFile)
  - private static Drilling.File.IPS.CConfigStructureFile+ST_REQUIRED_CSV Csv(String itemName, String relativePath, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> requiredHeaderGroups, Action<String, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>>> rowValidator)
  - private Drilling.Common.Managers.ST_CONFIG_FILE_STATUS EnsureDirectory(String itemName, String relativePath, Threading.CancellationToken cancellationToken)
  - private static String GetFirstValue(Collections.Generic.IReadOnlyDictionary<String, String> row, String[] names)
  - private String GetPath(String relativePath)
  - private static String NormalizePath(String path)
  - private static Collections.Generic.IReadOnlyList<String> SplitCsvLine(String line, String path, Int32 lineNo)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_CONFIG_FILE_STATUS>> Validate(Threading.CancellationToken cancellationToken = null)
  - private static Void ValidateDeviceNumberKey(String tableName, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> rows)
  - private static Void ValidateIdKey(String tableName, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> rows)
  - private static Void ValidateIndexKey(String tableName, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> rows)
  - private static Void ValidateNameKey(String tableName, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> rows)
  - private static Void ValidateStepKey(String tableName, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> rows)
  - private static Void ValidateTabNameKey(String tableName, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> rows)
  - private static Void ValidateUniqueKey(String tableName, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> rows, Collections.Generic.IReadOnlyList<String> displayKeyNames, Func<Collections.Generic.IReadOnlyDictionary<String, String>, String> createKey)
  - private static Drilling.File.IPS.CConfigStructureFile+ST_VALUE_CSV ValueCsv(String itemName, String relativePath, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> requiredHeaderGroups, Action<String, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>>> rowValidator)

### Drilling.File.IPS.CInterfaceFile

- Kind: class
- Interfaces: Drilling.Common.Managers.IInterfaceFile
- Constructors:
  - public CInterfaceFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<String> FieldNames
  - private static readonly Collections.Generic.IReadOnlyList<String> Headers
  - private static readonly Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> RequiredHeaderGroups
  - private readonly Drilling.Common.Log.CLogManager _logManager
- Methods:
  - private static String BuildComparisonText(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> CreateEmptyFieldMap()
  - private static Collections.Generic.IReadOnlyDictionary<String, String> CreateFieldMap(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private static String CreateInterfaceKey(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private static String DeviceText(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private static String FormatInterfaceLabel(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private String GetInterfacePath()
  - private static String InterfaceTypeText(Drilling.Common.Interface.EN_INTERFACE_TYPE type)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> LoadInterfaceRows()
  - private static String Normalize(String value)
  - private Drilling.Common.Interface.ST_INTERFACE_DATA Parse(Collections.Generic.IReadOnlyDictionary<String, String> row, Int32 rowNo)
  - private static Drilling.Common.Interface.EN_EQP_MODULE ParseDevice(String value)
  - private static Drilling.Common.Interface.EN_INTERFACE_TYPE ParseInterfaceType(String value)
  - private static Collections.Generic.IReadOnlyList<String> ReadArguments(Collections.Generic.IReadOnlyDictionary<String, String> row)
  - private static Boolean ReadRequiredBool(String value, Int32 rowNo, String fieldName)
  - private static Int32 ReadRequiredInt(String value, Int32 rowNo, String fieldName)
  - private static Void RequireArgument(Drilling.Common.Interface.ST_INTERFACE_DATA data, String value, String fieldName)
  - private static Void RequirePositiveInt(Drilling.Common.Interface.ST_INTERFACE_DATA data, String value, String fieldName)
  - private static String RequireText(Collections.Generic.IReadOnlyDictionary<String, String> row, Int32 rowNo, String[] names)
  - public Threading.Tasks.Task SaveAll(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces, Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> ToRow(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private static Void Validate(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces)
  - private static Void ValidateConnectionArguments(Drilling.Common.Interface.ST_INTERFACE_DATA data)
  - private static Void ValidateParity(Drilling.Common.Interface.ST_INTERFACE_DATA data, String value)
  - private Void ValidateSavedRows(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> expectedRows)
  - private static Void ValidateStopBits(Drilling.Common.Interface.ST_INTERFACE_DATA data, String value)
  - private Void WriteFieldModifyLog(String interfaceLabel, Collections.Generic.IReadOnlyDictionary<String, String> oldFields, Collections.Generic.IReadOnlyDictionary<String, String> newFields)
  - private Void WriteModifyLog(Collections.Generic.IReadOnlyDictionary<String, Drilling.Common.Interface.ST_INTERFACE_DATA> oldRows, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> newRows)

### Drilling.File.IPS.CIoFile

- Kind: class
- Interfaces: Drilling.Common.Motion.IIoFile
- Constructors:
  - public CIoFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<String> Headers
  - private static readonly Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> RequiredHeaderGroups
- Methods:
  - private static Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> CreateDefaultRows()
  - private Void EnsureFile()
  - private String GetIoPath()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)
  - private static String NormalizeAddress(String value)
  - private static String NormalizeId(String value)
  - private static String NormalizeText(String value)
  - private Drilling.Common.Motion.ST_IO_DATA Parse(Collections.Generic.IReadOnlyDictionary<String, String> row, Int32 rowNo)
  - private static Boolean ReadBool(String value, Boolean defaultValue)
  - private static String ReadController(String value)
  - private static Boolean ReadDirection(String value, Int32 rowNo)
  - private static String ReadFirst(Collections.Generic.IReadOnlyDictionary<String, String> row, String[] names)
  - private static Int32 ReadInt(String value, Int32 rowNo, String fieldName, Int32 defaultValue)
  - private static String RequireText(Collections.Generic.IReadOnlyDictionary<String, String> row, Int32 rowNo, String[] names)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> Row(String id, String address, String name, Boolean isOutput, Boolean initialState, Int32 displayOrder, String description)
  - private static Void Validate(Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_IO_DATA> rows)

### Drilling.File.IPS.CIpsRecipeFile

- Kind: class
- Interfaces: Drilling.Common.Managers.IRecipeFile
- Constructors:
  - public CIpsRecipeFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<String> FormHeaders
  - private readonly String _configRoot
  - private readonly Drilling.Common.Log.CLogManager _logManager
  - private readonly String _recipeDirectory
- Methods:
  - private static String CreateKey(String tab, String name)
  - public Threading.Tasks.Task Delete(String recipeId, Threading.CancellationToken cancellationToken = null)
  - private static Void DeleteIfExists(String path)
  - private static String Escape(String value)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_RECIPE_DATA> Find(String recipeId, Threading.CancellationToken cancellationToken = null)
  - private static Drilling.Common.Managers.ST_RECIPE_FORM_ITEM FindFormItem(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_FORM_ITEM> formItems, Drilling.Common.Managers.ST_RECIPE_PARAM parameter)
  - private static String FormatRecipeLine(String tab, String name, String value)
  - private String GetFormPath()
  - private static String GetOrDefault(String value, String defaultValue)
  - private static String GetRecipeName(Drilling.Common.Managers.ST_RECIPE_DATA recipe)
  - private String GetRecipePath(String recipeId)
  - private static String GetSafeFileName(String recipeId)
  - private static String GetValue(Collections.Generic.IReadOnlyDictionary<String, String> values, String tab, String name, String defaultValue)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_FORM_ITEM> LoadFormItems()
  - private Drilling.Common.Managers.ST_RECIPE_DATA LoadRecipe(String path, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_FORM_ITEM> formItems)
  - private static Drilling.Common.Managers.ST_RECIPE_VALUE ParseRecipeLine(String line)
  - private static Boolean ReadBool(String value, Boolean defaultValue)
  - private static Drilling.Common.Managers.EN_RECIPE_DATA_TYPE ReadDataType(String value)
  - private static Double ReadDouble(String value, Double defaultValue)
  - private static Int32 ReadInt(String value, Int32 defaultValue)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_VALUE> ReadRecipeValues(String path)
  - public Threading.Tasks.Task Rename(String oldRecipeId, String newRecipeId, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(Drilling.Common.Managers.ST_RECIPE_DATA recipe, Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<String> SplitCsvLine(String line)
  - private static Void ValidateSavedRecipeFile(String path, Collections.Generic.IReadOnlyDictionary<String, String> expectedValues)

### Drilling.File.IPS.CManualScanFile

- Kind: class
- Interfaces: Drilling.Common.Managers.IManualScanFile
- Constructors:
  - public CManualScanFile(String configRoot)
- Fields:
  - private static String DefaultSettingName
  - private static readonly Collections.Generic.IReadOnlyList<String> ValueHeaders
  - private readonly String _manualDirectory
- Methods:
  - private static Collections.Generic.IReadOnlyList<Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM> CreateFallbackFormItems()
  - public Threading.Tasks.Task Delete(String settingName, Threading.CancellationToken cancellationToken = null)
  - private String GetDefaultSettingName()
  - private String GetFormPath()
  - private static String GetOrDefault(String value, String defaultValue)
  - private String GetSettingPath(String settingName)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<String>> List(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM> Load(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM> Load(String settingName, Threading.CancellationToken cancellationToken = null)
  - private Collections.Generic.IReadOnlyList<Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM> LoadFormItems()
  - private static String NormalizeSettingName(String settingName)
  - private static Boolean ReadBool(String value, Boolean defaultValue)
  - private static Drilling.Common.Managers.EN_RECIPE_DATA_TYPE ReadDataType(String value)
  - private Double ReadDouble(Collections.Generic.IReadOnlyDictionary<String, String> values, Collections.Generic.IReadOnlyList<Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM> formItems, String key, Double defaultValue)
  - private static Double ReadDoubleValue(String value, Double defaultValue)
  - private static Int32 ReadInt(String value, Int32 defaultValue)
  - private String ReadString(Collections.Generic.IReadOnlyDictionary<String, String> values, Collections.Generic.IReadOnlyList<Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM> formItems, String key, String defaultValue)
  - public Threading.Tasks.Task Rename(String oldSettingName, String newSettingName, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM settings, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task Save(String settingName, Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM settings, Threading.CancellationToken cancellationToken = null)
  - private static String ValidateBoolParameter(Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM formItem, String value)
  - private static String ValidateDoubleParameter(Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM formItem, String value)
  - private static String ValidateIntParameter(Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM formItem, String value)
  - private static String ValidateNumericRange(Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM formItem, Double value)
  - private Void ValidateSavedSetting(String settingName, Collections.Generic.IReadOnlyDictionary<String, String> values, Threading.CancellationToken cancellationToken = null)
  - private static Void ValidateValues(Collections.Generic.IReadOnlyList<Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM> formItems, Collections.Generic.IReadOnlyDictionary<String, String> values)

### Drilling.File.IPS.CMotorFile

- Kind: class
- Interfaces: Drilling.Common.Motion.IMotorFile
- Constructors:
  - public CMotorFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<String> Headers
  - private static readonly Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> RequiredHeaderGroups
- Methods:
  - private static Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> CreateDefaultRows()
  - private Void EnsureFile()
  - private String GetMotorPath()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA>> LoadAll(Threading.CancellationToken cancellationToken = null)
  - private static String Normalize(String value)
  - private Drilling.Common.Motion.ST_MOTOR_DATA Parse(Collections.Generic.IReadOnlyDictionary<String, String> row, Int32 rowNo)
  - private static Boolean ReadBool(String value, Boolean defaultValue)
  - private static String ReadController(String value)
  - private static Double ReadDouble(String value, Int32 rowNo, String fieldName, Double defaultValue)
  - private static String ReadFirst(Collections.Generic.IReadOnlyDictionary<String, String> row, String[] names)
  - private static Int32 ReadInt(String value, Int32 rowNo, String fieldName, Int32 defaultValue)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> Row(String name, Int32 axis, String displayName, String unit, Double min, Double max, Double maxVel, Double maxAcc)
  - private static Void Validate(Collections.Generic.IReadOnlyList<Drilling.Common.Motion.ST_MOTOR_DATA> motors)

### Drilling.File.IPS.CPowerMeterFile

- Kind: class
- Interfaces: Drilling.Common.Interface.IPowerMeterFile
- Constructors:
  - public CPowerMeterFile(String configRoot)
- Fields:
  - private static String DefaultProcessFile
  - private static readonly Collections.Generic.IReadOnlyList<String> Headers
  - private readonly String _powerMeterDirectory
- Methods:
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> CreateDefaultSteps()
  - private Void EnsureFiles()
  - private String GetFormPath()
  - private String GetProcessPath(String processFile)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<String>> List(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA> Load(String processFile = , Threading.CancellationToken cancellationToken = null)
  - private static String NormalizeProcessFile(String processFile)
  - private static Boolean ReadBool(String value, Boolean defaultValue)
  - private static Double ReadDouble(String value, Double defaultValue)
  - private static Int32 ReadInt(String value, Int32 defaultValue)
  - private static Nullable<Double> ReadNullableDouble(String value)
  - private Collections.Generic.List<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> ReadSteps(String path)
  - private static String ReadText(String value, String defaultValue)
  - public Threading.Tasks.Task Save(String processFile, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> steps, Threading.CancellationToken cancellationToken = null)
  - private static String SelectProcessFile(Collections.Generic.IReadOnlyList<String> files, String processFile)
  - private static Void WriteSteps(String path, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_POWER_METER_STEP_DATA> steps)

### Drilling.File.IPS.CSettingFile

- Kind: class
- Interfaces: Drilling.Common.Managers.ISettingFile
- Constructors:
  - public CSettingFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<String> FormHeaders
  - private static readonly Collections.Generic.IReadOnlyList<String> ValueHeaders
  - private readonly Drilling.Common.Log.CLogManager _logManager
  - private readonly String _settingDirectory
- Methods:
  - private static String CreateKey(String tab, String name)
  - private String GetFormPath()
  - private static String GetOrDefault(String value, String defaultValue)
  - private static String GetParameterKey(Drilling.Common.Managers.ST_SYSTEM_PARAMETER parameter)
  - private static String GetValue(Collections.Generic.IReadOnlyDictionary<String, String> values, String tab, String name, String defaultValue)
  - private String GetValuePath()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER>> Load(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - private Collections.Generic.IReadOnlyList<Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM> LoadFormItems()
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY>> LoadHistory(Drilling.Common.Managers.EN_SETTING_TAB section, Threading.CancellationToken cancellationToken = null)
  - private Collections.Generic.Dictionary<String, String> LoadSettingValues()
  - private static String NormalizeSettingText(String value, String defaultValue)
  - private static String NormalizeTab(String value)
  - private static Boolean ReadBool(String value, Boolean defaultValue)
  - private static Drilling.Common.Managers.EN_RECIPE_DATA_TYPE ReadDataType(String value)
  - private static Double ReadDouble(String value, Double defaultValue)
  - private static Int32 ReadInt(String value, Int32 defaultValue)
  - public Threading.Tasks.Task Save(Drilling.Common.Managers.EN_SETTING_TAB section, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER> parameters, Threading.CancellationToken cancellationToken = null)
  - private static String ToTabText(Drilling.Common.Managers.EN_SETTING_TAB section)
  - private static String ValidateBoolParameter(Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM formItem, String value)
  - private static String ValidateDoubleParameter(Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM formItem, String value)
  - private static String ValidateIntParameter(Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM formItem, String value)
  - private static String ValidateNumericRange(Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM formItem, Double value)
  - private Void ValidateSavedSection(Drilling.Common.Managers.EN_SETTING_TAB section, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER> expectedParameters)
  - private static Void ValidateSectionParameters(String sectionTab, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER> parameters, Collections.Generic.IReadOnlyList<Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM> formItems)
  - private Void WriteSettingValues(Collections.Generic.IReadOnlyList<Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM> formItems, Collections.Generic.IReadOnlyDictionary<String, String> values)

### Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM>
- Constructors:
  - public ST_MANUAL_FORM_ITEM(String Name, String DisplayName, Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType, String Unit, Boolean Show, Boolean Use, String DefaultValue, Double Min, Double Max, String Description, Int32 DisplayOrder)
  - private ST_MANUAL_FORM_ITEM(Drilling.File.IPS.CManualScanFile+ST_MANUAL_FORM_ITEM original)
- Properties:
  - public Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType (get/set)
  - public String DefaultValue (get/set)
  - public String Description (get/set)
  - public String DisplayName (get/set)
  - public Int32 DisplayOrder (get/set)
  - private Type EqualityContract (get)
  - public Double Max (get/set)
  - public Double Min (get/set)
  - public String Name (get/set)
  - public Boolean Show (get/set)
  - public String Unit (get/set)
  - public Boolean Use (get/set)

### Drilling.File.IPS.CConfigStructureFile+ST_REQUIRED_CSV

- Kind: class
- Interfaces: System.IEquatable<Drilling.File.IPS.CConfigStructureFile+ST_REQUIRED_CSV>
- Constructors:
  - public ST_REQUIRED_CSV(String ItemName, String RelativePath, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> RequiredHeaderGroups, Action<String, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>>> RowValidator)
  - private ST_REQUIRED_CSV(Drilling.File.IPS.CConfigStructureFile+ST_REQUIRED_CSV original)
- Properties:
  - private Type EqualityContract (get)
  - public String ItemName (get/set)
  - public String RelativePath (get/set)
  - public Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> RequiredHeaderGroups (get/set)
  - public Action<String, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>>> RowValidator (get/set)

### Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM>
- Constructors:
  - public ST_SETTING_FORM_ITEM(String Tab, String Group, String Name, String DisplayName, Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType, String Unit, Boolean Show, Boolean Use, String DefaultValue, Double Min, Double Max, String Description, Int32 DisplayOrder, Collections.Generic.IReadOnlyDictionary<String, String> Extra = null)
  - private ST_SETTING_FORM_ITEM(Drilling.File.IPS.CSettingFile+ST_SETTING_FORM_ITEM original)
- Properties:
  - public Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType (get/set)
  - public String DefaultValue (get/set)
  - public String Description (get/set)
  - public String DisplayName (get/set)
  - public Int32 DisplayOrder (get/set)
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Extra (get/set)
  - public String Group (get/set)
  - public Double Max (get/set)
  - public Double Min (get/set)
  - public String Name (get/set)
  - public Boolean Show (get/set)
  - public String Tab (get/set)
  - public String Unit (get/set)
  - public Boolean Use (get/set)

### Drilling.File.IPS.CConfigStructureFile+ST_VALUE_CSV

- Kind: class
- Interfaces: System.IEquatable<Drilling.File.IPS.CConfigStructureFile+ST_VALUE_CSV>
- Constructors:
  - public ST_VALUE_CSV(String ItemName, String RelativePath, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> RequiredHeaderGroups, Action<String, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>>> RowValidator)
  - private ST_VALUE_CSV(Drilling.File.IPS.CConfigStructureFile+ST_VALUE_CSV original)
- Properties:
  - private Type EqualityContract (get)
  - public String ItemName (get/set)
  - public String RelativePath (get/set)
  - public Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyList<String>> RequiredHeaderGroups (get/set)
  - public Action<String, Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>>> RowValidator (get/set)

### Drilling.File.Parser.CCsvParser

- Kind: static class
- Methods:
  - private static String Escape(String value)
  - public static String Get(Collections.Generic.IReadOnlyDictionary<String, String> row, String key)
  - public static Collections.Generic.IReadOnlyDictionary<String, String> GetExtra(Collections.Generic.IReadOnlyDictionary<String, String> row, Collections.Generic.IEnumerable<String> knownHeaders)
  - public static String GetFirst(Collections.Generic.IReadOnlyDictionary<String, String> row, String[] names)
  - private static String NormalizeHeader(String value)
  - private static Collections.Generic.List<String> ParseLine(String line, String path, Int32 lineNo)
  - public static Collections.Generic.IReadOnlyList<Collections.Generic.IReadOnlyDictionary<String, String>> Read(String path)
  - public static Boolean ReadBool(String value, Boolean defaultValue)
  - public static Double ReadDouble(String value, String tableName, Int32 rowNo, String fieldName, Double defaultValue)
  - public static Collections.Generic.IReadOnlyList<String> ReadHeaders(String path)
  - public static Int32 ReadInt(String value, String tableName, Int32 rowNo, String fieldName, Int32 defaultValue)
  - public static Boolean ReadRequiredBool(String value, String tableName, Int32 rowNo, String fieldName)
  - public static Int32 ReadRequiredInt(String value, String tableName, Int32 rowNo, String fieldName, Boolean allowNegative = False)
  - public static String RequireText(Collections.Generic.IReadOnlyDictionary<String, String> row, String tableName, Int32 rowNo, String[] names)
  - private static Void ValidateHeaders(String path, Collections.Generic.IReadOnlyList<String> headers)
  - public static Void ValidateRequiredHeaders(String path, String tableName, Collections.Generic.IEnumerable<Collections.Generic.IEnumerable<String>> requiredHeaderGroups)
  - public static Void Write(String path, Collections.Generic.IReadOnlyList<String> headers, Collections.Generic.IEnumerable<Collections.Generic.IReadOnlyDictionary<String, String>> rows)

### Drilling.File.Product.CProductFile

- Kind: class
- Interfaces: Drilling.Common.Product.IProductFile
- Constructors:
  - public CProductFile(String configRoot)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<String> ActiveHeaders
  - private static readonly Collections.Generic.IReadOnlyList<String> HistoryHeaders
  - private readonly String _productRoot
- Methods:
  - public Threading.Tasks.Task AppendHeadResults(Drilling.Common.Product.ST_PRODUCT_DATA product, Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task AppendHistory(Drilling.Common.Product.ST_PRODUCT_HISTORY history, Threading.CancellationToken cancellationToken = null)
  - private static Void AppendRow(String path, Collections.Generic.IReadOnlyList<String> headers, Collections.Generic.IReadOnlyDictionary<String, String> row)
  - public Threading.Tasks.Task ClearActive(Threading.CancellationToken cancellationToken = null)
  - private static Void DeleteIfExists(String path)
  - private static String FormatDate(Nullable<DateTimeOffset> value)
  - private String GetActiveProductPath()
  - private String GetHistoryPath(DateTimeOffset timestamp)
  - private static Boolean IsProductRow(Collections.Generic.IReadOnlyDictionary<String, String> row, String productId, String rowType)
  - public Threading.Tasks.Task<Drilling.Common.Product.ST_PRODUCT_DATA> LoadActive(Threading.CancellationToken cancellationToken = null)
  - public Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HISTORY>> LoadHistory(Int32 maxRows = 100, Int32 days = 14, Threading.CancellationToken cancellationToken = null)
  - private static Nullable<DateTimeOffset> ParseDate(String value)
  - private static T ParseEnum(String value, T defaultValue)
  - private static Drilling.Common.Product.ST_PRODUCT_HEAD_RESULT ParseHead(Collections.Generic.IReadOnlyDictionary<String, String> row)
  - private static Drilling.Common.Product.ST_PRODUCT_HISTORY ParseHistory(Collections.Generic.IReadOnlyDictionary<String, String> row)
  - private static Int32 ReadInt(Collections.Generic.IReadOnlyDictionary<String, String> row, String key)
  - private static String ReadProcessId(Collections.Generic.IReadOnlyDictionary<String, String> row)
  - public Threading.Tasks.Task SaveActive(Drilling.Common.Product.ST_PRODUCT_DATA product, Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> ToHeadResultRow(Drilling.Common.Product.ST_PRODUCT_DATA product, Drilling.Common.Product.ST_PRODUCT_HEAD_RESULT head)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> ToHeadRow(String productId, Drilling.Common.Product.ST_PRODUCT_HEAD_RESULT head)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> ToHistoryRow(Drilling.Common.Product.ST_PRODUCT_HISTORY history)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> ToParameterRow(String productId, String name, String value)
  - private static Collections.Generic.IReadOnlyDictionary<String, String> ToProductRow(Drilling.Common.Product.ST_PRODUCT_DATA product)

### Drilling.File.Script.CAutomation1ScriptFile+CAutomation1Script

- Kind: class
- Interfaces: Drilling.Common.Station.IAutomation1Script
- Constructors:
  - public CAutomation1Script(String scriptDirectory, String fileName)
- Fields:
  - private DateTimeOffset _createdAt
  - private Double _currentX
  - private Double _currentY
  - private Int32 _deviceNo
  - private Double _jumpSpeed
  - private String _laserAxis
  - private Double _laserOutputPeriod
  - private readonly Collections.Generic.List<String> _lines
  - private Double _markSpeed
  - private Boolean _nMarkDriveLaserControl
  - private Int32 _pointCount
  - private Boolean _scanPlannerStageEncoderMode
  - private readonly String _scriptDirectory
  - private String _stageXAxis
  - private String _stageYAxis
  - private Double _tactTime
  - private String _xAxis
  - private String _yAxis
- Properties:
  - public String FileName (get)
  - public String FilePath (get)
  - public Collections.Generic.IReadOnlyList<String> Lines (get)
- Methods:
  - public Void AddLine(String line)
  - private Void AddMoveTactTime(Double nextX, Double nextY, Double speed)
  - public Void Arc(Double startX, Double startY, Double endX, Double endY, Double centerX, Double centerY, Double angle)
  - public Void BufferedEnd()
  - public Void Clear()
  - public Void DeclareEncoderVariable(String axis = , Boolean useFeedback = False)
  - public Void DefaultSetting(Double scannerAcc = 500000, Int32 motionUpdateRate = 0, Int32 executeLineCount = 110, Boolean resetPso = True)
  - public Void DisableAxisPair()
  - public Void Dwell(Double delay)
  - public Void EnableAxisPair()
  - public Void EncoderNotFeedback(String axis)
  - public Void End(Boolean bufferedRun = False)
  - public Void FaultAckAxisPair()
  - public Void HomeAxisPair()
  - public Void InitDeclareVariable()
  - public Void InitDeclareVariableIFOV()
  - public Void InitEncoderCount(String galvoAxis)
  - private static Boolean IsUsableAxis(String axis)
  - public Void Jump(Double x, Double y)
  - public Void JumpLinear(Double x, Double y)
  - public Void JumpRel(Double x, Double y)
  - public Void LaserAuto()
  - public Void LaserFire(Boolean on)
  - public Void LaserOff()
  - public Void LaserOn()
  - public Void Mark(Double x, Double y)
  - public Void MarkRel(Double x, Double y)
  - private static String NormalizeAxis(String axis, String defaultAxis)
  - public Void OffsetClearAxisPair()
  - public Void OffsetSetAxisPair(Double x, Double y)
  - public Void ProgramEnd()
  - public Void ProgramStart()
  - public Void PsoLaserControl(Boolean on, Boolean manual = False)
  - public Void ReleaseEncoderScaleFactor(String galvoAxis)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_AUTOMATION1_SCRIPT> Save(Threading.CancellationToken cancellationToken = null)
  - public Void SetAbsoluteMode()
  - public Void SetAerotechEncoderReset(String axisX, String axisY)
  - public Void SetAxis(String xAxis, String yAxis, String laserAxis = null)
  - public Void SetCoordinatedAccelLimit(Int64 acc, Int64 arcAcc)
  - public Void SetDeviceNo(Int32 deviceNo)
  - public Void SetEmulatedQuadratureDividerX(Int32 value)
  - public Void SetEmulatedQuadratureDividerY(Int32 value)
  - public Void SetEncoderScaleFactor(String galvoAxis, String encoderAxis, Int32 scale)
  - public Void SetEncoderScaleFactor(String galvoAxis, String encoderAxis, Boolean directionPlus)
  - public Void SetEncoderScaleFactor(String galvoAxis, String encoderAxis, Double encoderX, Double encoderY, Boolean directionPlus)
  - public Void SetEncoderScaleFactorByPrimaryDivider(String galvoAxis, String encoderAxis, Boolean directionPlus)
  - public Void SetExecuteLineCount(Int32 lineCount)
  - public Void SetFrequency(Double frequencyKhz)
  - public Void SetGalvoPosZero()
  - public Void SetGearing(String masterAxis, String slaveAxis)
  - public Void SetGearingOff(String slaveAxis = AUTO)
  - public Void SetHomePos()
  - public Void SetIFOV(Boolean use)
  - public Void SetIFOVEmulatedQuadratureDivider()
  - public Void SetIFOVIO(Boolean use = True)
  - public Void SetIFOVPair(String xStageAxis, String yStageAxis, Boolean xDirection, Boolean yDirection)
  - public Void SetIFOVScaleXY()
  - public Void SetIFOVSize(Double size)
  - public Void SetIFOVSyncAxis()
  - public Void SetIFOVTime(Int64 time)
  - public Void SetIFOVTrackingAccel(Int64 acc)
  - public Void SetIFOVTrackingSpeed(Int64 speed)
  - public Void SetIncrementalMode()
  - public Void SetJumpSpeed(Double speedMmPerSec)
  - public Void SetJumpSpeedRate(Double speedMmPerSec, Double rate = 1)
  - public Void SetLaserDelay(Double onDelay, Double offDelay)
  - public Void SetLaserMode(Int32 mode)
  - public Void SetLaserPower(Double powerPercent, Double outputRate = 100, Boolean analogOutputUse = False)
  - public Void SetMarkAcc(Double acc)
  - public Void SetMarkSpeed(Double speedMmPerSec)
  - public Void SetMoveBlending(Boolean use)
  - public Void SetMoveDelay(Double delay, Boolean addTactTime = True)
  - public Void SetMoveUpdateRate(Int32 rate)
  - public Void SetNMarkDriveLaserControl(Boolean use)
  - public Void SetProjection(String axis, Double offsetX, Double offsetY, Double offsetT)
  - public Void SetProjectionOff(String axis)
  - public Void SetPSO(Double pulseDistance, Double totalTime, Double laserOnTime, Double delay, Drilling.Common.Station.EN_AEROTECH_MODE mode, Drilling.Common.Station.EN_AEROTECH_PSO_MODE psoMode, Double frequencyKhz, Double powerPercent, Int32 windowMaskDirection, Double markSpeed, Boolean manual = False)
  - public Void SetPSOChangePower(Double frequencyKhz, Double powerPercent)
  - public Void SetPSODistance(Double pulseDistance)
  - public Void SetPSOFire(Double totalTime, Double laserOnTime, Int32 count, Double delay, Drilling.Common.Station.EN_AEROTECH_MODE mode)
  - public Void SetPSOLaserWindowMask(Boolean on, Double windowStartRange = 0, Double windowEndRange = 0)
  - public Void SetPSOOnOff(Boolean on)
  - public Void SetPulseOnTimeLaserPower(Double powerPercent, Double dutyPercent, Double outputRate = 100)
  - public Void SetScannerAcc(Double acc)
  - public Void SetScannerRotate(Double angle)
  - public Void SetScannerRotate(String laserAxis, Double angle)
  - public Void SetScanPlannerStageEncoder(String stageAxis)
  - public Void SetScanPlannerStageEncoderMode(Boolean use)
  - public Void SetScanTrajectoryFIRFilterX(Int64 delay)
  - public Void SetScanTrajectoryFIRFilterY(Int64 delay)
  - public Void SetSignalLogTrigger(Boolean use)
  - public Void SetSoftwareLimitSetup(Boolean use = True)
  - public Void SetStageAxis(String xAxis, String yAxis)
  - public Void SetStageEmulatedQuadratureDivider(Int32 xValue, Int32 yValue)
  - public Void SetStageSpeed(Double speedX, Double speedY)
  - public Void SetStageTrajectoryFIRFilterX(Int64 delay)
  - public Void SetStageTrajectoryFIRFilterY(Int64 delay)
  - public Void SetTaskAccelLimit(Int64 acc, Int64 arcAcc)
  - public Void SetWaitForEncoder(String axis, Double position, Boolean directionPlus = True)
  - public Void SetWaitForEncoder(String axis, Boolean directionPlus, Double position, Double limit, Double encoderScale = 1)
  - public Void SetWaitForEncoder2Axis(String axisX, String axisY, Boolean inToOut, Double posX, Double posY, Double limitX, Double limitY)
  - public Void SetWaitForStartAxis2(String axisX, String axisY, Boolean inToOut, Double posX, Double posY, Double limitX, Double limitY)
  - public Void SetWaitModeAuto()
  - public Void Start(String title = )
  - public Void WaitInpos()
  - public Void WaitMoveDone()

### Drilling.File.Script.CAutomation1ScriptFile

- Kind: class
- Interfaces: Drilling.Common.Station.IAutomationScriptFile
- Constructors:
  - public CAutomation1ScriptFile(String scriptDirectory = null)
- Fields:
  - public static String DefaultScriptFileName
  - private readonly String _scriptDirectory
- Properties:
  - public String ScriptFileName (get)
- Methods:
  - private static Void AppendDefaultSetting(Collections.Generic.List<String> lines, Collections.Generic.IReadOnlyDictionary<String, String> parameters)
  - private static Void AppendHeadEnd(Collections.Generic.List<String> lines, Collections.Generic.IReadOnlyDictionary<String, String> common, Drilling.Common.Station.ST_HEAD_PROCESS_DATA head, String laserAxis)
  - private static Void AppendHeadProcess(Collections.Generic.List<String> lines, Collections.Generic.IReadOnlyDictionary<String, String> common, Drilling.Common.Station.ST_HEAD_PROCESS_DATA head)
  - private static Void AppendLaserSetting(Collections.Generic.List<String> lines, Collections.Generic.IReadOnlyDictionary<String, String> common, Drilling.Common.Station.ST_HEAD_PROCESS_DATA head, String laserAxis)
  - private static Void AppendPath(Collections.Generic.List<String> lines, Drilling.Common.Station.ST_HEAD_PROCESS_DATA head, String xAxis, String yAxis)
  - private static Void AppendPsoSetting(Collections.Generic.List<String> lines, Collections.Generic.IReadOnlyDictionary<String, String> common, Drilling.Common.Station.ST_HEAD_PROCESS_DATA head, String laserAxis)
  - private static Void AppendScriptEnd(Collections.Generic.List<String> lines, Collections.Generic.IReadOnlyDictionary<String, String> parameters)
  - private static Void AppendScriptStart(Collections.Generic.List<String> lines, Drilling.Common.Station.ST_PROCESS_MODEL processModel, DateTimeOffset createdAt)
  - private static Void AppendSpeedSetting(Collections.Generic.List<String> lines, Drilling.Common.Station.ST_HEAD_PROCESS_DATA head, String xAxis, String yAxis)
  - private static Drilling.Common.Station.ST_PATH_POINT ApplyOffset(Drilling.Common.Station.ST_PATH_POINT point, Drilling.Common.Station.ST_HEAD_PROCESS_DATA head)
  - public Threading.Tasks.Task<Drilling.Common.Station.ST_AUTOMATION1_SCRIPT> Build(Drilling.Common.Station.ST_PROCESS_MODEL processModel, Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<String> BuildLines(Drilling.Common.Station.ST_PROCESS_MODEL processModel, DateTimeOffset createdAt)
  - private static Double CalculateLaserPeriod(Double frequencyKhz)
  - public Drilling.Common.Station.IAutomation1Script Create(String fileName = null)
  - private static Collections.Generic.IReadOnlyList<String> CreateHeadKeys(Int32 headNo, String[] names)
  - private static String Format(Double value)
  - private static String NormalizeFileName(String fileName)
  - private static Boolean ReadBool(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Boolean defaultValue, String[] keys)
  - private static Boolean ReadBool(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Boolean defaultValue, Collections.Generic.IEnumerable<String> keys)
  - private static Double ReadDouble(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Double defaultValue, String[] keys)
  - private static Double ReadDouble(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Double defaultValue, Collections.Generic.IEnumerable<String> keys)
  - private static Int32 ReadInt(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Int32 defaultValue, String[] keys)
  - private static Int32 ReadInt(Collections.Generic.IReadOnlyDictionary<String, String> parameters, Int32 defaultValue, Collections.Generic.IEnumerable<String> keys)
  - private static String ReadText(Collections.Generic.IReadOnlyDictionary<String, String> parameters, String defaultValue, Collections.Generic.IEnumerable<String> keys)

## Drilling.UI

Type count: 78

### Drilling.UI.CApp

- Kind: class
- Base: System.Windows.Application
- Interfaces: System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient
- Constructors:
  - public CApp()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - public static Void Main()
  - protected Void OnStartup(Windows.StartupEventArgs e)
  - private Void RegisterExceptionHandlers()

### Drilling.UI.CAppStartup

- Kind: static class
- Methods:
  - public static Drilling.UI.CRootView CreateMainViewModel()
  - private static String GetConfigRoot()
  - private static String GetScriptDirectory(String configRoot)
  - private static Int32 WriteManagerStartupStatus(Drilling.Common.Managers.CManager manager, String title, Int32 afterOrder)

### Drilling.UI.CRootView

- Kind: class
- Base: Drilling.UI.Menu.CBindingBase
- Interfaces: System.ComponentModel.INotifyPropertyChanged
- Constructors:
  - public CRootView(Drilling.Common.Station.IStationManager stationManager, Drilling.Common.Interface.IInterfaceManager interfaceManager, Drilling.Common.Motion.IMotionManager motionManager, Drilling.Common.Alarm.CAlarmManager alarmManager, Drilling.Common.InterLock.CInterLockManager interLockManager, Drilling.Common.Managers.IManualScanFile manualScanFile, Drilling.Common.Managers.IRecipeManager recipeManager, Drilling.Common.Managers.ISettingManager settingManager, Drilling.Common.Product.IProductManager productManager)
- Fields:
  - private static readonly Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.EN_MENU> OperatorMenus
  - private readonly Drilling.Common.Alarm.CAlarmManager _alarmManager
  - private String _currentDateText
  - private Drilling.UI.Menu.Menus.CScreenViewModel _currentScreen
  - private String _currentTimeText
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS> _interfaceCommStatuses
  - private readonly Drilling.Common.Interface.IInterfaceManager _interfaceManager
  - private readonly Drilling.Common.InterLock.CInterLockManager _interLockManager
  - private readonly Collections.Generic.IReadOnlyDictionary<Drilling.UI.Menu.Menus.EN_MENU, Drilling.UI.Menu.Menus.IMenu> _menus
  - private readonly Drilling.Common.Motion.IMotionManager _motionManager
  - private Drilling.Common.Managers.ST_PM_LOCK_STATUS _pmLockStatus
  - private readonly Drilling.Common.Managers.IRecipeManager _recipeManager
  - private readonly Collections.Generic.Dictionary<Drilling.Common.Interface.EN_EQP_MODULE, Int32> _selectedHeaderModuleIndexes
  - private Int32 _selectedHeadNo
  - private String _selectedManualSettingName
  - private Drilling.UI.Menu.Menus.CMenuItem _selectedMenu
  - private String _selectedMonitorTab
  - private String _selectedRecipeCategory
  - private String _selectedRecipeId
  - private String _selectedSettingGroup
  - private String _selectedSettingTab
  - private readonly Drilling.Common.Station.IStationManager _stationManager
  - private String _statusMessage
  - private Boolean _statusRefreshRunning
  - private Drilling.Common.Managers.ST_SYSTEM_STATUS _systemStatus
- Properties:
  - private Boolean CanSelectHead (get)
  - public String CurrentDateText (get/set)
  - public Drilling.UI.Menu.Menus.CScreenViewModel CurrentScreen (get/set)
  - public String CurrentTimeText (get/set)
  - public String CurrentUserText (get)
  - public Collections.ObjectModel.ObservableCollection<Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM> FooterStatusItems (get)
  - public Collections.ObjectModel.ObservableCollection<Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM> HeaderStatusItems (get)
  - public Collections.ObjectModel.ObservableCollection<Drilling.UI.Menu.Menus.CMenuItem> Menus (get)
  - public Drilling.UI.Menu.Menus.CMenuItem SelectedMenu (get/set)
  - public Drilling.UI.Menu.CButtonCommand SelectHeadCommand (get)
  - public Drilling.UI.Menu.CButtonCommand StartCommand (get)
  - public String StatusMessage (get/set)
  - public Drilling.UI.Menu.CButtonCommand StopCommand (get)
- Methods:
  - private static String AlarmStateValue(Drilling.Common.Alarm.EN_ALARM_STATE state)
  - private static String ConnectionStateValue(Drilling.Common.Interface.EN_COMM_STATE state)
  - private static Drilling.Common.Managers.ST_SYSTEM_STATUS CreateFallbackST_SYSTEM_STATUS()
  - private Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM> CreateFooterStatusItems(Drilling.UI.Menu.Menus.EN_MENU menu)
  - private Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM> CreateHeaderStatusItems()
  - private static Drilling.UI.Menu.Menus.CScreenViewModel CreateLoadingScreen(Drilling.UI.Menu.Menus.EN_MENU menu, String title)
  - private Collections.Generic.IReadOnlyDictionary<Drilling.UI.Menu.Menus.EN_MENU, Drilling.UI.Menu.Menus.IMenu> CreateMenus(Drilling.Common.Station.IStationManager stationManager, Drilling.Common.Interface.IInterfaceManager interfaceManager, Drilling.Common.Motion.IMotionManager motionManager, Drilling.Common.Alarm.CAlarmManager alarmManager, Drilling.Common.InterLock.CInterLockManager interLockManager, Drilling.Common.Managers.IManualScanFile manualScanFile, Drilling.Common.Managers.IRecipeManager recipeManager, Drilling.Common.Managers.ISettingManager settingManager, Drilling.Common.Product.IProductManager productManager)
  - private Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM> CreateMonitorFooterStatusItems()
  - private static Collections.Generic.IReadOnlyDictionary<String, String> CreateProcessParameters(Drilling.Common.Managers.ST_RECIPE_DATA recipe)
  - private Void EnterPMLock()
  - private Drilling.Common.Managers.ST_RECIPE_DATA FindSelectedRecipe(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA> recipes)
  - private Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Alarm.ST_ALARM_DATA>> GetCurrentAlarms(Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task<Drilling.Common.Managers.ST_DEVICE_STATUS> GetDeviceStatus(Threading.CancellationToken cancellationToken)
  - private String GetHeaderRecipeId(Drilling.Common.Managers.ST_SYSTEM_STATUS status)
  - private static String GetMenuDisplayName(Drilling.UI.Menu.Menus.EN_MENU menu)
  - private Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS> GetModuleCommunicationStatuses(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private Drilling.Common.Managers.ST_PM_LOCK_STATUS GetPMLockStatus()
  - private Int32 GetSelectedModuleIndex(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 count)
  - private String GetSelectedRecipeFileText()
  - private Threading.Tasks.Task<Drilling.Common.Managers.ST_SYSTEM_STATUS> GetSystemStatus(Threading.CancellationToken cancellationToken = null)
  - private static String ModuleDisplayName(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM ModuleHeader(Drilling.Common.Managers.ST_SYSTEM_STATUS status, Drilling.Common.Interface.EN_EQP_MODULE module)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS> NormalizeCommunicationStatuses(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS> modules)
  - private static String NormalizeMonitorTab(String tab)
  - private static String OperationModeState(Drilling.Common.Managers.EN_SYSTEM_MODE mode)
  - private static String OperationModeValue(Drilling.Common.Managers.EN_SYSTEM_MODE mode)
  - private Threading.Tasks.Task PrepareInitialProcessPlan()
  - private Threading.Tasks.Task RefreshCurrentScreen()
  - private Void RefreshShellStatusItems()
  - private Threading.Tasks.Task RefreshSystemStatus()
  - private static Void Replace(Collections.ObjectModel.ObservableCollection<T> target, Collections.Generic.IEnumerable<T> items)
  - private Threading.Tasks.Task SelectHead(Object parameter)
  - private Void SelectModuleStatus(Drilling.Common.Interface.EN_EQP_MODULE module, Int32 offset)
  - private Void SelectNextModuleStatus(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private Void SelectPreviousModuleStatus(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private Void ShowModuleStatusPopup(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private Void StartClock()
  - private Threading.Tasks.Task StartCycle()
  - private Threading.Tasks.Task StopCycle()
  - private Void SyncSelectedManualSettingFromScreen()
  - private Void SyncSelectedRecipeIdFromScreen()
  - private Void SyncSelectedSettingSelectionFromScreen()

### Drilling.UI.Menu.CBindingBase

- Kind: class
- Interfaces: System.ComponentModel.INotifyPropertyChanged
- Constructors:
  - protected CBindingBase()
- Events:
  - ComponentModel.PropertyChangedEventHandler PropertyChanged
- Methods:
  - protected Void OnPropertyChanged(String propertyName = null)
  - protected Boolean SetProperty(T& field, T value, String propertyName = null)

### Drilling.UI.Menu.CButtonCommand

- Kind: class
- Interfaces: System.Windows.Input.ICommand
- Constructors:
  - public CButtonCommand(Action<Object> execute, Predicate<Object> canExecute = null)
- Properties:
  - public Drilling.UI.Menu.CButtonCommand NoOp (get)
- Events:
  - EventHandler CanExecuteChanged
- Methods:
  - public Boolean CanExecute(Object parameter)
  - public Void Execute(Object parameter)
  - public Void NotifyCanExecuteChanged()

### Drilling.UI.Menu.Menus.CMenuAlarm

- Kind: class
- Interfaces: Drilling.UI.Menu.Menus.IMenu
- Constructors:
  - public CMenuAlarm(Drilling.Common.Interface.IInterfaceManager interfaceManager, Drilling.Common.Motion.IMotionManager motionManager, Drilling.Common.Alarm.CAlarmManager alarmManager, Drilling.Common.InterLock.CInterLockManager interLockManager, Drilling.Common.Station.IStationManager stationManager, Action<String> setStatusMessage, Action refreshShellStatus, Func<Threading.Tasks.Task> refreshCurrentScreen)
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> AlarmDetail (get/set)
  - public Drilling.UI.Menu.CButtonCommand AlarmResetCommand (get)
  - public Drilling.UI.Menu.CButtonCommand BuzzerOffCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_CURRENT_ROW> CurrentAlarmRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> CurrentAlarms (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_DETAIL_ROW> DetailRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> History (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_HISTORY_ROW> HistoryRows (get/set)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> RecoveryCommands (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_SUMMARY_ITEM> SummaryItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> Trend (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_TREND_BAR> TrendBars (get/set)
- Methods:
  - private Void Apply(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> currentAlarms, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> alarmDetail, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> recoveryCommands, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> history, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> trend, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_CURRENT_ROW> currentAlarmRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_DETAIL_ROW> detailRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_HISTORY_ROW> historyRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_TREND_BAR> trendBars, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_SUMMARY_ITEM> summaryItems)
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_CURRENT_ROW> BuildCurrentAlarmRows(Collections.Generic.IReadOnlyList<Drilling.Common.Alarm.ST_ALARM_DATA> alarms)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_DETAIL_ROW> BuildDetailRows(Drilling.Common.Alarm.ST_ALARM_DATA alarm)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_HISTORY_ROW> BuildHistoryRows()
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_SUMMARY_ITEM> BuildSummaryItems(Collections.Generic.IReadOnlyList<Drilling.Common.Alarm.ST_ALARM_DATA> alarms)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_ALARM_TREND_BAR> BuildTrendBars()
  - private static String FormatSeverity(Drilling.Common.Alarm.EN_ALARM_LEVEL severity)
  - private Threading.Tasks.Task<Collections.Generic.IReadOnlyList<Drilling.Common.Alarm.ST_ALARM_DATA>> GetCurrentAlarms(Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task<Drilling.Common.Managers.ST_DEVICE_STATUS> GetDeviceStatus(Threading.CancellationToken cancellationToken)
  - private Threading.Tasks.Task ResetAlarm()
  - private static String SeverityState(Drilling.Common.Alarm.EN_ALARM_LEVEL severity)

### Drilling.UI.Menu.Menus.CMenuExit

- Kind: class
- Interfaces: Drilling.UI.Menu.Menus.IMenu
- Constructors:
  - public CMenuExit()
- Properties:
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
- Methods:
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)

### Drilling.UI.Menu.Menus.CMenuIcon

- Kind: static class
- Fields:
  - private static readonly Collections.Generic.IReadOnlyDictionary<Drilling.UI.Menu.Menus.EN_MENU, Windows.Media.Geometry> Icons
- Methods:
  - public static Windows.Media.Geometry Get(Drilling.UI.Menu.Menus.EN_MENU menu)
  - private static Windows.Media.Geometry Icon(String data)

### Drilling.UI.Menu.Menus.CMenuItem

- Kind: class
- Constructors:
  - public CMenuItem(Drilling.UI.Menu.Menus.EN_MENU menu, String name)
- Properties:
  - public String Description (get)
  - public Windows.Media.Geometry IconGeometry (get)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public String Name (get)

### Drilling.UI.Menu.Menus.CMenuMain

- Kind: class
- Interfaces: Drilling.UI.Menu.Menus.IMenu
- Constructors:
  - public CMenuMain(Drilling.Common.Station.IStationManager stationManager, Func<Int32> selectedHeadNoProvider, Drilling.UI.Menu.CButtonCommand selectHeadCommand)
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> CurrentStepDetails (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> CycleItems (get/set)
  - public String ElapsedTimeText (get/set)
  - public String EstimatedTimeText (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEAD_PARAMETER> HeadParameters (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEAD_PREVIEW> HeadPreviews (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_INTERFACE_LOG_ITEM> InterfaceLogs (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_INTERLOCK_ITEM> InterlockItems (get/set)
  - public String LaserOnSegmentsText (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> LifecycleItems (get/set)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public String MoveCountText (get/set)
  - public Windows.Media.Brush ProcessResultBrush (get/set)
  - public String ProcessResultValue (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> ProcessSequenceItems (get/set)
  - public String ProcessStep (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> ProcessSummaryItems (get/set)
  - public Double ProgressPercent (get/set)
  - public String ProgressText (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> ResultItems (get/set)
  - public String ResultMessage (get/set)
  - public String ScriptStatus (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> ScriptStatusItems (get/set)
  - public Drilling.UI.Menu.Menus.ST_HEAD_PREVIEW SelectedHeadPreview (get/set)
  - public Drilling.UI.Menu.CButtonCommand SelectHeadCommand (get/set)
  - public String TotalPointsText (get/set)
- Methods:
  - private Void Apply(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEAD_PREVIEW> headPreviews, Drilling.UI.Menu.Menus.ST_HEAD_PREVIEW selectedHeadPreview, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> cycleItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> resultItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> processSequenceItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> currentStepDetails, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> processSummaryItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_INTERFACE_LOG_ITEM> interfaceLogs, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> scriptStatusItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> lifecycleItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_INTERLOCK_ITEM> interlockItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEAD_PARAMETER> headParameters, String processStep, String scriptStatus, String resultMessage, Drilling.Common.Station.ST_PROCESS_STATISTICS statistics, String processResultValue, Drilling.UI.Menu.CButtonCommand selectHeadCommand)
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_HEAD_PARAMETER> BuildHeadParameters()
  - private static Drilling.UI.Menu.Menus.ST_HEAD_PREVIEW BuildHeadPreviewItem(Drilling.Common.Station.ST_HEAD_PATH_DATA head, Int32 selectedHeadNo)
  - private static Windows.Media.Geometry BuildPreviewGeometry(Collections.Generic.IReadOnlyList<Drilling.Common.Station.ST_PATH_POINT> points)
  - private static String FormatDuration(TimeSpan value)
  - private static String FormatInterLockState(Drilling.Common.InterLock.ST_INTERLOCK_ITEM item)
  - private static String FormatProcessResult(Drilling.Common.Station.ST_STATION_PROCESS_STATUS snapshot)
  - public static String FormatScriptStatus(Drilling.Common.Station.EN_SCRIPT_STATUS status)
  - private static Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM ToDisplayItem(Drilling.Common.Station.ST_PROCESS_DISPLAY_ITEM item)
  - private static Drilling.UI.Menu.Menus.ST_INTERFACE_LOG_ITEM ToInterfaceLogItem(Drilling.Common.Station.ST_PROCESS_LOG_ITEM item)
  - private static Drilling.UI.Menu.Menus.ST_INTERLOCK_ITEM ToInterlockItem(Drilling.Common.InterLock.ST_INTERLOCK_ITEM item)

### Drilling.UI.Menu.Menus.CMenuManual

- Kind: class
- Interfaces: Drilling.UI.Menu.Menus.IMenu
- Constructors:
  - public CMenuManual(Drilling.Common.Managers.IManualScanFile scanFile, Func<Int32> selectedHeadNoProvider, Func<String> selectedSettingNameProvider, Action<String> selectedSettingNameSetter, Drilling.UI.Menu.CButtonCommand selectHeadCommand, Action<String> setStatusMessage, Action refreshShellStatus, Func<Threading.Tasks.Task> refreshCurrentScreen)
- Fields:
  - private readonly Func<Threading.Tasks.Task> _refreshCurrentScreen
  - private readonly Action _refreshShellStatus
  - private readonly Drilling.Common.Managers.IManualScanFile _scanFile
  - private readonly Func<Int32> _selectedHeadNoProvider
  - private readonly Func<String> _selectedSettingNameProvider
  - private readonly Action<String> _selectedSettingNameSetter
  - private readonly Action<String> _setStatusMessage
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> CommandStateItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_COMMAND_STATE> CommandStateRows (get/set)
  - public Drilling.UI.Menu.CButtonCommand CreateCommand (get)
  - public Drilling.UI.Menu.CButtonCommand DeleteCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_HEAD_CARD> HeadCards (get/set)
  - public String LoadedSettingName (get/set)
  - public String LoadedSettingPath (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> ManualSettings (get/set)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> PositionMoveItems (get/set)
  - public Drilling.UI.Menu.CButtonCommand RenameCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SaveCommand (get)
  - public String SelectedHead (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> SelectedHeadItems (get/set)
  - public Drilling.UI.Menu.CButtonCommand SelectHeadCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SelectSettingCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_SETTING_FILE> SettingFiles (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_PARAMETER> SettingParameters (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> ShapeScanItems (get/set)
- Methods:
  - private Void Apply(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> manualSettings, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> selectedHeadItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> positionMoveItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> shapeScanItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> commandStateItems, String selectedHead, String loadedSettingName, String loadedSettingPath, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_HEAD_CARD> headCards, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_SETTING_FILE> settingFiles, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_PARAMETER> settingParameters, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_COMMAND_STATE> commandStateRows)
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_COMMAND_STATE> BuildCommandStateRows(Drilling.UI.Menu.Menus.ST_MANUAL_HEAD_CARD selectedHead, Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM settings)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_HEAD_CARD> BuildHeadCards(Int32 selectedHeadNo)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_SETTING_FILE> BuildSettingFiles(Collections.Generic.IReadOnlyList<String> settingNames, String selectedSettingName)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MANUAL_PARAMETER> BuildSettingParameters(Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM settings)
  - private static Boolean ConfirmManualSettingDelete(String settingName)
  - private Threading.Tasks.Task Create()
  - private static Drilling.Common.Managers.ST_MANUAL_SCAN_PARAM CreateManualScanParamFromScreen(Drilling.UI.Menu.Menus.CMenuManual manualScreen)
  - private Threading.Tasks.Task Delete()
  - private static Windows.Window GetActiveWindow()
  - private static String GetManualSettingNameFromParameter(Object parameter)
  - private static Drilling.UI.Menu.Menus.ST_MANUAL_HEAD_CARD Head(Int32 headNo, String gx, String gy, String state, Int32 selectedHeadNo)
  - private static String NormalizeManualSettingNameInput(String value)
  - private static String NormalizeSettingName(String settingName)
  - private static Double ReadManualDouble(Drilling.UI.Menu.Menus.CMenuManual manualScreen, String parameterName, Double defaultValue)
  - private static String ReadManualValue(Drilling.UI.Menu.Menus.CMenuManual manualScreen, String parameterName, String defaultValue)
  - private Threading.Tasks.Task Rename()
  - private static String ResolveSelectedSettingName(Collections.Generic.IReadOnlyList<String> settingNames, String selectedSettingName)
  - private Threading.Tasks.Task Save()
  - private Threading.Tasks.Task SelectSetting(Object parameter)
  - private static String ShowManualSettingNameDialog(String title, String message, String initialValue, Func<String, String> validate = null)
  - private static String ValidateManualSettingName(String settingName, Collections.Generic.IReadOnlyList<String> settingNames, String currentSettingName = )

### Drilling.UI.Menu.Menus.CMenuMonitor

- Kind: class
- Interfaces: Drilling.UI.Menu.Menus.IMenu
- Constructors:
  - public CMenuMonitor(Drilling.Common.Interface.IInterfaceManager interfaceManager, Drilling.Common.Motion.IMotionManager motionManager, Drilling.Common.InterLock.CInterLockManager interLockManager, Drilling.Common.Product.IProductManager productManager, Func<String> selectedTabAccessor, Action<String> selectedTabSetter, Action<String> setStatusMessage, Action refreshShellStatus, Func<Threading.Tasks.Task> refreshCurrentScreen)
- Fields:
  - private static readonly String[] MonitorTabs
  - private readonly Drilling.Common.Interface.IInterfaceManager _interfaceManager
  - private readonly Drilling.Common.InterLock.CInterLockManager _interLockManager
  - private readonly Drilling.Common.Motion.IMotionManager _motionManager
  - private readonly Drilling.Common.Product.IProductManager _productManager
  - private readonly Func<Threading.Tasks.Task> _refreshCurrentScreen
  - private readonly Action _refreshShellStatus
  - private String _selectedAxisId
  - private String _selectedPowerMeterProcessName
  - private readonly Func<String> _selectedTabAccessor
  - private readonly Action<String> _selectedTabSetter
  - private readonly Action<String> _setStatusMessage
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_AXIS_ROW> AxisRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_BET_TABLE_ROW> BetTableRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_COMMAND_HISTORY_ROW> CommandHistoryRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION> DeviceTabs (get/set)
  - public Drilling.UI.Menu.CButtonCommand ExecuteOperationCommand (get)
  - public String HistoryPanelTitle (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW> InputRows (get/set)
  - public Boolean IsAttenuator (get)
  - public Boolean IsBet (get)
  - public Boolean IsChiller (get)
  - public Boolean IsControlDevice (get)
  - public Boolean IsGenericDevice (get)
  - public Boolean IsIo (get)
  - public Boolean IsLaser (get)
  - public Boolean IsMotor (get)
  - public Boolean IsPowerMeter (get)
  - public Boolean IsProduct (get)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> OperationButtons (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW> OperationFields (get/set)
  - public String OperationPanelTitle (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW> OutputRows (get/set)
  - public String ParameterPanelTitle (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW> ParameterRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_POSITION_ROW> PositionRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HEAD_ROW> ProductHeadRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HISTORY_ROW> ProductHistoryRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_ITEM> ProductItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_DEVICE_ROW> PwmDeviceRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> PwmProcessButtons (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_PROCESS_ROW> PwmProcessRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> PwmRunButtons (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_SETTING_ROW> PwmSettingRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> PwmStepButtons (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_STEP_ROW> PwmStepRows (get/set)
  - public String SelectedAxisId (get)
  - public Drilling.UI.Menu.Menus.ST_MONITOR_AXIS_ROW SelectedAxisRow (get/set)
  - public String SelectedTab (get/set)
  - public Drilling.UI.Menu.CButtonCommand SelectTabCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SetOutputOffCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SetOutputOnCommand (get)
  - public String StatusPanelTitle (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_STATUS_ROW> StatusRows (get/set)
  - public String Subtitle (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_SUMMARY_ITEM> SummaryItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_TAB> Tabs (get/set)
  - public String Title (get/set)
  - public String TrendPanelTitle (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_TREND_POINT> TrendPoints (get/set)
- Methods:
  - private Void Apply(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION> deviceTabs, String selectedTab, String title, String subtitle, String statusPanelTitle, String operationPanelTitle, String parameterPanelTitle, String trendPanelTitle, String historyPanelTitle, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_TAB> tabs, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW> inputRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW> outputRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_AXIS_ROW> axisRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_COMMAND_HISTORY_ROW> commandHistoryRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_STATUS_ROW> statusRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> operationButtons, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW> operationFields, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW> parameterRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_BET_TABLE_ROW> betTableRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_TREND_POINT> trendPoints, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_SUMMARY_ITEM> summaryItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_POSITION_ROW> positionRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_ITEM> productItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HEAD_ROW> productHeadRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HISTORY_ROW> productHistoryRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_PROCESS_ROW> pwmProcessRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_STEP_ROW> pwmStepRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_SETTING_ROW> pwmSettingRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_DEVICE_ROW> pwmDeviceRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> pwmProcessButtons, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> pwmStepButtons, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> pwmRunButtons)
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_AXIS_ROW> CreateAxisRows(Drilling.Common.Managers.ST_DEVICE_STATUS snapshot, String selectedAxisId)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_BET_TABLE_ROW> CreateBetTableRows(String tab, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_BET_TABLE_DATA> table, Drilling.Common.Managers.ST_DEVICE_STATUS snapshot)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_COMMAND_HISTORY_ROW> CreateCommandHistoryRows(String tab, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_HISTORY> interfaceHistory)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW> CreateInputRows(Drilling.Common.Managers.ST_DEVICE_STATUS snapshot)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION> CreateLegacyTabs(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_TAB> tabs)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> CreateOperationButtons(String tab)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW> CreateOperationFields(String tab)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW> CreateOutputRows(Drilling.Common.Managers.ST_DEVICE_STATUS snapshot)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW> CreateParameterRows(String tab)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_POSITION_ROW> CreatePositionRows(String tab, Drilling.Common.Managers.ST_DEVICE_STATUS snapshot)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HEAD_ROW> CreateProductHeadRows(Drilling.Common.Product.ST_PRODUCT_DATA product)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HISTORY_ROW> CreateProductHistoryRows(Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HISTORY> history)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_ITEM> CreateProductItems(Drilling.Common.Product.ST_PRODUCT_DATA product, String error)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_DEVICE_ROW> CreatePwmDeviceRows(String tab, Drilling.Common.Managers.ST_DEVICE_STATUS snapshot)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> CreatePwmProcessButtons(String tab)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_PROCESS_ROW> CreatePwmProcessRows(String tab, Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA table)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> CreatePwmRunButtons(String tab)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_SETTING_ROW> CreatePwmSettingRows(String tab, Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA table)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON> CreatePwmStepButtons(String tab)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_PWM_STEP_ROW> CreatePwmStepRows(String tab, Drilling.Common.Managers.ST_DEVICE_STATUS snapshot, Drilling.Common.Interface.ST_POWER_METER_TABLE_DATA table)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_STATUS_ROW> CreateStatusRows(String tab, Drilling.Common.Managers.ST_DEVICE_STATUS snapshot, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS> communication)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_SUMMARY_ITEM> CreateSummaryItems(String tab, Drilling.Common.Managers.ST_DEVICE_STATUS snapshot)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_MONITOR_TREND_POINT> CreateTrendPoints(String tab)
  - private Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteAttenuatorOperation(String label)
  - private Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteBETOperation(String label)
  - private Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteChillerOperation(String label)
  - private Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteLaserOperation(String label)
  - private Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecuteMotorOperation(String label)
  - private Threading.Tasks.Task ExecuteOperation(Object parameter)
  - private Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> ExecutePowerMeterOperation(String label)
  - private static String FormatAxisPosition(String axisId, Double value)
  - private static String FormatInterfaceHistoryCommand(Drilling.Common.Interface.ST_INTERFACE_HISTORY item)
  - private static String FormatInterfaceHistoryResult(Drilling.Common.Interface.ST_INTERFACE_HISTORY item)
  - private static String FormatInterfaceHistoryTarget(Drilling.Common.Interface.ST_INTERFACE_HISTORY item)
  - private static String FormatProductDateTime(Nullable<DateTimeOffset> value)
  - private Threading.Tasks.Task<Drilling.Common.Managers.ST_DEVICE_STATUS> GetDeviceStatus(Threading.CancellationToken cancellationToken)
  - private static String GetHistoryPanelTitle(String tab)
  - private static Drilling.Common.Interface.EN_COMM_STATE GetModuleState(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_DEVICE_COMM_STATUS> communication, Drilling.Common.Interface.EN_EQP_MODULE module)
  - private static Nullable<Drilling.Common.Interface.EN_EQP_MODULE> GetMonitorModule(String tab)
  - private static String GetMonitorOperationLabel(Object parameter)
  - private static String GetOperationPanelTitle(String tab)
  - private static String GetParameterPanelTitle(String tab)
  - private static String GetStatusPanelTitle(String tab)
  - private static String GetSubtitle(String tab)
  - private static String GetTrendPanelTitle(String tab)
  - private static Boolean IsInPosition(Double current, Double target)
  - private Threading.Tasks.Task<ValueTuple<Drilling.Common.Product.ST_PRODUCT_DATA, Collections.Generic.IReadOnlyList<Drilling.Common.Product.ST_PRODUCT_HISTORY>, String>> LoadProductDisplay(Threading.CancellationToken cancellationToken)
  - private static String NormalizeMonitorOperation(String label)
  - private static String NormalizeMonitorTab(String tab)
  - private static String OnOffText(Boolean value)
  - private static String ProductResultTone(Drilling.Common.Product.EN_PRODUCT_RESULT result)
  - private static String ProductStateTone(Drilling.Common.Product.EN_PRODUCT_STATE state, Drilling.Common.Product.EN_PRODUCT_RESULT result)
  - private Double ReadOperationField(String parameter, Double defaultValue)
  - private Double ReadPwmSetting(String parameter, Double defaultValue)
  - private Threading.Tasks.Task<Drilling.Common.Interface.ST_DEVICE_COMMAND_RESULT> RunPowerMeterMeasureSequence()
  - private Threading.Tasks.Task SelectTab(Object parameter)
  - private Threading.Tasks.Task SetOutput(Object parameter, Boolean isOn)
  - private static String ToCommunicationText(Drilling.Common.Interface.EN_COMM_STATE state)

### Drilling.UI.Menu.Menus.CMenuPm

- Kind: class
- Interfaces: Drilling.UI.Menu.Menus.IMenu
- Constructors:
  - public CMenuPm(Func<Drilling.Common.Managers.ST_PM_LOCK_STATUS> lockStatusProvider, Action enterLock)
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> BlockedItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> LockItems (get/set)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public String StartTime (get/set)
- Methods:
  - private Void Apply(String startTime, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> lockItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> blockedItems)
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)

### Drilling.UI.Menu.Menus.CMenuRecipe

- Kind: class
- Interfaces: Drilling.UI.Menu.Menus.IMenu
- Constructors:
  - public CMenuRecipe(Drilling.Common.Managers.IRecipeManager recipeManager, Func<String> selectedRecipeIdProvider, Action<String> selectedRecipeIdSetter, Func<String> selectedCategoryProvider, Action<String> selectedCategorySetter, Func<Drilling.UI.Menu.Menus.CMenuRecipe> editScreenProvider, Action<String> setStatusMessage, Action<Drilling.UI.Menu.Menus.EN_MENU, String> showLoadingScreen, Action refreshShellStatus, Func<Threading.Tasks.Task> refreshCurrentScreen)
- Fields:
  - private readonly Func<Drilling.UI.Menu.Menus.CMenuRecipe> _editScreenProvider
  - private readonly Drilling.Common.Managers.IRecipeManager _recipeManager
  - private readonly Func<Threading.Tasks.Task> _refreshCurrentScreen
  - private readonly Action _refreshShellStatus
  - private readonly Func<String> _selectedCategoryProvider
  - private readonly Action<String> _selectedCategorySetter
  - private String _selectedGroup
  - private readonly Func<String> _selectedRecipeIdProvider
  - private readonly Action<String> _selectedRecipeIdSetter
  - private readonly Action<String> _setStatusMessage
  - private readonly Action<Drilling.UI.Menu.Menus.EN_MENU, String> _showLoadingScreen
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> Actions (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> AllManagedItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_HISTORY_ROW> ChangeHistory (get/set)
  - public Drilling.UI.Menu.CButtonCommand CreateCommand (get)
  - public Drilling.UI.Menu.CButtonCommand DeleteCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_GROUP_TAB> GroupTabs (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> History (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_CATEGORY_TAB> ItemTabs (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> ManagedItems (get/set)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public Drilling.UI.Menu.CButtonCommand ModifyCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> Parameters (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_FILE> RecipeFiles (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> RecipeList (get/set)
  - public Drilling.UI.Menu.CButtonCommand SaveCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SelectCategoryCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SelectCommand (get)
  - public String SelectedGroup (get/set)
  - public String SelectedRecipeFile (get/set)
  - public Drilling.UI.Menu.CButtonCommand SelectGroupCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_STATE_ROW> StateRows (get/set)
- Methods:
  - private Void Apply(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> recipeList, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> parameters, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> history, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> actions, String selectedRecipeFile, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_FILE> recipeFiles, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_CATEGORY_TAB> itemTabs, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_GROUP_TAB> groupTabs, String selectedGroup, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> allManagedItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> managedItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_HISTORY_ROW> changeHistory, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_STATE_ROW> stateRows)
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> BuildActions()
  - private static Collections.Generic.IReadOnlyList<String> BuildCategories(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> managedItems)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_CATEGORY_TAB> BuildCategoryTabs(Collections.Generic.IReadOnlyList<String> categories, String selectedCategory)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_HISTORY_ROW> BuildChangeHistory(Drilling.Common.Managers.ST_RECIPE_DATA recipe)
  - private static Collections.Generic.IReadOnlyList<String> BuildGroups(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> managedItems)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_GROUP_TAB> BuildGroupTabs(Collections.Generic.IReadOnlyList<String> groups, String selectedGroup)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> BuildManagedItems(Drilling.Common.Managers.ST_RECIPE_DATA recipe)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_FILE> BuildRecipeFiles(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA> recipes, Drilling.Common.Managers.ST_RECIPE_DATA selectedRecipe)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_STATE_ROW> BuildStateRows(Drilling.Common.Managers.ST_RECIPE_DATA recipe, String selectedRecipeFile, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> managedItems)
  - private static Boolean ConfirmRecipeDelete(String recipeId)
  - private Threading.Tasks.Task Create()
  - private static Drilling.Common.Managers.ST_RECIPE_PARAM CreateRecipeParameterFromRow(Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM item, String recipeId)
  - private Threading.Tasks.Task Delete()
  - private static Windows.Window GetActiveWindow()
  - private static String GetEditedRecipeName(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_PARAM> parameters, String fallbackRecipeId)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> GetEditItems(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM> loadedItems, Drilling.UI.Menu.Menus.CMenuRecipe editScreen, String selectedRecipeFile)
  - private static String GetRecipeFileName(Drilling.Common.Managers.ST_RECIPE_DATA recipe)
  - private static String GetRecipeIdFromParameter(Object parameter)
  - private static Drilling.Common.Managers.ST_RECIPE_DATA GetSelectedRecipe(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA> recipes, String selectedRecipeId)
  - private static String GetValueState(Drilling.Common.Managers.ST_RECIPE_PARAM parameter)
  - private static Boolean IsModified(Drilling.Common.Managers.ST_RECIPE_PARAM parameter)
  - private static Boolean IsOnOffValue(String value)
  - private Threading.Tasks.Task Modify()
  - private static String NormalizeCategory(String category, Collections.Generic.IReadOnlyList<String> categories)
  - private static String NormalizeGroup(String group, Collections.Generic.IReadOnlyList<String> groups)
  - private static String NormalizeRecipeIdInput(String value)
  - private static String NormalizeRecipeText(String value, String defaultValue)
  - private static String NormalizeRecipeUnit(String unit)
  - private static String NormalizeRecipeValue(String value)
  - private static String NormalizeUnit(String unit)
  - private Void NotifyCommands()
  - private Threading.Tasks.Task Save()
  - private Threading.Tasks.Task Select(Object parameter)
  - private Threading.Tasks.Task SelectCategory(Object parameter)
  - private Threading.Tasks.Task SelectGroup(Object parameter)
  - private static String ShowRecipeNameDialog(String title, String message, String initialValue, Func<String, String> validate = null)
  - private static String ValidateBoolParameter(Drilling.Common.Managers.ST_RECIPE_PARAM parameter, String value)
  - private static String ValidateDoubleParameter(Drilling.Common.Managers.ST_RECIPE_PARAM parameter, String value)
  - private static String ValidateIntParameter(Drilling.Common.Managers.ST_RECIPE_PARAM parameter, String value)
  - private static String ValidateNumericRange(Drilling.Common.Managers.ST_RECIPE_PARAM parameter, Double value)
  - private static String ValidateRecipeId(String recipeId, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_DATA> recipes, String currentRecipeId = )
  - private static String ValidateRecipeParameters(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_RECIPE_PARAM> parameters)

### Drilling.UI.Menu.Menus.CMenuSetting

- Kind: class
- Base: Drilling.UI.Menu.CBindingBase
- Interfaces: Drilling.UI.Menu.Menus.IMenu, System.ComponentModel.INotifyPropertyChanged
- Constructors:
  - public CMenuSetting(Drilling.Common.Managers.ISettingManager settingManager, Func<String> selectedTabProvider, Action<String> selectedTabSetter, Func<String> selectedGroupProvider, Action<String> selectedGroupSetter, Func<Drilling.UI.Menu.Menus.CMenuSetting> editScreenProvider, Action<String> setStatusMessage, Action<Drilling.UI.Menu.Menus.EN_MENU, String> showLoadingScreen, Action refreshShellStatus, Func<Threading.Tasks.Task> refreshCurrentScreen)
- Fields:
  - private static readonly String[] InterfaceHistoryFields
  - private static readonly Drilling.Common.Managers.EN_SETTING_TAB[] Sections
  - private readonly Func<Drilling.UI.Menu.Menus.CMenuSetting> _editScreenProvider
  - private readonly Func<Threading.Tasks.Task> _refreshCurrentScreen
  - private readonly Action _refreshShellStatus
  - private readonly Func<String> _selectedGroupProvider
  - private readonly Action<String> _selectedGroupSetter
  - private Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW _selectedInterfaceRow
  - private readonly Func<String> _selectedTabProvider
  - private readonly Action<String> _selectedTabSetter
  - private readonly Action<String> _setStatusMessage
  - private readonly Drilling.Common.Managers.ISettingManager _settingManager
  - private readonly Action<Drilling.UI.Menu.Menus.EN_MENU, String> _showLoadingScreen
- Properties:
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> AllInterfaceRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> AllParameterRows (get/set)
  - public Drilling.UI.Menu.CButtonCommand CancelCommand (get)
  - public Boolean CanOperateSelectedInterface (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_HISTORY_ROW> ChangeHistory (get/set)
  - public Drilling.UI.Menu.CButtonCommand ConnectInterfaceCommand (get)
  - public Drilling.UI.Menu.CButtonCommand DisconnectInterfaceCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_GROUP> GroupItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> History (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> InterfaceRows (get/set)
  - public Boolean IsInterfaceTab (get)
  - public Boolean IsParameterTab (get)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> ParameterRows (get/set)
  - public Drilling.UI.Menu.CButtonCommand ReloadCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SaveCommand (get)
  - public String SelectedGroup (get/set)
  - public Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW SelectedInterfaceRow (get/set)
  - public String SelectedTab (get/set)
  - public Drilling.UI.Menu.CButtonCommand SelectGroupCommand (get)
  - public Drilling.UI.Menu.CButtonCommand SelectTabCommand (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_SUMMARY_ROW> SummaryRows (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_TAB> TabItems (get/set)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION> Tabs (get/set)
- Methods:
  - private Void Apply(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION> tabs, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> history, String selectedTab, String selectedGroup, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_TAB> tabItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_GROUP> groupItems, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> allParameterRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> parameterRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> allInterfaceRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> interfaceRows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_HISTORY_ROW> changeHistory, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_SUMMARY_ROW> summaryRows)
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)
  - private static Collections.Generic.IReadOnlyList<String> BuildGroups(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> rows)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> BuildHistoryItems(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY> history)
  - private static Collections.Generic.IEnumerable<Drilling.UI.Menu.Menus.ST_SETTING_HISTORY_ROW> BuildHistoryRow(Drilling.Common.Managers.ST_SETTING_HISTORY item)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_HISTORY_ROW> BuildHistoryRows(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY> history)
  - private static Collections.Generic.IReadOnlyList<String> BuildInterfaceGroups(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> rows)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> BuildInterfaceRows(Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> interfaces)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> BuildParameterRows(Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SYSTEM_PARAMETER> parameters)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_SUMMARY_ROW> BuildSummaryRows(String selectedTab, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> rows, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> interfaceRows, Collections.Generic.IReadOnlyList<Drilling.Common.Managers.ST_SETTING_HISTORY> history)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_TAB> BuildTabs(String selectedTab)
  - private Threading.Tasks.Task Cancel()
  - private Threading.Tasks.Task ConnectInterface()
  - private static String DeviceText(Drilling.Common.Interface.EN_EQP_MODULE module)
  - private Threading.Tasks.Task DisconnectInterface()
  - private static String GetDefaultGroup()
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> GetEditInterfaceRows(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> loadedRows, Drilling.UI.Menu.Menus.CMenuSetting editScreen, String selectedTab)
  - private static Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> GetEditRows(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW> loadedRows, Drilling.UI.Menu.Menus.CMenuSetting editScreen, String selectedTab)
  - private static Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW GetSelectedInterfaceRow(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> rows, Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW current)
  - private static String GetValueState(String value)
  - private static String InterfaceRowLabel(Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW row)
  - private static String InterfaceTypeText(Drilling.Common.Interface.EN_INTERFACE_TYPE type)
  - private static Boolean IsSameInterfaceKey(Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW left, Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW right)
  - private static String NormalizeGroup(String group, Collections.Generic.IReadOnlyList<String> groups)
  - private static String NormalizeSettingText(String value, String defaultValue)
  - private static String NormalizeSettingUnit(String unit)
  - private static String NormalizeTab(String tab)
  - private static String NormalizeUnit(String unit)
  - private static Drilling.Common.Interface.EN_EQP_MODULE ParseDevice(String value)
  - private static Drilling.Common.Interface.EN_INTERFACE_TYPE ParseInterfaceType(String value)
  - private static Boolean ReadBool(String value, String nickName, String fieldName)
  - private static Int32 ReadInt(String value, String nickName, String fieldName)
  - private Void RefreshInterfaceCommandState()
  - private Threading.Tasks.Task Reload()
  - private static String RequireText(String value, String fieldName)
  - private Threading.Tasks.Task Save()
  - private Void SelectedInterfaceRowChanged(Object sender, ComponentModel.PropertyChangedEventArgs e)
  - private Threading.Tasks.Task SelectGroup(Object parameter)
  - private Threading.Tasks.Task SelectTab(Object parameter)
  - private static Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_DATA> ToInterfaceData(Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW> rows)
  - private static Drilling.Common.Managers.EN_SETTING_TAB ToSection(String tab)
  - private static String ToTabText(Drilling.Common.Managers.EN_SETTING_TAB section)
  - private static Boolean TryBuildLegacyInterfaceHistoryRows(Drilling.Common.Managers.ST_SETTING_HISTORY item, Collections.Generic.IReadOnlyList`1[[Drilling.UI.Menu.Menus.ST_SETTING_HISTORY_ROW, Drilling.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]& rows)
  - private static Boolean TrySplitLegacyInterfaceValue(String value, String[]& fields)

### Drilling.UI.Menu.Menus.CMonitorIcon

- Kind: static class
- Fields:
  - private static readonly Collections.Generic.IReadOnlyDictionary<String, Windows.Media.Geometry> Icons
- Methods:
  - public static Windows.Media.Geometry Get(String icon)
  - private static Windows.Media.Geometry Icon(String data)

### Drilling.UI.Menu.Menus.CScreenViewModel

- Kind: class
- Constructors:
  - public CScreenViewModel(Drilling.UI.Menu.Menus.EN_MENU menu, String title, String subtitle, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> metrics, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION> sections, Boolean showCycleControls = False, Drilling.UI.Menu.Menus.CMenuMain mainOperating = null, Drilling.UI.Menu.Menus.CMenuManual manual = null, Drilling.UI.Menu.Menus.CMenuRecipe recipe = null, Drilling.UI.Menu.Menus.CMenuSetting setting = null, Drilling.UI.Menu.Menus.CMenuAlarm alarm = null, Drilling.UI.Menu.Menus.CMenuMonitor monitor = null, Drilling.UI.Menu.Menus.CMenuPm pm = null)
- Properties:
  - public Drilling.UI.Menu.Menus.CMenuAlarm Alarm (get)
  - public Boolean IsAlarmLayout (get)
  - public Boolean IsGenericLayout (get)
  - public Boolean IsMainLayout (get)
  - public Boolean IsManualLayout (get)
  - public Boolean IsMonitorLayout (get)
  - public Boolean IsPmLayout (get)
  - public Boolean IsRecipeLayout (get)
  - public Boolean IsSettingLayout (get)
  - public Drilling.UI.Menu.Menus.CMenuMain MainOperating (get)
  - public Drilling.UI.Menu.Menus.CMenuManual Manual (get)
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> Metrics (get)
  - public Drilling.UI.Menu.Menus.CMenuMonitor Monitor (get)
  - public Drilling.UI.Menu.Menus.CMenuPm Pm (get)
  - public Drilling.UI.Menu.Menus.CMenuRecipe Recipe (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION> Sections (get)
  - public Drilling.UI.Menu.Menus.CMenuSetting Setting (get)
  - public Boolean ShowCycleControls (get)
  - public String Subtitle (get)
  - public String Title (get)

### Drilling.UI.Menu.Menus.CStatusBrush

- Kind: static class
- Fields:
  - public static readonly Windows.Media.Brush Active
  - public static readonly Windows.Media.Brush CommandBlue
  - public static readonly Windows.Media.Brush CommandBlueBorder
  - public static readonly Windows.Media.Brush CommandDark
  - public static readonly Windows.Media.Brush CommandDarkBorder
  - public static readonly Windows.Media.Brush CommandGreen
  - public static readonly Windows.Media.Brush CommandGreenBorder
  - public static readonly Windows.Media.Brush CommandRed
  - public static readonly Windows.Media.Brush CommandRedBorder
  - public static readonly Windows.Media.Brush Muted
  - public static readonly Windows.Media.Brush Offline
  - public static readonly Windows.Media.Brush Online
  - public static readonly Windows.Media.Brush PrimaryText
  - public static readonly Windows.Media.Brush Recipe
  - public static readonly Windows.Media.Brush Simul
  - public static readonly Windows.Media.Brush Wait
- Methods:
  - public static Windows.Media.Brush ForDisplayState(String state)
  - public static Windows.Media.Brush ForHeaderState(String state)
  - public static Windows.Media.Brush ForHeadStatus(String status)
  - public static Windows.Media.SolidColorBrush Frozen(Byte r, Byte g, Byte b)
  - private static String Normalize(String value)

### Drilling.UI.Menu.Menus.EN_MENU

- Kind: enum
- Interfaces: System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
- Values:
  - Main = 0
  - Manual = 1
  - Recipe = 2
  - Setting = 3
  - Alarm = 4
  - Monitor = 5
  - Pm = 6
  - Exit = 7

### Drilling.UI.Menu.Menus.IMenu

- Kind: interface
- Properties:
  - public Drilling.UI.Menu.Menus.EN_MENU Menu (get)
- Methods:
  - public Threading.Tasks.Task<Drilling.UI.Menu.Menus.CScreenViewModel> Build(Threading.CancellationToken cancellationToken = null)

### Drilling.UI.Menu.Menus.ST_ALARM_CURRENT_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_ALARM_CURRENT_ROW>
- Constructors:
  - public ST_ALARM_CURRENT_ROW(String Code, String Level, String Device, String Message, String Cause, String Action, String Time)
- Properties:
  - public String Action (get/set)
  - public String Cause (get/set)
  - public String Code (get/set)
  - public String Device (get/set)
  - private Type EqualityContract (get)
  - public String Level (get/set)
  - public Windows.Media.Brush LevelBrush (get)
  - public String Message (get/set)
  - public String Time (get/set)

### Drilling.UI.Menu.Menus.ST_ALARM_DETAIL_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_ALARM_DETAIL_ROW>
- Constructors:
  - public ST_ALARM_DETAIL_ROW(String Name, String Value, String State = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String State (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_ALARM_HISTORY_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_ALARM_HISTORY_ROW>
- Constructors:
  - public ST_ALARM_HISTORY_ROW(String Time, String Code, String Level, String Device, String Message, String ResetUser)
- Properties:
  - public String Code (get/set)
  - public String Device (get/set)
  - private Type EqualityContract (get)
  - public String Level (get/set)
  - public Windows.Media.Brush LevelBrush (get)
  - public String Message (get/set)
  - public String ResetUser (get/set)
  - public String Time (get/set)

### Drilling.UI.Menu.Menus.ST_ALARM_SUMMARY_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_ALARM_SUMMARY_ITEM>
- Constructors:
  - public ST_ALARM_SUMMARY_ITEM(String Name, String Value, String State = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String State (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_ALARM_TREND_BAR

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_ALARM_TREND_BAR>
- Constructors:
  - public ST_ALARM_TREND_BAR(String Time, Double Scanner, Double Laser, Double Chiller, Double Motion, Double Total, Double TotalY)
- Properties:
  - public Double Chiller (get/set)
  - private Type EqualityContract (get)
  - public Double Laser (get/set)
  - public Double Motion (get/set)
  - public Double Scanner (get/set)
  - public String Time (get/set)
  - public Double Total (get/set)
  - public Double TotalY (get/set)

### Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM>
- Constructors:
  - public ST_DISPLAY_ITEM(String Name, String Value, String Detail = )
- Properties:
  - public String Detail (get/set)
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public Windows.Media.Brush StateBrush (get)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_HEAD_PARAMETER

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_HEAD_PARAMETER>
- Constructors:
  - public ST_HEAD_PARAMETER(String Head, Boolean Use, String Shape, Double Power, Double Frequency, Int32 Shot, Double Speed, Double OffsetX, Double OffsetY)
- Properties:
  - private Type EqualityContract (get)
  - public Double Frequency (get/set)
  - public String Head (get/set)
  - public Double OffsetX (get/set)
  - public Double OffsetY (get/set)
  - public Double Power (get/set)
  - public String Shape (get/set)
  - public Int32 Shot (get/set)
  - public Double Speed (get/set)
  - public Boolean Use (get/set)

### Drilling.UI.Menu.Menus.ST_HEAD_PREVIEW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_HEAD_PREVIEW>
- Constructors:
  - public ST_HEAD_PREVIEW(Int32 HeadNo, String HeadName, String Status, String PointSummary, Windows.Media.Geometry PreviewGeometry, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public String HeadName (get/set)
  - public Int32 HeadNo (get/set)
  - public Boolean IsSelected (get/set)
  - public String PointSummary (get/set)
  - public Windows.Media.Geometry PreviewGeometry (get/set)
  - public String Status (get/set)
  - public Windows.Media.Brush StatusBrush (get)

### Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_HEADER_STATUS_ITEM>
- Constructors:
  - public ST_HEADER_STATUS_ITEM(String Name, String Value, String State, Boolean CanNavigate = False, String PageText = , Drilling.UI.Menu.CButtonCommand PreviousCommand = null, Drilling.UI.Menu.CButtonCommand NextCommand = null, Drilling.UI.Menu.CButtonCommand OpenCommand = null)
- Properties:
  - public Windows.Media.Brush AccentBrush (get)
  - public Boolean CanNavigate (get/set)
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public Drilling.UI.Menu.CButtonCommand Next (get)
  - public Drilling.UI.Menu.CButtonCommand NextCommand (get/set)
  - public Drilling.UI.Menu.CButtonCommand Open (get)
  - public Drilling.UI.Menu.CButtonCommand OpenCommand (get/set)
  - public String PageText (get/set)
  - public Drilling.UI.Menu.CButtonCommand Previous (get)
  - public Drilling.UI.Menu.CButtonCommand PreviousCommand (get/set)
  - public String State (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_INTERFACE_LOG_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_INTERFACE_LOG_ITEM>
- Constructors:
  - public ST_INTERFACE_LOG_ITEM(String Time, String Level, String Source, String Message)
- Properties:
  - private Type EqualityContract (get)
  - public String Level (get/set)
  - public String Message (get/set)
  - public String Source (get/set)
  - public String Time (get/set)

### Drilling.UI.Menu.Menus.ST_INTERLOCK_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_INTERLOCK_ITEM>
- Constructors:
  - public ST_INTERLOCK_ITEM(String Signal, String State, String Detail, String Result)
- Properties:
  - public String Detail (get/set)
  - private Type EqualityContract (get)
  - public String Result (get/set)
  - public String Signal (get/set)
  - public String State (get/set)

### Drilling.UI.Menu.Menus.ST_MANUAL_COMMAND_STATE

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MANUAL_COMMAND_STATE>
- Constructors:
  - public ST_MANUAL_COMMAND_STATE(String Name, String Value, String Unit = )
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String Unit (get/set)
  - public String Value (get/set)

### Drilling.UI.Menu.Menus.ST_MANUAL_HEAD_CARD

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MANUAL_HEAD_CARD>
- Constructors:
  - public ST_MANUAL_HEAD_CARD(Int32 HeadNo, String HeadName, String Gx, String Gy, String State, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public String Gx (get/set)
  - public String Gy (get/set)
  - public String HeadName (get/set)
  - public Int32 HeadNo (get/set)
  - public Boolean IsSelected (get/set)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)

### Drilling.UI.Menu.Menus.ST_MANUAL_PARAMETER

- Kind: class
- Base: Drilling.UI.Menu.CBindingBase
- Interfaces: System.ComponentModel.INotifyPropertyChanged
- Constructors:
  - public ST_MANUAL_PARAMETER(String parameter, String value, String unit)
- Fields:
  - private String _value
- Properties:
  - public String Parameter (get)
  - public String Unit (get)
  - public String Value (get/set)

### Drilling.UI.Menu.Menus.ST_MANUAL_SETTING_FILE

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MANUAL_SETTING_FILE>
- Constructors:
  - public ST_MANUAL_SETTING_FILE(String Name, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)
  - public String Name (get/set)

### Drilling.UI.Menu.Menus.ST_MONITOR_AXIS_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_AXIS_ROW>
- Constructors:
  - public ST_MONITOR_AXIS_ROW(String Axis, String Name, String CurrentPosition, String TargetPosition, String CommandPosition, String Servo, String Home, String LimitPlus, String LimitMinus, String Alarm, String State, Boolean IsSelected = False)
- Properties:
  - public String Alarm (get/set)
  - public String Axis (get/set)
  - public String CommandPosition (get/set)
  - public String CurrentPosition (get/set)
  - private Type EqualityContract (get)
  - public String Home (get/set)
  - public Boolean IsSelected (get/set)
  - public String LimitMinus (get/set)
  - public String LimitPlus (get/set)
  - public String Name (get/set)
  - public Windows.Media.Brush RowBrush (get)
  - public String Servo (get/set)
  - public Windows.Media.Brush ServoBrush (get)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)
  - public String TargetPosition (get/set)

### Drilling.UI.Menu.Menus.ST_MONITOR_BET_TABLE_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_BET_TABLE_ROW>
- Constructors:
  - public ST_MONITOR_BET_TABLE_ROW(String No, Boolean Use, String BeamSize, String Mag, String Div, String MagPosition, String DivPosition, String Tolerance, String State, Boolean IsSelected = False)
- Properties:
  - public String BeamSize (get/set)
  - public String Div (get/set)
  - public String DivPosition (get/set)
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)
  - public String Mag (get/set)
  - public String MagPosition (get/set)
  - public String No (get/set)
  - public Windows.Media.Brush RowBrush (get)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)
  - public String Tolerance (get/set)
  - public Boolean Use (get/set)
  - public Windows.Media.Brush UseBrush (get)
  - public String UseText (get)

### Drilling.UI.Menu.Menus.ST_MONITOR_COMMAND_HISTORY_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_COMMAND_HISTORY_ROW>
- Constructors:
  - public ST_MONITOR_COMMAND_HISTORY_ROW(String Time, String User, String Name, String Command, String Target, String Result)
- Properties:
  - public String Command (get/set)
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String Result (get/set)
  - public Windows.Media.Brush ResultBrush (get)
  - public String Target (get/set)
  - public String Time (get/set)
  - public String User (get/set)

### Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_IO_ROW>
- Constructors:
  - public ST_MONITOR_IO_ROW(String Id, String Address, String Name, String State, String OnDelay, String OffDelay, String Description, Boolean IsSelected = False)
- Properties:
  - public String Address (get/set)
  - public String Description (get/set)
  - private Type EqualityContract (get)
  - public String Id (get/set)
  - public Boolean IsSelected (get/set)
  - public String Name (get/set)
  - public String OffDelay (get/set)
  - public String OnDelay (get/set)
  - public Windows.Media.Brush RowBrush (get)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)
- Methods:
  - private static Windows.Media.Brush MonitorStatusBrush(String state)

### Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_OPERATION_BUTTON>
- Constructors:
  - public ST_MONITOR_OPERATION_BUTTON(String Label, String Icon, String Tone)
- Properties:
  - public Windows.Media.Brush BackgroundBrush (get)
  - public Windows.Media.Brush BorderBrush (get)
  - private Type EqualityContract (get)
  - public String Icon (get/set)
  - public Windows.Media.Geometry IconGeometry (get)
  - public String Label (get/set)
  - public String Tone (get/set)

### Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_PARAMETER_ROW>
- Constructors:
  - public ST_MONITOR_PARAMETER_ROW(String Parameter, String Value, String Unit, String State = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Parameter (get/set)
  - public String State (get/set)
  - public String Unit (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_MONITOR_POSITION_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_POSITION_ROW>
- Constructors:
  - public ST_MONITOR_POSITION_ROW(String Name, String Value, String Unit, String State = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String State (get/set)
  - public String Unit (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HEAD_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HEAD_ROW>
- Constructors:
  - public ST_MONITOR_PRODUCT_HEAD_ROW(String Head, String State, String TotalPoints, String CompletedPoints, String Result, String ErrorCode, String Message)
- Properties:
  - public String CompletedPoints (get/set)
  - private Type EqualityContract (get)
  - public String ErrorCode (get/set)
  - public String Head (get/set)
  - public String Message (get/set)
  - public String Result (get/set)
  - public Windows.Media.Brush ResultBrush (get)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)
  - public String TotalPoints (get/set)

### Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HISTORY_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_HISTORY_ROW>
- Constructors:
  - public ST_MONITOR_PRODUCT_HISTORY_ROW(String Time, String ProductId, String RecipeId, String Action, String State, String Result, String Detail)
- Properties:
  - public String Action (get/set)
  - public String Detail (get/set)
  - private Type EqualityContract (get)
  - public String ProductId (get/set)
  - public String RecipeId (get/set)
  - public String Result (get/set)
  - public Windows.Media.Brush ResultBrush (get)
  - public String State (get/set)
  - public String Time (get/set)

### Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_PRODUCT_ITEM>
- Constructors:
  - public ST_MONITOR_PRODUCT_ITEM(String Name, String Value, String Tone = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String Tone (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_MONITOR_STATUS_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_STATUS_ROW>
- Constructors:
  - public ST_MONITOR_STATUS_ROW(String Item, String Value, String State, String Unit, String Description)
- Properties:
  - public String Description (get/set)
  - private Type EqualityContract (get)
  - public String Item (get/set)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)
  - public String Unit (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_MONITOR_SUMMARY_ITEM

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_SUMMARY_ITEM>
- Constructors:
  - public ST_MONITOR_SUMMARY_ITEM(String Name, String Value, String Unit, String State = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String State (get/set)
  - public String Unit (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_MONITOR_TAB

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_TAB>
- Constructors:
  - public ST_MONITOR_TAB(String Name, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)
  - public String Name (get/set)

### Drilling.UI.Menu.Menus.ST_MONITOR_TREND_POINT

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_MONITOR_TREND_POINT>
- Constructors:
  - public ST_MONITOR_TREND_POINT(String Time, Double PrimaryY, Double SecondaryY, Double TertiaryY)
- Properties:
  - private Type EqualityContract (get)
  - public Double PrimaryY (get/set)
  - public Double SecondaryY (get/set)
  - public Double TertiaryY (get/set)
  - public String Time (get/set)

### Drilling.UI.Menu.Menus.ST_PWM_DEVICE_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_PWM_DEVICE_ROW>
- Constructors:
  - public ST_PWM_DEVICE_ROW(String Item, String Value, String Unit, String Command)
- Properties:
  - public String Command (get/set)
  - private Type EqualityContract (get)
  - public String Item (get/set)
  - public String Unit (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_PWM_PROCESS_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_PWM_PROCESS_ROW>
- Constructors:
  - public ST_PWM_PROCESS_ROW(String No, String ProcessName, String Use, String State, String AveragePower, Boolean IsSelected = False)
- Properties:
  - public String AveragePower (get/set)
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)
  - public String No (get/set)
  - public String ProcessName (get/set)
  - public Windows.Media.Brush RowBrush (get)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)
  - public String Use (get/set)
  - public Windows.Media.Brush UseBrush (get)

### Drilling.UI.Menu.Menus.ST_PWM_SETTING_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_PWM_SETTING_ROW>
- Constructors:
  - public ST_PWM_SETTING_ROW(String Parameter, String Value, String Unit)
- Properties:
  - private Type EqualityContract (get)
  - public String Parameter (get/set)
  - public String Unit (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_PWM_STEP_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_PWM_STEP_ROW>
- Constructors:
  - public ST_PWM_STEP_ROW(String Step, String OptionName, String PowerOut, String PowerUnit, String SettingAtt, String SettingPower, String SettingFreq, String MeasureCycle, String MeasureTime, String MeasureInterval, String StartDelay, String CycleDelay, String Rotator, String MeasurePower, String State, Boolean IsSelected = False)
- Properties:
  - public String CycleDelay (get/set)
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)
  - public String MeasureCycle (get/set)
  - public String MeasureInterval (get/set)
  - public String MeasurePower (get/set)
  - public String MeasureTime (get/set)
  - public String OptionName (get/set)
  - public Windows.Media.Brush PowerBrush (get)
  - public String PowerOut (get/set)
  - public String PowerUnit (get/set)
  - public String Rotator (get/set)
  - public Windows.Media.Brush RowBrush (get)
  - public String SettingAtt (get/set)
  - public String SettingFreq (get/set)
  - public String SettingPower (get/set)
  - public String StartDelay (get/set)
  - public String State (get/set)
  - public Windows.Media.Brush StateBrush (get)
  - public String Step (get/set)

### Drilling.UI.Menu.Menus.ST_RECIPE_CATEGORY_TAB

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_RECIPE_CATEGORY_TAB>
- Constructors:
  - public ST_RECIPE_CATEGORY_TAB(String Category, Boolean IsSelected)
- Properties:
  - public String Category (get/set)
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)

### Drilling.UI.Menu.Menus.ST_RECIPE_FILE

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_RECIPE_FILE>
- Constructors:
  - public ST_RECIPE_FILE(String No, String FileName, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public String FileName (get/set)
  - public Boolean IsSelected (get/set)
  - public String No (get/set)

### Drilling.UI.Menu.Menus.ST_RECIPE_GROUP_TAB

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_RECIPE_GROUP_TAB>
- Constructors:
  - public ST_RECIPE_GROUP_TAB(String Group, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public String Group (get/set)
  - public Boolean IsSelected (get/set)

### Drilling.UI.Menu.Menus.ST_RECIPE_HISTORY_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_RECIPE_HISTORY_ROW>
- Constructors:
  - public ST_RECIPE_HISTORY_ROW(String Time, String Action, String Tab, String Group, String Item, String Before, String After)
- Properties:
  - public String Action (get/set)
  - public String After (get/set)
  - public Windows.Media.Brush AfterBrush (get)
  - public String Before (get/set)
  - private Type EqualityContract (get)
  - public String Group (get/set)
  - public String Item (get/set)
  - public String Tab (get/set)
  - public String Time (get/set)

### Drilling.UI.Menu.Menus.ST_RECIPE_MANAGED_ITEM

- Kind: class
- Base: Drilling.UI.Menu.CBindingBase
- Interfaces: System.ComponentModel.INotifyPropertyChanged
- Constructors:
  - public ST_RECIPE_MANAGED_ITEM(String category, String group, String item, String value, String unit, String description, String valueState = Normal, String key = , String sourceGroup = , Drilling.Common.Managers.EN_RECIPE_DATA_TYPE dataType = String, Double changeLimit = 0, Double min = 0, Double max = 0)
- Fields:
  - private readonly String _initialValue
  - private readonly String _initialValueState
  - private String _value
  - private String _valueState
- Properties:
  - public String Category (get)
  - public Double ChangeLimit (get)
  - public Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType (get)
  - public String Description (get)
  - public String Group (get)
  - public Boolean IsEdited (get)
  - public String Item (get)
  - public String Key (get)
  - public Double Max (get)
  - public Double Min (get)
  - public String OriginalValue (get)
  - public String SourceGroup (get)
  - public String Unit (get)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)
  - public String ValueState (get/set)
- Methods:
  - private static String NormalizeValue(String value)

### Drilling.UI.Menu.Menus.ST_RECIPE_STATE_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_RECIPE_STATE_ROW>
- Constructors:
  - public ST_RECIPE_STATE_ROW(String Name, String Value, String State = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String State (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_SCREEN_SECTION

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_SCREEN_SECTION>
- Constructors:
  - public ST_SCREEN_SECTION(String Title, Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> Items)
- Properties:
  - private Type EqualityContract (get)
  - public Collections.Generic.IReadOnlyList<Drilling.UI.Menu.Menus.ST_DISPLAY_ITEM> Items (get/set)
  - public String Title (get/set)

### Drilling.UI.Menu.Menus.ST_SETTING_GROUP

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_SETTING_GROUP>
- Constructors:
  - public ST_SETTING_GROUP(String Name, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)
  - public String Name (get/set)

### Drilling.UI.Menu.Menus.ST_SETTING_HISTORY_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_SETTING_HISTORY_ROW>
- Constructors:
  - public ST_SETTING_HISTORY_ROW(String Time, String User, String Tab, String Parameter, String Before, String After, String AfterState = Warn)
- Properties:
  - public String After (get/set)
  - public Windows.Media.Brush AfterBrush (get)
  - public String AfterState (get/set)
  - public String Before (get/set)
  - private Type EqualityContract (get)
  - public String Parameter (get/set)
  - public String Tab (get/set)
  - public String Time (get/set)
  - public String User (get/set)

### Drilling.UI.Menu.Menus.ST_SETTING_INTERFACE_ROW

- Kind: class
- Base: Drilling.UI.Menu.CBindingBase
- Interfaces: System.ComponentModel.INotifyPropertyChanged
- Constructors:
  - public ST_SETTING_INTERFACE_ROW(String no, String type, String device, String number, String nickName, String systemSection, String autoConnection, String simul, String arg1, String arg2, String arg3, String arg4, String arg5, Collections.Generic.IReadOnlyDictionary<String, String> extra = null)
- Fields:
  - private String _arg1
  - private String _arg2
  - private String _arg3
  - private String _arg4
  - private String _arg5
  - private String _autoConnection
  - private String _device
  - private String _nickName
  - private String _number
  - private readonly String _originalArg1
  - private readonly String _originalArg2
  - private readonly String _originalArg3
  - private readonly String _originalArg4
  - private readonly String _originalArg5
  - private readonly String _originalAutoConnection
  - private readonly String _originalDevice
  - private readonly String _originalNickName
  - private readonly String _originalNumber
  - private readonly String _originalSimul
  - private readonly String _originalSystemSection
  - private readonly String _originalType
  - private String _simul
  - private String _systemSection
  - private String _type
- Properties:
  - public String Arg1 (get/set)
  - public String Arg2 (get/set)
  - public String Arg3 (get/set)
  - public String Arg4 (get/set)
  - public String Arg5 (get/set)
  - public String AutoConnection (get/set)
  - public String Device (get/set)
  - public Collections.Generic.IReadOnlyDictionary<String, String> Extra (get)
  - public Boolean IsModified (get)
  - public Boolean IsSimulation (get)
  - public Windows.Media.Brush ModifiedBrush (get)
  - public String ModifiedText (get)
  - public String NickName (get/set)
  - public String No (get)
  - public String Number (get/set)
  - public String Simul (get/set)
  - public Windows.Media.Brush SimulBrush (get)
  - public String SystemSection (get/set)
  - public String Type (get/set)
- Methods:
  - private static Boolean IsChanged(String current, String original)
  - private static String Normalize(String value)
  - private Void SetEditable(String& field, String value)

### Drilling.UI.Menu.Menus.ST_SETTING_SUMMARY_ROW

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_SETTING_SUMMARY_ROW>
- Constructors:
  - public ST_SETTING_SUMMARY_ROW(String Name, String Value, String State = Normal)
- Properties:
  - private Type EqualityContract (get)
  - public String Name (get/set)
  - public String State (get/set)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)

### Drilling.UI.Menu.Menus.ST_SETTING_TAB

- Kind: class
- Interfaces: System.IEquatable<Drilling.UI.Menu.Menus.ST_SETTING_TAB>
- Constructors:
  - public ST_SETTING_TAB(String Name, Boolean IsSelected)
- Properties:
  - private Type EqualityContract (get)
  - public Boolean IsSelected (get/set)
  - public String Name (get/set)

### Drilling.UI.Menu.Menus.ST_SYSTEM_PARAMETER_ROW

- Kind: class
- Base: Drilling.UI.Menu.CBindingBase
- Interfaces: System.ComponentModel.INotifyPropertyChanged
- Constructors:
  - public ST_SYSTEM_PARAMETER_ROW(String group, String parameter, String value, String unit, String description, Boolean isModified = False, String valueState = Normal, String key = , String defaultValue = , Drilling.Common.Managers.EN_RECIPE_DATA_TYPE dataType = String, Double min = 0, Double max = 0)
- Fields:
  - private readonly String _originalValue
  - private readonly String _originalValueState
  - private String _value
  - private String _valueState
- Properties:
  - public Drilling.Common.Managers.EN_RECIPE_DATA_TYPE DataType (get)
  - public String DefaultValue (get)
  - public String Description (get)
  - public String Group (get)
  - public Boolean IsModified (get)
  - public String Key (get)
  - public Double Max (get)
  - public Double Min (get)
  - public Windows.Media.Brush ModifiedBrush (get)
  - public String ModifiedText (get)
  - public String OriginalValue (get)
  - public String Parameter (get)
  - public String Unit (get)
  - public String Value (get/set)
  - public Windows.Media.Brush ValueBrush (get)
  - public String ValueState (get/set)
- Methods:
  - private static String NormalizeValue(String value)

### Drilling.UI.Popup.CInterfaceStatusDialog

- Kind: class
- Base: System.Windows.Window
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.IWindowService, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CInterfaceStatusDialog(String title, Collections.Generic.IReadOnlyList<Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS> statuses)
- Fields:
  - private Boolean _contentLoaded
  - internal Windows.Controls.DataGrid StatusGrid
  - internal Windows.Controls.TextBlock SummaryText
  - internal Windows.Controls.TextBlock TitleText
- Methods:
  - public Void InitializeComponent()
  - private Void OnCloseClicked(Object sender, Windows.RoutedEventArgs e)
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Popup.CInterfaceStatusDialog+CInterfaceStatusRow

- Kind: class
- Constructors:
  - public CInterfaceStatusRow(Drilling.Common.Interface.ST_INTERFACE_COMM_STATUS status)
- Properties:
  - public String Endpoint (get)
  - public String InterfaceType (get)
  - public String LastChangedText (get)
  - public String NickName (get)
  - public Int32 No (get)
  - public String State (get)
  - public Windows.Media.Brush StateBrush (get)
- Methods:
  - private static String ToStateText(Drilling.Common.Interface.EN_COMM_STATE state)

### Drilling.UI.Popup.CRecipeConfirmDialog

- Kind: class
- Base: System.Windows.Window
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.IWindowService, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CRecipeConfirmDialog(String title, String message, String confirmText = OK)
- Fields:
  - private Boolean _contentLoaded
  - internal Windows.Controls.Button ConfirmButton
  - internal Windows.Controls.TextBlock MessageText
  - internal Windows.Controls.TextBlock TitleText
- Methods:
  - public Void InitializeComponent()
  - private Void OnCancelClicked(Object sender, Windows.RoutedEventArgs e)
  - private Void OnConfirmClicked(Object sender, Windows.RoutedEventArgs e)
  - protected Void OnKeyDown(Windows.Input.KeyEventArgs e)
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Popup.CRecipeNameDialog

- Kind: class
- Base: System.Windows.Window
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.IWindowService, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CRecipeNameDialog(String title, String message, String initialName, Func<String, String> validate = null)
- Fields:
  - private Boolean _contentLoaded
  - private readonly Func<String, String> _validate
  - internal Windows.Controls.TextBlock ErrorText
  - internal Windows.Controls.TextBlock MessageText
  - internal Windows.Controls.Button OkButton
  - internal Windows.Controls.TextBox RecipeNameTextBox
  - internal Windows.Controls.TextBlock TitleText
- Properties:
  - public String RecipeName (get)
- Methods:
  - public Void InitializeComponent()
  - private Void OnCancelClicked(Object sender, Windows.RoutedEventArgs e)
  - private Void OnOkClicked(Object sender, Windows.RoutedEventArgs e)
  - private Void OnRecipeNameKeyDown(Object sender, Windows.Input.KeyEventArgs e)
  - private Void OnRecipeNameTextChanged(Object sender, Windows.Controls.TextChangedEventArgs e)
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)
  - private Void UpdateOkState()

### Drilling.UI.Views.CAlarmView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CAlarmView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

## Automation1 Script I/F Update (2026-07-20)

- `AeroScriptClient.HealthCheckAsync`: Server TCP 수신과 API Key/ModePolicy 준비 상태를 Job 생성 전에 확인한다.
- `ScriptRequestType.HealthCheck`: protocol v3 준비 상태 요청이다.
- `AeroScriptServer.DispatchAsync`: HealthCheck에 bind 주소, port, mode policy, 최대 Script 크기를 응답한다.
- `AeroScriptLocalFileStore`: Local Script 경로 정규화, 폴더 생성, UTF-8(no BOM) 실제 파일 저장을 담당한다.
- `MainWindow.GenerateCurrentAeroScriptPackage`: Process/Review 좌표 로그를 남기고 Client의 Local Script File에 UTF-8(no BOM) 파일을 실제 저장한다.
- `MainWindow.SelectAllProcessablePointsForHighlightedScanners`: 선택 Head의 `InField` 좌표 전체를 Matrix 선택 집합으로 만든다.

## MOF Coordinate Sample - Automation1 Client/Server Classes (2026-07-20)

- `AeroScriptGenerator`: Client PC에서 `VirtualWaitSimulation` 또는 `HardwareCoordinateProgram`을 생성한다. Virtual 모드는 단일 Head, Stage PositionFeedback wait, GX band/GY point 반복을 강제한다.
- `AeroScriptGenerationOptions`: Stage Y, GX/GY, speed, ramp, FIR, MotionUpdateRate, ExecuteNumLines, MoveDelay, wait step과 software limit을 전달한다.
- `AeroScriptPackage`: Job ID, controller file, UTF-8 source, SHA-256, Task index와 생성 모드를 전달한다.
- `ScriptServerRequest` / `ScriptServerResponse`: Upload, Run, Status protocol 계약이다.
- `AeroScriptProtocol`: 4-byte big-endian 길이와 UTF-8 JSON frame을 읽고 쓴다.
- `AeroScriptClient`: WPF Client에서 Scanner Server로 명령을 보낸다.
- `AeroScriptServer`: Server PC의 검증, spool 저장, 단일 Task 실행 queue, 상태 snapshot을 관리한다.
- `AeroScriptModePolicy`: `Any`, `VirtualOnly`, `HardwareOnly` 정책으로 잘못된 생성 모드 Job을 Upload 단계에서 차단한다.
- `IAutomation1Runtime`: Server와 Automation1 실행 구현 사이의 경계이다.
- `SimulationAutomation1Runtime`: SDK 없는 PC에서 TCP/Job 상태만 검증한다. AeroScript와 Wait 구문은 실행하지 않는다.
- `Automation1ReflectionRuntime`: 공식 Automation1 .NET API DLL을 런타임에 로드해 `Files.WriteText`, `Task.Program.Run`, `TaskState` 폴링을 수행한다.
- `Automation1Server/Program`: Server PC용 독립 실행 진입점과 TCP 통합 self-test를 제공한다.
- `RUN_AUTOMATION1_VIRTUAL_WAIT_SERVER.bat`: Automation1 Virtual Controller에 접속해 Laser/PSO 없이 wait 기반 1D MOF 순서를 검증한다.

## Local Update: Shell UI / Review Offset Base

This section documents the local-only changes made after the original inventory generation.

### Drilling.Common.Review.CReviewCorrectionManager

- Kind: class
- Purpose: Converts Review measurement errors into Head-level correction offsets.
- Important methods:
  - `CreatePolicy(IReadOnlyDictionary<string,string>)`
  - `Calculate(ST_REVIEW_MEASUREMENT_BATCH, ST_REVIEW_OFFSET_POLICY)`
  - `ApplyToProcessPlan(ST_PROCESS_PLAN, ST_REVIEW_CORRECTION_RESULT)`
- Data movement:
  - reads `REVIEW_OFFSET_*` parameters
  - calculates Head offsets from `ST_REVIEW_MEASUREMENT_POINT.ErrorX/ErrorY`
  - writes `Hxx_OFFSET_X`, `Hxx_OFFSET_Y`, `Hxx_REVIEW_OFFSET_X`, `Hxx_REVIEW_OFFSET_Y`

### Drilling.Common.Review.ST_REVIEW_MEASUREMENT_POINT

- Kind: record
- Main fields:
  - `PointId`, `HeadNo`, `CellId`
  - `DesignX`, `DesignY`
  - `ProcessX`, `ProcessY`
  - `ReviewX`, `ReviewY`
  - `MeasuredX`, `MeasuredY`
  - `ErrorX`, `ErrorY`
  - `ToleranceX`, `ToleranceY`
  - `BeamNo`, `IsUsed`, `MeasuredAt`

### Drilling.Common.Review.ST_REVIEW_MEASUREMENT_BATCH

- Kind: record
- Main fields:
  - `BatchId`, `ProcessId`, `RecipeId`, `ProductId`
  - `Mode`, `Source`, `Points`, `CreatedAt`

### Drilling.Common.Review.ST_REVIEW_OFFSET_POLICY

- Kind: record
- Main fields:
  - `GainX`, `GainY`
  - `MaxAbsOffsetX`, `MaxAbsOffsetY`
  - `MinSampleCount`
  - `AccumulateWithCurrentOffset`
  - `ApplyToNextProcessPlan`

### Drilling.Common.Review.ST_REVIEW_HEAD_OFFSET

- Kind: record
- Main fields:
  - `HeadNo`, `OffsetX`, `OffsetY`, `Theta`
  - `SampleCount`, `Source`, `CreatedAt`

### Drilling.Common.Review.ST_REVIEW_CORRECTION_RESULT

- Kind: record
- Main fields:
  - `BatchId`, `IsApplicable`, `Message`
  - `HeadOffsets`, `Policy`, `CreatedAt`

### Drilling.Common.Station.CStationProcess

- Added conceptual auto steps:
  - `REVIEW_MEASURE`
  - `REVIEW_OFFSET`
- Added state:
  - `_reviewCorrectionManager`
  - `_lastReviewBatch`
  - `_lastReviewCorrection`
- Added methods:
  - `CollectReviewMeasurement(...)`
  - `ApplyReviewOffset(...)`
  - `CreateSimulationReviewBatch(...)`
  - `CreateSimulationReviewPoint(...)`
  - `ReadReviewMode(...)`
- Updated flow:
  - `PLAN -> INTERLOCK -> PARAMETER -> DEVICE -> SCRIPT -> TASK -> WAIT_DONE -> REVIEW_MEASURE -> REVIEW_OFFSET -> COMPLETE`

### Drilling.UI.CRootView.xaml

- Kind: WPF shell view
- Change:
  - detailed operator views are no longer rendered from the root layout
  - screen now shows a minimal structure shell for code walkthrough
  - `StartCommand` and `StopCommand` remain bound so the station/review flow can still be exercised
### Drilling.UI.Views.CExitView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CExitView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Views.CMainView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CMainView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Views.CManualView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CManualView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Views.CMonitorView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CMonitorView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Views.CPmView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CPmView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Views.CRecipeView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CRecipeView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

### Drilling.UI.Views.CSettingView

- Kind: class
- Base: System.Windows.Controls.UserControl
- Interfaces: System.ComponentModel.ISupportInitialize, System.Windows.IFrameworkInputElement, System.Windows.IInputElement, System.Windows.Markup.IAddChild, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IHaveResources, System.Windows.Markup.IQueryAmbient, System.Windows.Media.Animation.IAnimatable, System.Windows.Media.Composition.DUCE+IResource
- Constructors:
  - public CSettingView()
- Fields:
  - private Boolean _contentLoaded
- Methods:
  - public Void InitializeComponent()
  - private Void System.Windows.Markup.IComponentConnector.Connect(Int32 connectionId, Object target)

