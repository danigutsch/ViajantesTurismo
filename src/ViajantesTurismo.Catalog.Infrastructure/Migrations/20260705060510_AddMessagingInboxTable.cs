using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingInboxTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS messaging.idempotency_keys (
                    "Scope" character varying(200) NOT NULL,
                    "Key" character varying(255) NOT NULL,
                    "State" character varying(32) NOT NULL,
                    "StartedAt" timestamp with time zone NOT NULL,
                    "CompletedAt" timestamp with time zone NULL,
                    "ResultFingerprint" character varying(512) NULL,
                    CONSTRAINT "PK_idempotency_keys" PRIMARY KEY ("Scope", "Key")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS messaging.idempotency_keys;");
        }
    }
}
