namespace NewsReader.Core.Nntp;

public sealed record NntpConnectionOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = 119;
    public bool UseTls { get; init; }
    public bool AllowInsecureTls { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public string ClientName { get; init; } = "NewsReader";
    public string? Username { get; init; }
    public string? Password { get; init; }
}
