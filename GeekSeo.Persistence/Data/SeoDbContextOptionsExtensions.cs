using Microsoft.EntityFrameworkCore;

namespace GeekSeo.Persistence.Data;

public static class SeoDbContextOptionsExtensions
{
    public const string SchemaName = "geek_seo";
    public const string MigrationsHistoryTableName = "__EFSeoMigrationsHistory";

    /// <summary>Runtime queries — PascalCase columns match EF migrations applied to <c>geek_seo.*</c>.</summary>
    public static DbContextOptionsBuilder<SeoDbContext> UseGeekSeoDatabase(
        this DbContextOptionsBuilder<SeoDbContext> builder,
        string connectionString) =>
        builder
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(MigrationsHistoryTableName, SchemaName));

    /// <summary>
    /// Applying EF migrations only. Omits snake_case so <c>__EFSeoMigrationsHistory</c> keeps
    /// <c>MigrationId</c> / <c>ProductVersion</c> columns (see EFCore.NamingConventions issue #1).
    /// </summary>
    public static DbContextOptionsBuilder<SeoDbContext> UseGeekSeoDatabaseMigrations(
        this DbContextOptionsBuilder<SeoDbContext> builder,
        string connectionString) =>
        builder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable(MigrationsHistoryTableName, SchemaName));
}
