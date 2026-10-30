using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <summary>
    /// Replaces the raw caller keys of document idempotency entries with their SHA-256 digests, so entries written
    /// before keys were digested still replay. Hand-edited because EF cannot infer a data transform. The digest cannot be
    /// reversed, so Down leaves the digests in place.
    /// </summary>
    public partial class DigestDocumentIdempotencyKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "messaging"."idempotency_keys"
                SET "Key" = upper(encode(sha256(convert_to("Key", 'UTF8')), 'hex'))
                WHERE starts_with("Scope", 'admin.documents.');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
