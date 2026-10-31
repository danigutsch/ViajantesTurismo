using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StampDocumentAuditLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentLineageId",
                table: "DocumentAuditRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("ALTER TABLE \"DocumentAuditRecords\" DISABLE TRIGGER \"TR_DocumentAuditRecords_RejectMutation\";");
            migrationBuilder.Sql(
                "UPDATE \"DocumentAuditRecords\" AS audit SET \"DocumentLineageId\" = draft.\"DocumentLineageId\" FROM \"DocumentDrafts\" AS draft WHERE audit.\"DocumentId\" = draft.\"Id\";");
            migrationBuilder.Sql("ALTER TABLE \"DocumentAuditRecords\" ENABLE TRIGGER \"TR_DocumentAuditRecords_RejectMutation\";");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditRecords_DocumentLineageId",
                table: "DocumentAuditRecords",
                column: "DocumentLineageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentAuditRecords_DocumentLineageId",
                table: "DocumentAuditRecords");

            migrationBuilder.DropColumn(
                name: "DocumentLineageId",
                table: "DocumentAuditRecords");
        }
    }
}
