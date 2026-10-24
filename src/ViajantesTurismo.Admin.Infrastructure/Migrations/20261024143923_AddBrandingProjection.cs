using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "branding_projection");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "branding_projection",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevision = table.Column<long>(type: "bigint", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "branding_projection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevision = table.Column<long>(type: "bigint", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    HeadingFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BodyFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LogoUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_Outcome",
                schema: "branding_projection",
                table: "inbox_messages",
                column: "Outcome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "branding_projection");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "branding_projection");
        }
    }
}
