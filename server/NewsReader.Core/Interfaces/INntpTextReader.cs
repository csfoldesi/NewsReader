namespace NewsReader.Core.Interfaces;

public interface INntpTextReader : IAsyncDisposable
{
    ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ReadMultilineAsync(CancellationToken cancellationToken = default);
}
