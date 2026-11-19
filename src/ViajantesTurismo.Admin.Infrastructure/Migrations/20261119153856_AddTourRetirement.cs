using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourRetirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CatalogRetirementAppliedAt",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetiredAt",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RetirementId",
                table: "Tours",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TourRetirementAuditRecords",
                columns: table => new
                {
                    RetirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourRetirementAuditRecords", x => x.RetirementId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_RetirementId",
                table: "Tours",
                column: "RetirementId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tours_RetirementState",
                table: "Tours",
                sql: "(\"RetirementId\" IS NULL) = (\"RetiredAt\" IS NULL)\nAND (\"CatalogRetirementAppliedAt\" IS NULL OR \"RetirementId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_TourRetirementAuditRecords_TourId",
                table: "TourRetirementAuditRecords",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourRetirementAuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_Tours_RetirementId",
                table: "Tours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tours_RetirementState",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "CatalogRetirementAppliedAt",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "RetiredAt",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "RetirementId",
                table: "Tours");
        }
    }
}
