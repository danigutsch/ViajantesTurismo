using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingSettingsRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceEpoch",
                schema: "branding",
                table: "BrandingSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "SourceRevision",
                schema: "branding",
                table: "BrandingSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "BrandingSettingsRevisions",
                schema: "branding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevision = table.Column<long>(type: "bigint", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    HeadingFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BodyFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LogoUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CommittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingSettingsRevisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandingSettingsRevisions_SourceEpoch_SourceRevision",
                schema: "branding",
                table: "BrandingSettingsRevisions",
                columns: new[] { "SourceEpoch", "SourceRevision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandingSettingsRevisions",
                schema: "branding");

            migrationBuilder.DropColumn(
                name: "SourceEpoch",
                schema: "branding",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "SourceRevision",
                schema: "branding",
                table: "BrandingSettings");
        }
    }
}
