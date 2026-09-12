using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NewsReader.Core.Decoding;
using NewsReader.Core.Interfaces;

namespace NewsReader.Core.Nntp;

public sealed class NntpConnection : INntpConnection
{
    private readonly NntpConnectionOptions _options;
    private TcpClient? _client;
    private Stream? _stream;
    private NntpTextReader? _reader;
    private bool _disposed;

    public NntpConnection(NntpConnectionOptions options)
    {
        _options = options;
    }

    public bool IsConnected => _client?.Connected == true && _stream is not null;

    public async Task<NntpResponse> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.Timeout);

        try
        {
            await _client.ConnectAsync(_options.Host, _options.Port, timeoutCts.Token).ConfigureAwait(false);

            Stream stream = _client.GetStream();
            if (_options.UseTls)
            {
                var ssl = new SslStream(stream, leaveInnerStreamOpen: false, ValidateCertificate);
                await ssl.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions { TargetHost = _options.Host },
                    timeoutCts.Token
                ).ConfigureAwait(false);
                stream = ssl;
            }

            _stream = stream;
            _reader = new NntpTextReader(stream, leaveOpen: true);

            var banner = await ReadResponseAsync(timeoutCts.Token).ConfigureAwait(false);
            if (banner.Code is not (200 or 201))
            {
                throw new NntpException(banner.Code, banner.Message);
            }

            return banner;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out connecting to {_options.Host}:{_options.Port}.");
        }
    }

    public async Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var bytes = Encoding.UTF8.GetBytes(line + "\r\n");
        await _stream!.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<NntpResponse> ReadResponseAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var line = await _reader!.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (line is null)
        {
            throw new IOException("The NNTP server closed the connection.");
        }

        return NntpResponse.Parse(line);
    }

    public async Task<IReadOnlyList<string>> ReadMultilineAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var lines = new List<string>();
        await foreach (var line in _reader!.ReadMultilineAsync(cancellationToken).ConfigureAwait(false))
        {
            lines.Add(line);
        }

        return lines;
    }

    private bool ValidateCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        return _options.AllowInsecureTls;
    }

    private void EnsureConnected()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected)
        {
            throw new InvalidOperationException("The NNTP connection is not open.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_reader is not null)
        {
            await _reader.DisposeAsync().ConfigureAwait(false);
        }

        if (_stream is not null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }

        _client?.Dispose();
    }
}
