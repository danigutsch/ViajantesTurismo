using System;
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

            migrationBuilder.CreateTable(
                name: "management_cookie_tickets",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(449)", maxLength: 449, nullable: false, collation: "C"),
                    value = table.Column<byte[]>(type: "bytea", nullable: false),
                    expiresattime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slidingexpirationinseconds = table.Column<long>(type: "bigint", nullable: true),
                    absoluteexpiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_management_cookie_tickets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expiresattime",
                schema: "security",
                table: "management_cookie_tickets",
                column: "expiresattime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_protection_keys",
                schema: "security");

            migrationBuilder.DropTable(
                name: "management_cookie_tickets",
                schema: "security");
        }
    }
}
