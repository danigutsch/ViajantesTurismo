using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUndecidableCountToDocumentAuditPurgeEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UndecidableCount",
                table: "DocumentAuditPurgeRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UndecidableCount",
                table: "DocumentAuditPurgeRecords");
        }
    }
}
