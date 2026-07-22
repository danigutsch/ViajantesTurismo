using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedAdminIdempotencyKeys : Migration
    {
        private const string MessagingSchema = "messaging";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF cannot scaffold the data-safety precondition for dropping an obsolete table.
            migrationBuilder.Sql(
                """
                LOCK TABLE messaging.idempotency_keys IN ACCESS EXCLUSIVE MODE;

                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM messaging.idempotency_keys LIMIT 1) THEN
                        RAISE EXCEPTION 'Cannot remove messaging.idempotency_keys because unexpected rows exist.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: MessagingSchema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: MessagingSchema,
                columns: table => new
                {
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultFingerprint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => new { x.Scope, x.Key });
                });
        }
    }
}
