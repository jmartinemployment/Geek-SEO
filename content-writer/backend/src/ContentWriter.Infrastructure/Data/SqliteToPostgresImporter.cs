using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContentWriter.Infrastructure.Data;

public sealed record SqliteImportResult(
    int Projects,
    int CrawledSites,
    int KeywordSources,
    int GeneratedContents);

/// <summary>
/// One-time copy from the legacy local SQLite file (no schema prefix) into Supabase Postgres.
/// </summary>
public static class SqliteToPostgresImporter
{
    private static readonly char ListSeparator = (char)0x1F;

    public static async Task<SqliteImportResult> ImportAsync(
        string sqlitePath,
        ContentWriterDbContext postgres,
        bool replaceExisting = false,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sqlitePath))
            throw new FileNotFoundException("SQLite database not found.", sqlitePath);

        await postgres.Database.MigrateAsync(cancellationToken);

        var existingProjects = await postgres.Projects.CountAsync(cancellationToken);
        if (existingProjects > 0 && !replaceExisting)
        {
            throw new InvalidOperationException(
                $"Postgres already has {existingProjects} project(s). Pass replaceExisting=true to clear and re-import.");
        }

        if (replaceExisting && existingProjects > 0)
        {
            await postgres.Database.ExecuteSqlRawAsync(
                """
                TRUNCATE TABLE content_writer."GeneratedContents",
                             content_writer."KeywordSources",
                             content_writer."CrawledSites",
                             content_writer."Projects"
                CASCADE;
                """,
                cancellationToken);
        }

        await using var sqlite = new SqliteConnection($"Data Source={sqlitePath}");
        await sqlite.OpenAsync(cancellationToken);

        var projects = await ReadProjectsAsync(sqlite, cancellationToken);
        var crawledSites = await ReadCrawledSitesAsync(sqlite, cancellationToken);
        var keywordSources = await ReadKeywordSourcesAsync(sqlite, cancellationToken);
        var generatedContents = await ReadGeneratedContentsAsync(sqlite, cancellationToken);

        postgres.Projects.AddRange(projects);
        postgres.CrawledSites.AddRange(crawledSites);
        postgres.KeywordSources.AddRange(keywordSources);
        postgres.GeneratedContents.AddRange(generatedContents);
        await postgres.SaveChangesAsync(cancellationToken);

        return new SqliteImportResult(
            projects.Count,
            crawledSites.Count,
            keywordSources.Count,
            generatedContents.Count);
    }

    private static List<string> SplitList(string? value) =>
        string.IsNullOrEmpty(value) ? [] : value.Split(ListSeparator, StringSplitOptions.None).ToList();

    private static async Task<List<Project>> ReadProjectsAsync(SqliteConnection sqlite, CancellationToken ct)
    {
        var results = new List<Project>();
        await using var cmd = sqlite.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, Name, ProjectUrl, TargetKeyword, Status, PreferredProvider, CreatedAtUtc, UpdatedAtUtc
            FROM Projects
            """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new Project
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                ProjectUrl = reader.GetString(2),
                TargetKeyword = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Status = (ProjectStatus)reader.GetInt32(4),
                PreferredProvider = (LlmProviderType)reader.GetInt32(5),
                CreatedAtUtc = ReadDateTime(reader, 6),
                UpdatedAtUtc = reader.IsDBNull(7) ? null : ReadDateTime(reader, 7),
            });
        }

        return results;
    }

    private static async Task<List<CrawledSite>> ReadCrawledSitesAsync(SqliteConnection sqlite, CancellationToken ct)
    {
        var results = new List<CrawledSite>();
        await using var cmd = sqlite.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, ProjectId, SourceUrl, SiteName, JsonLdBlocks, Headings, Paragraphs,
                   DetectedTone, DetectedFocus, PagesCrawled, CrawledAtUtc
            FROM CrawledSites
            """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new CrawledSite
            {
                Id = reader.GetGuid(0),
                ProjectId = reader.GetGuid(1),
                SourceUrl = reader.GetString(2),
                SiteName = reader.GetString(3),
                JsonLdBlocks = SplitList(reader.IsDBNull(4) ? null : reader.GetString(4)),
                Headings = SplitList(reader.IsDBNull(5) ? null : reader.GetString(5)),
                Paragraphs = SplitList(reader.IsDBNull(6) ? null : reader.GetString(6)),
                DetectedTone = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                DetectedFocus = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                PagesCrawled = reader.GetInt32(9),
                CrawledAtUtc = ReadDateTime(reader, 10),
            });
        }

        return results;
    }

    private static async Task<List<KeywordSource>> ReadKeywordSourcesAsync(SqliteConnection sqlite, CancellationToken ct)
    {
        var results = new List<KeywordSource>();
        await using var cmd = sqlite.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, ProjectId, Category, OriginalFileName, RawContent, ExtractedTitle,
                   ExtractedHeadings, ExtractedParagraphs, ExtractedQuestions, UploadedAtUtc
            FROM KeywordSources
            """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new KeywordSource
            {
                Id = reader.GetGuid(0),
                ProjectId = reader.GetGuid(1),
                Category = (KeywordSourceCategory)reader.GetInt32(2),
                OriginalFileName = reader.GetString(3),
                RawContent = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                ExtractedTitle = reader.IsDBNull(5) ? null : reader.GetString(5),
                ExtractedHeadings = SplitList(reader.IsDBNull(6) ? null : reader.GetString(6)),
                ExtractedParagraphs = SplitList(reader.IsDBNull(7) ? null : reader.GetString(7)),
                ExtractedQuestions = SplitList(reader.IsDBNull(8) ? null : reader.GetString(8)),
                UploadedAtUtc = ReadDateTime(reader, 9),
            });
        }

        return results;
    }

    private static async Task<List<GeneratedContent>> ReadGeneratedContentsAsync(SqliteConnection sqlite, CancellationToken ct)
    {
        var results = new List<GeneratedContent>();
        await using var cmd = sqlite.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, ProjectId, ContentType, Title, Slug, BodyHtml, MetaDescription, Keywords,
                   WordCount, SectionOutline, JsonLdSchema, RelatedArticleUrl,
                   GeneratedByProvider, GeneratedByModel, CreatedAtUtc
            FROM GeneratedContents
            """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new GeneratedContent
            {
                Id = reader.GetGuid(0),
                ProjectId = reader.GetGuid(1),
                ContentType = (GeneratedContentType)reader.GetInt32(2),
                Title = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Slug = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                BodyHtml = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                MetaDescription = reader.IsDBNull(6) ? null : reader.GetString(6),
                Keywords = SplitList(reader.IsDBNull(7) ? null : reader.GetString(7)),
                WordCount = reader.GetInt32(8),
                SectionOutline = SplitList(reader.IsDBNull(9) ? null : reader.GetString(9)),
                JsonLdSchema = reader.IsDBNull(10) ? null : reader.GetString(10),
                RelatedArticleUrl = reader.IsDBNull(11) ? null : reader.GetString(11),
                GeneratedByProvider = (LlmProviderType)reader.GetInt32(12),
                GeneratedByModel = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                CreatedAtUtc = ReadDateTime(reader, 14),
            });
        }

        return results;
    }

    private static DateTime ReadDateTime(SqliteDataReader reader, int ordinal)
    {
        var value = reader.GetString(ordinal);
        var parsed = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return parsed.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : parsed.ToUniversalTime();
    }
}
