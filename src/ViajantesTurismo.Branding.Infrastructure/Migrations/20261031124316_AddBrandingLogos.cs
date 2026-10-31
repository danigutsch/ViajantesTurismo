using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingLogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoSha256",
                schema: "branding",
                table: "BrandingSettingsRevisions",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoSha256",
                schema: "branding",
                table: "BrandingSettings",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BrandingLogos",
                schema: "branding",
                columns: table => new
                {
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    StoredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingLogos", x => x.Sha256);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandingSettingsRevisions_LogoSha256",
                schema: "branding",
                table: "BrandingSettingsRevisions",
                column: "LogoSha256");

            migrationBuilder.CreateIndex(
                name: "IX_BrandingSettings_LogoSha256",
                schema: "branding",
                table: "BrandingSettings",
                column: "LogoSha256");

            migrationBuilder.AddForeignKey(
                name: "FK_BrandingSettings_BrandingLogos_LogoSha256",
                schema: "branding",
                table: "BrandingSettings",
                column: "LogoSha256",
                principalSchema: "branding",
                principalTable: "BrandingLogos",
                principalColumn: "Sha256",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BrandingSettingsRevisions_BrandingLogos_LogoSha256",
                schema: "branding",
                table: "BrandingSettingsRevisions",
                column: "LogoSha256",
                principalSchema: "branding",
                principalTable: "BrandingLogos",
                principalColumn: "Sha256",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                "UPDATE branding.\"BrandingSettings\" SET \"LogoUri\" = NULL WHERE \"LogoSha256\" IS NULL AND \"LogoUri\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrandingSettings_BrandingLogos_LogoSha256",
                schema: "branding",
                table: "BrandingSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_BrandingSettingsRevisions_BrandingLogos_LogoSha256",
                schema: "branding",
                table: "BrandingSettingsRevisions");

            migrationBuilder.DropTable(
                name: "BrandingLogos",
                schema: "branding");

            migrationBuilder.DropIndex(
                name: "IX_BrandingSettingsRevisions_LogoSha256",
                schema: "branding",
                table: "BrandingSettingsRevisions");

            migrationBuilder.DropIndex(
                name: "IX_BrandingSettings_LogoSha256",
                schema: "branding",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "LogoSha256",
                schema: "branding",
                table: "BrandingSettingsRevisions");

            migrationBuilder.DropColumn(
                name: "LogoSha256",
                schema: "branding",
                table: "BrandingSettings");
        }
    }
}
