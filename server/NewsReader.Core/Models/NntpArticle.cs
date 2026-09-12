namespace NewsReader.Core.Models;

public sealed record NntpArticle
{
    public required long ArticleNumber { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public string Body { get; init; } = string.Empty;
    public string RawText { get; init; } = string.Empty;

    public string? GetHeader(string name) =>
        Headers.TryGetValue(name, out var value) ? value : null;
}
