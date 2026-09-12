namespace NewsReader.Core.Models;

public sealed record NntpGroupListing(
    string Name,
    long LastArticle,
    long FirstArticle,
    bool PostingAllowed
);
