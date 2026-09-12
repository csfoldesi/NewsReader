namespace NewsReader.Core.Models;

public sealed record NntpOverview
{
    public required long ArticleNumber { get; init; }
    public string? Subject { get; init; }
    public string? From { get; init; }
    public string? Date { get; init; }
    public string? MessageId { get; init; }
    public string? References { get; init; }
    public long? Bytes { get; init; }
    public long? Lines { get; init; }
}
