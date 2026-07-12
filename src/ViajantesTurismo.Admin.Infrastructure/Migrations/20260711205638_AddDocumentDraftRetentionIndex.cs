using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDraftRetentionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentDrafts_RetentionExpiresAt_Unfinalized",
                table: "DocumentDrafts",
                column: "RetentionExpiresAt",
                filter: "\"FinalizedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentDrafts_RetentionExpiresAt_Unfinalized",
                table: "DocumentDrafts");
        }
    }
}
