using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ObserveCatalogCompletedInboxDuplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_event_inbox_duplicates",
                schema: "messaging",
                columns: table => new
                {
                    Scope = table.Column<string>(type: "character varying(200)", nullable: false),
                    Key = table.Column<string>(type: "character varying(255)", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuplicateCount = table.Column<int>(type: "integer", nullable: false),
                    LastObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_event_inbox_duplicates", x => new { x.Scope, x.Key });
                    table.ForeignKey(
                        name: "FK_integration_event_inbox_duplicates_idempotency_keys_Scope_K~",
                        columns: x => new { x.Scope, x.Key },
                        principalSchema: "messaging",
                        principalTable: "idempotency_keys",
                        principalColumns: new[] { "Scope", "Key" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_integration_event_inbox_duplicates_ConsumerName_EventId",
                schema: "messaging",
                table: "integration_event_inbox_duplicates",
                columns: new[] { "ConsumerName", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_event_inbox_duplicates_ConsumerName_LastObserve~",
                schema: "messaging",
                table: "integration_event_inbox_duplicates",
                columns: new[] { "ConsumerName", "LastObservedAt", "EventId" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_event_inbox_duplicates",
                schema: "messaging");
        }
    }
}
