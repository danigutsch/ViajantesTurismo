using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecordFinalizedArtifactIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinalizedArtifactLength",
                table: "DocumentDrafts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalizedArtifactSha256",
                table: "DocumentDrafts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // Keep the artifact bodies untouched, including grandfathered bodies above the current write limit.
            migrationBuilder.Sql("""
                UPDATE "DocumentDrafts" AS revision
                SET "FinalizedArtifactLength" = octet_length(body."Content"),
                    "FinalizedArtifactSha256" = encode(sha256(body."Content"), 'hex')
                FROM "DocumentArtifactBodies" AS body
                WHERE revision."Id" = body."DocumentId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizedArtifactLength",
                table: "DocumentDrafts");

            migrationBuilder.DropColumn(
                name: "FinalizedArtifactSha256",
                table: "DocumentDrafts");
        }
    }
}
