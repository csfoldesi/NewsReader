using System.Text;
using NewsReader.Core.Interfaces;

namespace NewsReader.Core.Decoding;

public sealed class NntpTextReader : INntpTextReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer = new byte[8192];
    private int _start;
    private int _end;
    private readonly bool _leaveOpen;

    public NntpTextReader(Stream stream, bool leaveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    public async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        var line = await ReadLineRawAsync(cancellationToken).ConfigureAwait(false);
        if (line is null)
        {
            return null;
        }

        return line;
    }

    public async IAsyncEnumerable<string> ReadMultilineAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var line = await ReadLineRawAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            if (line == ".")
            {
                yield break;
            }

            if (line.StartsWith("..", StringComparison.Ordinal))
            {
                line = line[1..];
            }

            yield return line;
        }
    }

    private async ValueTask<string?> ReadLineRawAsync(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        while (true)
        {
            if (_start >= _end)
            {
                _start = 0;
                _end = await _stream.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (_end == 0)
                {
                    return sb.Length > 0 ? sb.ToString() : null;
                }
            }

            var span = _buffer.AsSpan(_start, _end - _start);
            var lf = span.IndexOf((byte)'\n');
            if (lf < 0)
            {
                sb.Append(Encoding.UTF8.GetString(span));
                _start = _end;
                continue;
            }

            var lineSpan = span[..lf];
            if (lineSpan.Length > 0 && lineSpan[^1] == (byte)'\r')
            {
                lineSpan = lineSpan[..^1];
            }

            sb.Append(Encoding.UTF8.GetString(lineSpan));
            _start += lf + 1;
            return sb.ToString();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
