using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMediaUploadFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadFingerprint",
                table: "PublicMediaImages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicMediaImages_UploadFingerprint",
                table: "PublicMediaImages",
                column: "UploadFingerprint",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublicMediaImages_UploadFingerprint",
                table: "PublicMediaImages");

            migrationBuilder.DropColumn(
                name: "UploadFingerprint",
                table: "PublicMediaImages");
        }
    }
}
