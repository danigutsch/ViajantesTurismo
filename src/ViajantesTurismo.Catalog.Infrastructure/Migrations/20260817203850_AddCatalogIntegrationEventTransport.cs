using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogIntegrationEventTransport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transport_messages",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsumeAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastConsumeAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextConsumeAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastConsumeError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ClaimedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClaimedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EnvelopeSpec = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnvelopeSpecVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: true),
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DataSchema = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    PayloadEncoding = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExtensionAttributesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transport_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transport_messages_ConsumerName_EventId",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transport_messages_ConsumerName_ProcessedAt_NextConsumeAtte~",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "ProcessedAt", "NextConsumeAttemptAt", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transport_messages",
                schema: "messaging");
        }
    }
}
