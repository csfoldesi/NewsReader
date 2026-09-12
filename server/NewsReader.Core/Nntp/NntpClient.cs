using NewsReader.Core.Interfaces;
using NewsReader.Core.Models;

namespace NewsReader.Core.Nntp;

public sealed class NntpClient : INntpClient
{
    private readonly INntpConnection _connection;
    private readonly NntpConnectionOptions? _options;

    public NntpClient(INntpConnection connection, NntpConnectionOptions? options = null)
    {
        _connection = connection;
        _options = options;
    }

    public NntpClient(NntpConnectionOptions options)
        : this(new NntpConnection(options), options)
    {
    }

    public bool IsConnected => _connection.IsConnected;

    public NntpResponse? Banner { get; private set; }

    public async Task<NntpResponse> ConnectAsync(CancellationToken cancellationToken = default)
    {
        Banner = await _connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(_options?.Username))
        {
            await AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
        }

        return Banner;
    }

    public async Task AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        await _connection.WriteLineAsync($"AUTHINFO USER {username}", cancellationToken).ConfigureAwait(false);
        var response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);

        if (response.Code == 281)
        {
            return;
        }

        if (response.Code != 381)
        {
            throw new NntpException(response.Code, response.Message);
        }

        await _connection.WriteLineAsync($"AUTHINFO PASS {password}", cancellationToken).ConfigureAwait(false);
        response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);

        if (response.Code != 281)
        {
            throw new NntpException(response.Code, response.Message);
        }
    }

    public async Task<NntpCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        await _connection.WriteLineAsync("CAPABILITIES", cancellationToken).ConfigureAwait(false);
        var response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);

        if (response.Code != 101)
        {
            return new NntpCapabilities();
        }

        var lines = await _connection.ReadMultilineAsync(cancellationToken).ConfigureAwait(false);
        var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var token = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (token.Length > 0)
            {
                features.Add(token[0]);
            }
        }

        return new NntpCapabilities { Features = features };
    }

    public async Task<IReadOnlyList<NntpGroupListing>> ListGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        await _connection.WriteLineAsync("LIST ACTIVE", cancellationToken).ConfigureAwait(false);
        var response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);
        EnsureMultiline(response, 215);

        var lines = await _connection.ReadMultilineAsync(cancellationToken).ConfigureAwait(false);
        var groups = new List<NntpGroupListing>(lines.Count);

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                continue;
            }

            _ = long.TryParse(parts[1], out var last);
            _ = long.TryParse(parts[2], out var first);
            groups.Add(new NntpGroupListing(parts[0], last, first, parts[3].Equals("y", StringComparison.OrdinalIgnoreCase)));
        }

        return groups;
    }

    public async Task<NntpGroupInfo> GetGroupInfoAsync(string group, CancellationToken cancellationToken = default)
    {
        await _connection.WriteLineAsync($"GROUP {group}", cancellationToken).ConfigureAwait(false);
        var response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);

        if (response.Code != 211)
        {
            throw new NntpException(response.Code, response.Message);
        }

        var parts = response.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            throw new NntpException(response.Code, response.Message);
        }

        _ = long.TryParse(parts[0], out var count);
        _ = long.TryParse(parts[1], out var first);
        _ = long.TryParse(parts[2], out var last);

        return new NntpGroupInfo(parts[3], count, first, last);
    }

    public async Task<IReadOnlyList<long>> ListArticleNumbersAsync(
        string? group = null,
        CancellationToken cancellationToken = default)
    {
        var command = group is null ? "LISTGROUP" : $"LISTGROUP {group}";
        await _connection.WriteLineAsync(command, cancellationToken).ConfigureAwait(false);
        var response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);
        EnsureMultiline(response, 211);

        var lines = await _connection.ReadMultilineAsync(cancellationToken).ConfigureAwait(false);
        var numbers = new List<long>(lines.Count);

        foreach (var line in lines)
        {
            if (long.TryParse(line, out var number))
            {
                numbers.Add(number);
            }
        }

        return numbers;
    }

    public async Task<IReadOnlyList<NntpOverview>> GetOverviewAsync(
        long first,
        long last,
        CancellationToken cancellationToken = default)
    {
        await _connection.WriteLineAsync($"XOVER {first}-{last}", cancellationToken).ConfigureAwait(false);
        var response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);
        EnsureMultiline(response, 224);

        var lines = await _connection.ReadMultilineAsync(cancellationToken).ConfigureAwait(false);
        var overviews = new List<NntpOverview>(lines.Count);

        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length < 1 || !long.TryParse(parts[0], out var number))
            {
                continue;
            }

            overviews.Add(new NntpOverview
            {
                ArticleNumber = number,
                Subject = Field(parts, 1),
                From = Field(parts, 2),
                Date = Field(parts, 3),
                MessageId = Field(parts, 4),
                References = Field(parts, 5),
                Bytes = ParseLong(Field(parts, 6)),
                Lines = ParseLong(Field(parts, 7)),
            });
        }

        return overviews;
    }

    public Task<NntpArticle> GetArticleAsync(long articleNumber, CancellationToken cancellationToken = default) =>
        GetArticleCoreAsync($"ARTICLE {articleNumber}", articleNumber, cancellationToken);

    public Task<NntpArticle> GetHeadersAsync(long articleNumber, CancellationToken cancellationToken = default) =>
        GetArticleCoreAsync($"HEAD {articleNumber}", articleNumber, cancellationToken);

    public Task<NntpArticle> GetBodyAsync(long articleNumber, CancellationToken cancellationToken = default) =>
        GetArticleCoreAsync($"BODY {articleNumber}", articleNumber, cancellationToken);

    private async Task<NntpArticle> GetArticleCoreAsync(
        string command,
        long articleNumber,
        CancellationToken cancellationToken)
    {
        await _connection.WriteLineAsync(command, cancellationToken).ConfigureAwait(false);
        var response = await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);
        EnsureMultiline(response, 220, 221, 222);

        var lines = await _connection.ReadMultilineAsync(cancellationToken).ConfigureAwait(false);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = new List<string>();
        var inBody = false;

        foreach (var line in lines)
        {
            if (!inBody && line.Length == 0)
            {
                inBody = true;
                continue;
            }

            if (inBody)
            {
                body.Add(line);
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon > 0)
            {
                var name = line[..colon];
                var value = line[(colon + 1)..].TrimStart(' ');
                if (headers.TryGetValue(name, out var existing))
                {
                    headers[name] = existing + " " + value;
                }
                else
                {
                    headers[name] = value;
                }
            }
        }

        return new NntpArticle
        {
            ArticleNumber = articleNumber,
            Headers = headers,
            Body = string.Join("\n", body),
            RawText = string.Join("\n", lines),
        };
    }

    public async Task QuitAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return;
        }

        await _connection.WriteLineAsync("QUIT", cancellationToken).ConfigureAwait(false);
        await _connection.ReadResponseAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureMultiline(NntpResponse response, params int[] expectedCodes)
    {
        foreach (var code in expectedCodes)
        {
            if (response.Code == code)
            {
                return;
            }
        }

        throw new NntpException(response.Code, response.Message);
    }

    private static string? Field(string[] parts, int index) =>
        index < parts.Length ? parts[index] : null;

    private static long? ParseLong(string? value) =>
        long.TryParse(value, out var result) ? result : null;

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
