namespace NewsReader.Core.Models;

public sealed record NntpCapabilities
{
    public IReadOnlySet<string> Features { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool Supports(string feature) => Features.Contains(feature);

    public bool SupportsStartTls => Supports("STARTTLS");
    public bool SupportsReader => Supports("READER");
    public bool SupportsPost => Supports("POST");
    public bool SupportsOverviewFormat => Supports("OVER") || Supports("XOVER");
}
