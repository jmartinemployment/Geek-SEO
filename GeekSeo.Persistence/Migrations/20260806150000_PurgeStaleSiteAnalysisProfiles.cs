using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <summary>
/// Purges pre-tree SiteAnalysis profiles. All rows with AnalysisVersion IS DISTINCT FROM '2.0'
/// (including NULL) are pre-tree / stale and are deleted. Cascades remove child rows
/// (pillars→subtopics, headings, page_section_trees, step_runs, etc.) via FK Cascade
/// verified in SeoDbContext.Extensions.cs. One-way: Down is no-op, rows cannot be restored.
/// </summary>
public partial class PurgeStaleSiteAnalysisProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "geek_seo"."site_analysis_profiles"
            WHERE "AnalysisVersion" IS DISTINCT FROM '2.0';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible: deleted rows cannot be restored.
    }
}
