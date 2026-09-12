using NewsReader.Core.Nntp;

namespace NewsReader.Core.Interfaces;

public interface INntpConnection : IAsyncDisposable
{
    bool IsConnected { get; }

    Task<NntpResponse> ConnectAsync(CancellationToken cancellationToken = default);

    Task WriteLineAsync(string line, CancellationToken cancellationToken = default);

    Task<NntpResponse> ReadResponseAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ReadMultilineAsync(CancellationToken cancellationToken = default);
}
