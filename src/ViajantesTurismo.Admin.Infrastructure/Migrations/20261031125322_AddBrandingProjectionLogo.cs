using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingProjectionLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "LogoContent",
                schema: "branding_projection",
                table: "settings",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                schema: "branding_projection",
                table: "settings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoSha256",
                schema: "branding_projection",
                table: "settings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoContent",
                schema: "branding_projection",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "LogoContentType",
                schema: "branding_projection",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "LogoSha256",
                schema: "branding_projection",
                table: "settings");
        }
    }
}
