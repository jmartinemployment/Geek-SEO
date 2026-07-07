using ContentWriter.Infrastructure.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.TraversePath().Load();

var sqlitePath = args.FirstOrDefault(a => !a.StartsWith('-'))
    ?? Path.Combine(
        Directory.GetCurrentDirectory(),
        "src", "ContentWriter.Api", "contentwriter.db");

sqlitePath = Path.GetFullPath(sqlitePath);
var replaceExisting = args.Contains("--replace", StringComparer.OrdinalIgnoreCase);

var rawUrl = Environment.GetEnvironmentVariable("CONTENT_WRITER_DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrWhiteSpace(rawUrl))
{
    Console.Error.WriteLine("Set CONTENT_WRITER_DATABASE_URL or DATABASE_URL to your Supabase Postgres URI.");
    return 1;
}

var connectionString = DatabaseConnectionResolver.NormalizePostgresUrl(rawUrl);
var optionsBuilder = new DbContextOptionsBuilder<ContentWriterDbContext>();
optionsBuilder.UseContentWriterPostgres(connectionString);

await using var postgres = new ContentWriterDbContext(optionsBuilder.Options);

Console.WriteLine($"Importing from SQLite: {sqlitePath}");
Console.WriteLine($"Target schema: {ContentWriterDbContextOptionsExtensions.SchemaName}");

try
{
    var result = await SqliteToPostgresImporter.ImportAsync(sqlitePath, postgres, replaceExisting);
    Console.WriteLine(
        $"Done — projects={result.Projects}, crawled={result.CrawledSites}, sources={result.KeywordSources}, content={result.GeneratedContents}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    if (ex.InnerException != null)
        Console.Error.WriteLine($"Inner: {ex.InnerException.Message}");
    return 1;
}
