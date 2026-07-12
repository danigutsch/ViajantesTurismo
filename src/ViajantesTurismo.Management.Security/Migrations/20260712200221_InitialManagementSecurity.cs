using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ViajantesTurismo.Management.Security.Migrations
{
    /// <inheritdoc />
    public partial class InitialManagementSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_protection_keys", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS security.management_cookie_tickets (
                    id VARCHAR(449) COLLATE "C" PRIMARY KEY,
                    value BYTEA NOT NULL,
                    expiresattime TIMESTAMPTZ NOT NULL,
                    slidingexpirationinseconds BIGINT NULL,
                    absoluteexpiration TIMESTAMPTZ NULL
                );
                CREATE INDEX IF NOT EXISTS ix_expiresattime
                    ON security.management_cookie_tickets (expiresattime)
                    WITH (deduplicate_items = true);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS security.management_cookie_tickets;");

            migrationBuilder.DropTable(
                name: "data_protection_keys",
                schema: "security");
        }
    }
}
