using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDispositionHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "disposition_hold");

            migrationBuilder.AddColumn<int>(
                name: "HeldCount",
                table: "DocumentAuditPurgeRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "holds",
                schema: "disposition_hold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(193)", maxLength: 193, nullable: false),
                    Basis = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    PurposeCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AuthorityReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PlacedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleasedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseReasonCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_holds_Scope",
                schema: "disposition_hold",
                table: "holds",
                column: "Scope");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holds",
                schema: "disposition_hold");

            migrationBuilder.DropColumn(
                name: "HeldCount",
                table: "DocumentAuditPurgeRecords");
        }
    }
}
