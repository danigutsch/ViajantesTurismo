using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingRecoveryAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrandingRecoveryAudits",
                schema: "branding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OldEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    OldRevision = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_BrandingRecoveryAudits", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandingRecoveryAudits",
                schema: "branding");
        }
    }
}
