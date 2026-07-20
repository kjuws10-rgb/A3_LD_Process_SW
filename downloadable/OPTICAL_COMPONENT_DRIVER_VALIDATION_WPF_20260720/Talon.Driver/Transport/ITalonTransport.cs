namespace Talon.Driver;

public interface ITalonTransport : IAsyncDisposable
{
    EN_TALON_CONNECTION_STATE ConnectionState { get; }
    string Endpoint { get; }
    Task Connect(CancellationToken cancellationToken = default);
    Task Disconnect(CancellationToken cancellationToken = default);
    Task<string> Exchange(string command, bool expectResponse, CancellationToken cancellationToken = default);
}
