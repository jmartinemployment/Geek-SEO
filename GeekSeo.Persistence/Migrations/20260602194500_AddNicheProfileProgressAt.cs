using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNicheProfileProgressAt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "AnalysisProgressAt",
            schema: "geek_seo",
            table: "niche_profiles",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AnalysisProgressAt",
            schema: "geek_seo",
            table: "niche_profiles");
    }
}
