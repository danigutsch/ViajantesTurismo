using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueDocumentRevisionsAndRestoreIdempotency : Migration
    {
        private const string MessagingSchema = "messaging";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: MessagingSchema,
                columns: table => new
                {
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultFingerprint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => new { x.Scope, x.Key });
                });

            migrationBuilder.CreateIndex(
                name: "UX_DocumentDrafts_BookingId_Type_Revision",
                table: "DocumentDrafts",
                columns: new[] { "BookingId", "Type", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: MessagingSchema);

            migrationBuilder.DropIndex(
                name: "UX_DocumentDrafts_BookingId_Type_Revision",
                table: "DocumentDrafts");
        }
    }
}
