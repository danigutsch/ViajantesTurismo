using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyRetentionOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetentionOwner",
                schema: "messaging",
                table: "idempotency_keys",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_keys_RetentionOwner",
                schema: "messaging",
                table: "idempotency_keys",
                column: "RetentionOwner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_idempotency_keys_RetentionOwner",
                schema: "messaging",
                table: "idempotency_keys");

            migrationBuilder.DropColumn(
                name: "RetentionOwner",
                schema: "messaging",
                table: "idempotency_keys");
        }
    }
}
