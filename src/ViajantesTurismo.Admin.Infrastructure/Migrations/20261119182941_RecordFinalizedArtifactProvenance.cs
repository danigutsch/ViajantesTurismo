using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecordFinalizedArtifactProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FinalizedArtifactContentType",
                table: "DocumentDrafts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalizedArtifactRendererVersion",
                table: "DocumentDrafts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalizedArtifactRetentionPolicy",
                table: "DocumentDrafts",
                type: "text",
                nullable: true);

            // Record the policy currently governing older artifacts without inventing their render provenance.
            migrationBuilder.Sql("""
                UPDATE "DocumentDrafts"
                SET "FinalizedArtifactRetentionPolicy" = 'DOC-RET-1.0'
                WHERE "FinalizedAt" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizedArtifactContentType",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "FinalizedArtifactRendererVersion",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "FinalizedArtifactRetentionPolicy",
                table: "DocumentDrafts");
        }
    }
}
