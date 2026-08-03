using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <summary>
/// Historical cutover migration. Schema now uses site_analysis_* / focus naming
/// in earlier migrations; this step is intentionally a no-op.
/// Migration id retained for environments that already applied it.
/// </summary>
public partial class RenameLegacySiteAnalysisCutover : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No-op: table/column renames are reflected in earlier migration sources.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op.
    }
}
