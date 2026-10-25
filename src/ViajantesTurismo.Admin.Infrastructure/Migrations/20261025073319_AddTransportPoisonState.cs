using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportPoisonState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PoisonedAt",
                schema: "messaging",
                table: "transport_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transport_messages_ConsumerName_PoisonedAt",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "PoisonedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transport_messages_ConsumerName_PoisonedAt",
                schema: "messaging",
                table: "transport_messages");

            migrationBuilder.DropColumn(
                name: "PoisonedAt",
                schema: "messaging",
                table: "transport_messages");
        }
    }
}
