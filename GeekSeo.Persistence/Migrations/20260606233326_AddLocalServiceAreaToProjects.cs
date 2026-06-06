using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <inheritdoc />
public partial class AddLocalServiceAreaToProjects : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BusinessAddress",
            schema: "geek_seo",
            table: "seo_projects",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "LocalSeoEnabled",
            schema: "geek_seo",
            table: "seo_projects",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "ServiceRadiusMiles",
            schema: "geek_seo",
            table: "seo_projects",
            type: "integer",
            nullable: false,
            defaultValue: 20);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BusinessAddress",
            schema: "geek_seo",
            table: "seo_projects");

        migrationBuilder.DropColumn(
            name: "LocalSeoEnabled",
            schema: "geek_seo",
            table: "seo_projects");

        migrationBuilder.DropColumn(
            name: "ServiceRadiusMiles",
            schema: "geek_seo",
            table: "seo_projects");
    }
}
