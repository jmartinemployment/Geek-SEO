using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SiteAnalyzer2.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true);

        var userSecretsPath = ResolveUserSecretsPath(DatabaseConnection.UserSecretsId);
        if (userSecretsPath is not null)
            configBuilder.AddJsonFile(userSecretsPath, optional: true, reloadOnChange: false);

        var config = configBuilder
            .AddEnvironmentVariables()
            .Build();

        var connectionString = DatabaseConnection.ResolveRequired(config);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "sa2"))
            .Options;

        return new AppDbContext(options);
    }

    private static string? ResolveUserSecretsPath(string userSecretsId)
    {
        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft",
                "UserSecrets",
                userSecretsId,
                "secrets.json"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".microsoft",
                "usersecrets",
                userSecretsId,
                "secrets.json"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
