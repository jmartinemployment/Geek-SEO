using Microsoft.EntityFrameworkCore;

namespace ContentWriterV3.Infrastructure.Data;

public static class ContentWriterV3DbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseContentWriterV3(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
    {
        return optionsBuilder.UseNpgsql(
            connectionString,
            options => options.MigrationsHistoryTable(
                "__EFMigrationsHistory",
                ContentWriterV3DbContext.SchemaName));
    }
}
