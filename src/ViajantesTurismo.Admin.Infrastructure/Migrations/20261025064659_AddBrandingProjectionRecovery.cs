using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingProjectionRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recovery",
                schema: "branding_projection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AuthorizedEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizedEpoch = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizedRevision = table.Column<long>(type: "bigint", nullable: true),
                    AuthorizationReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IncidentReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AuthorizedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AuthorizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recovery_audit",
                schema: "branding_projection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OldEpoch = table.Column<Guid>(type: "uuid", nullable: true),
                    OldRevision = table.Column<long>(type: "bigint", nullable: true),
                    NewEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    NewRevision = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IncidentReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_audit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recovery_audit_Kind_OldEpoch",
                schema: "branding_projection",
                table: "recovery_audit",
                columns: new[] { "Kind", "OldEpoch" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recovery",
                schema: "branding_projection");

            migrationBuilder.DropTable(
                name: "recovery_audit",
                schema: "branding_projection");
        }
    }
}
