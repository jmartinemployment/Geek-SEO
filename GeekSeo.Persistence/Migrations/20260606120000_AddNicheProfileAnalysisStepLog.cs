using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNicheProfileAnalysisStepLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AnalysisStepLog",
            schema: "geek_seo",
            table: "niche_profiles",
            type: "jsonb",
            nullable: false,
            defaultValueSql: "'[]'::jsonb");

        migrationBuilder.AddColumn<int>(
            name: "AnalysisStepLogVersion",
            schema: "geek_seo",
            table: "niche_profiles",
            type: "integer",
            nullable: false,
            defaultValue: 1);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AnalysisStepLog",
            schema: "geek_seo",
            table: "niche_profiles");

        migrationBuilder.DropColumn(
            name: "AnalysisStepLogVersion",
            schema: "geek_seo",
            table: "niche_profiles");
    }
}
