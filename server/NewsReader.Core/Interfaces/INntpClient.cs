using NewsReader.Core.Models;
using NewsReader.Core.Nntp;

namespace NewsReader.Core.Interfaces;

public interface INntpClient : IAsyncDisposable
{
    bool IsConnected { get; }

    NntpResponse? Banner { get; }

    Task<NntpResponse> ConnectAsync(CancellationToken cancellationToken = default);

    Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<NntpCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NntpGroupListing>> ListGroupsAsync(CancellationToken cancellationToken = default);

    Task<NntpGroupInfo> GetGroupInfoAsync(string group, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<long>> ListArticleNumbersAsync(string? group = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NntpOverview>> GetOverviewAsync(long first, long last, CancellationToken cancellationToken = default);

    Task<NntpArticle> GetArticleAsync(long articleNumber, CancellationToken cancellationToken = default);

    Task<NntpArticle> GetHeadersAsync(long articleNumber, CancellationToken cancellationToken = default);

    Task<NntpArticle> GetBodyAsync(long articleNumber, CancellationToken cancellationToken = default);

    Task QuitAsync(CancellationToken cancellationToken = default);
}
