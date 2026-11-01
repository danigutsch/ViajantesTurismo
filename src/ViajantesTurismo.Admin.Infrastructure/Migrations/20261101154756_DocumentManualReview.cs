using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentManualReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManualReviewReasons",
                table: "DocumentDrafts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualReview",
                table: "DocumentDrafts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManualReviewReasons",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "RequiresManualReview",
                table: "DocumentDrafts");
        }
    }
}
