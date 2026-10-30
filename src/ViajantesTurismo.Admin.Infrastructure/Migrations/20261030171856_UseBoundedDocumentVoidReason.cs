using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseBoundedDocumentVoidReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-edited: EF cannot infer the free-text to reason-code transform. Rows are mapped before the
            // column narrows; any value outside the two codes the application ever wrote becomes Legacy.
            migrationBuilder.Sql(
                """
                UPDATE "DocumentDrafts"
                SET "VoidReason" = CASE "VoidReason"
                    WHEN 'administrative-void' THEN 'AdministrativeVoid'
                    WHEN 'approval-withdrawn' THEN 'ApprovalWithdrawn'
                    ELSE 'Legacy'
                END
                WHERE "VoidReason" IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "VoidReason",
                table: "DocumentDrafts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VoidReason",
                table: "DocumentDrafts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "DocumentDrafts"
                SET "VoidReason" = CASE "VoidReason"
                    WHEN 'AdministrativeVoid' THEN 'administrative-void'
                    WHEN 'ApprovalWithdrawn' THEN 'approval-withdrawn'
                    ELSE 'legacy'
                END
                WHERE "VoidReason" IS NOT NULL;
                """);
        }
    }
}
