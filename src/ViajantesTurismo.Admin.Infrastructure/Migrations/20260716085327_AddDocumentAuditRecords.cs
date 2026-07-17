using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentAuditRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentAuditRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentRevision = table.Column<int>(type: "integer", nullable: true),
                    Operation = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetentionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditRecords_RetentionExpiresAt",
                table: "DocumentAuditRecords",
                column: "RetentionExpiresAt");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public."PreventDocumentAuditRecordMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'UPDATE' THEN
                        RAISE EXCEPTION 'Document audit records are immutable.';
                    END IF;

                    IF OLD."RetentionExpiresAt" > CURRENT_TIMESTAMP THEN
                        RAISE EXCEPTION 'Document audit records cannot be deleted before their retention period expires.';
                    END IF;

                    RETURN OLD;
                END;
                $$;

                CREATE TRIGGER "TR_DocumentAuditRecords_RejectMutation"
                BEFORE UPDATE OR DELETE ON "DocumentAuditRecords"
                FOR EACH ROW EXECUTE FUNCTION public."PreventDocumentAuditRecordMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_DocumentAuditRecords_RejectMutation" ON "DocumentAuditRecords";
                DROP FUNCTION IF EXISTS public."PreventDocumentAuditRecordMutation"();
                """);

            migrationBuilder.DropTable(
                name: "DocumentAuditRecords");
        }
    }
}
