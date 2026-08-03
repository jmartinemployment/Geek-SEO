using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSiteAnalysisProfileAnalysisStep : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AnalysisStep",
            schema: "geek_seo",
            table: "site_analysis_profiles",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "AnalysisStepNumber",
            schema: "geek_seo",
            table: "site_analysis_profiles",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "AnalysisTotalSteps",
            schema: "geek_seo",
            table: "site_analysis_profiles",
            type: "integer",
            nullable: false,
            defaultValue: 10);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AnalysisStep", schema: "geek_seo", table: "site_analysis_profiles");
        migrationBuilder.DropColumn(name: "AnalysisStepNumber", schema: "geek_seo", table: "site_analysis_profiles");
        migrationBuilder.DropColumn(name: "AnalysisTotalSteps", schema: "geek_seo", table: "site_analysis_profiles");
    }
}
