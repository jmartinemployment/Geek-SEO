namespace SiteAnalyzer2.Repositories;

public record PendingSerpRun(Guid Id, Guid ProjectId, string Keyword, string SerpProviderKey);
