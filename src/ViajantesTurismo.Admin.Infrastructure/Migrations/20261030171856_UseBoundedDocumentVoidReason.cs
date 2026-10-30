using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <summary>
    /// Converts void reasons to bounded codes. Hand-edited because EF cannot infer the transform: Up refuses to run
    /// when any value is not one of the two codes the application wrote, so no free text is rewritten or dropped.
    /// </summary>
    public partial class UseBoundedDocumentVoidReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "DocumentDrafts"
                        WHERE "VoidReason" IS NOT NULL
                          AND "VoidReason" NOT IN ('administrative-void', 'approval-withdrawn')) THEN
                        RAISE EXCEPTION 'DocumentDrafts.VoidReason holds values other than the known reason codes; review them before migrating.';
                    END IF;
                END $$;

                UPDATE "DocumentDrafts"
                SET "VoidReason" = CASE "VoidReason"
                    WHEN 'administrative-void' THEN 'AdministrativeVoid'
                    WHEN 'approval-withdrawn' THEN 'ApprovalWithdrawn'
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
                END
                WHERE "VoidReason" IS NOT NULL;
                """);
        }
    }
}
