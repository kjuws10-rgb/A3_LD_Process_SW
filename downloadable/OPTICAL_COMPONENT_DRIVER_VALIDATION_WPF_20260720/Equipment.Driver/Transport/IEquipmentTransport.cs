namespace Equipment.Driver;

public interface IEquipmentTransport : IAsyncDisposable
{
    EN_EQUIPMENT_CONNECTION ConnectionState { get; }
    string Endpoint { get; }
    Task Connect(CancellationToken cancellationToken = default);
    Task Disconnect(CancellationToken cancellationToken = default);
    Task<string> Exchange(string command, bool expectResponse, CancellationToken cancellationToken = default);
}
