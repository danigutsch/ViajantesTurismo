using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitDocumentArtifactBodies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasFinalizedArtifact",
                table: "DocumentDrafts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DocumentArtifactBodies",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentArtifactBodies", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_DocumentArtifactBodies_DocumentDrafts_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "DocumentDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Copy sealed artifacts before removing the original column. The new limit applies to subsequent writes.
            migrationBuilder.Sql("""
                INSERT INTO "DocumentArtifactBodies" ("DocumentId", "Content")
                SELECT "Id", "FinalizedArtifactContent" FROM "DocumentDrafts"
                WHERE "FinalizedArtifactContent" IS NOT NULL;

                UPDATE "DocumentDrafts" SET "HasFinalizedArtifact" = TRUE
                WHERE "FinalizedArtifactContent" IS NOT NULL;

                ALTER TABLE "DocumentArtifactBodies"
                ADD CONSTRAINT "CK_DocumentArtifactBodies_ContentLength"
                CHECK (octet_length("Content") BETWEEN 1 AND 1048576) NOT VALID;
                """);

            migrationBuilder.DropColumn(
                name: "FinalizedArtifactContent",
                table: "DocumentDrafts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FinalizedArtifactContent",
                table: "DocumentDrafts",
                type: "bytea",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "DocumentDrafts" AS draft
                SET "FinalizedArtifactContent" = body."Content"
                FROM "DocumentArtifactBodies" AS body
                WHERE draft."Id" = body."DocumentId";
                """);

            migrationBuilder.DropTable(
                name: "DocumentArtifactBodies");

            migrationBuilder.DropColumn(
                name: "HasFinalizedArtifact",
                table: "DocumentDrafts");
        }
    }
}
