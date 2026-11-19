using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BoundBrandingLogoStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_BrandingLogos_ContentLength",
                schema: BrandingDbContext.SchemaName,
                table: "BrandingLogos",
                sql: "octet_length(\"Content\") BETWEEN 1 AND 40960");

            // The outbox is configured by the SharedKernel provider.
            migrationBuilder.AddCheckConstraint(
                name: "CK_BrandingOutbox_PayloadLength",
                schema: BrandingDbContext.SchemaName,
                table: "outbox_messages",
                sql: "octet_length(\"Payload\") <= 65536");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BrandingOutbox_PayloadLength",
                schema: BrandingDbContext.SchemaName,
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BrandingLogos_ContentLength",
                schema: BrandingDbContext.SchemaName,
                table: "BrandingLogos");
        }
    }
}
