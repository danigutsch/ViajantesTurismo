using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentBrandingTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandingAccentColor",
                table: "DocumentDrafts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandingBackgroundColor",
                table: "DocumentDrafts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandingBodyFontFamily",
                table: "DocumentDrafts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandingFooterText",
                table: "DocumentDrafts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandingHeadingFontFamily",
                table: "DocumentDrafts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandingPrimaryColor",
                table: "DocumentDrafts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandingTextColor",
                table: "DocumentDrafts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandingAccentColor",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "BrandingBackgroundColor",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "BrandingBodyFontFamily",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "BrandingFooterText",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "BrandingHeadingFontFamily",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "BrandingPrimaryColor",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "BrandingTextColor",
                table: "DocumentDrafts");
        }
    }
}
