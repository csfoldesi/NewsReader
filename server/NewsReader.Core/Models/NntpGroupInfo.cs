namespace NewsReader.Core.Models;

public sealed record NntpGroupInfo(string Name, long EstimatedCount, long FirstArticle, long LastArticle);
