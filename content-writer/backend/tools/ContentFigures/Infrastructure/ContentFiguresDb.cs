using ContentWriter.Infrastructure.Data;
using ContentWriter.Infrastructure.Repositories;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

namespace ContentFigures.Infrastructure;

public static class ContentFiguresDb
{
    public static ContentWriterDbContext CreateContext()
    {
        Env.TraversePath().Load();

        var rawUrl = Environment.GetEnvironmentVariable("CONTENT_WRITER_DATABASE_URL");
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            throw new InvalidOperationException(
                "CONTENT_WRITER_DATABASE_URL is required for ContentFigures CLI.");
        }

        var connectionString = DatabaseConnectionResolver.NormalizePostgresUrl(rawUrl);
        var optionsBuilder = new DbContextOptionsBuilder<ContentWriterDbContext>();
        optionsBuilder.UseContentWriterPostgres(connectionString);
        return new ContentWriterDbContext(optionsBuilder.Options);
    }
}
