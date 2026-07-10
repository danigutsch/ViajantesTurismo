using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialBranding : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BrandingSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BrandName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                AccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                TextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                HeadingFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                BodyFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                LogoUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BrandingSettings", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BrandingSettings");
    }
}
