using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNicheProfileFusionSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FusionSnapshot",
            schema: "geek_seo",
            table: "niche_profiles",
            type: "jsonb",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FusionSnapshot",
            schema: "geek_seo",
            table: "niche_profiles");
    }
}
