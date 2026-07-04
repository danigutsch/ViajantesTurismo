using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreMediaObjectKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceObjectKey",
                table: "PublicMediaImages",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "PublicMediaImageResponsiveVariants",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.DropColumn(
                name: "SourceUri",
                table: "PublicMediaImages");

            migrationBuilder.DropColumn(
                name: "Uri",
                table: "PublicMediaImageResponsiveVariants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceUri",
                table: "PublicMediaImages",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "Uri",
                table: "PublicMediaImageResponsiveVariants",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.DropColumn(
                name: "SourceObjectKey",
                table: "PublicMediaImages");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "PublicMediaImageResponsiveVariants");
        }
    }
}
