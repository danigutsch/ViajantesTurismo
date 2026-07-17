using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeDocumentRetentionExpiryOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "RetentionExpiresAt",
                table: "DocumentDrafts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.Sql(
                """
                UPDATE "DocumentDrafts"
                SET "RetentionExpiresAt" = NULL
                WHERE "FinalizedAt" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "DocumentDrafts"
                SET "RetentionExpiresAt" = COALESCE("RetentionExpiresAt", "FinalizedAt", CURRENT_TIMESTAMP);
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RetentionExpiresAt",
                table: "DocumentDrafts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
