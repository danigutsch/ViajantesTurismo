using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogWorkflowCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowCorrelationId",
                schema: "messaging",
                table: "transport_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowCorrelationId",
                schema: "messaging",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transport_messages_ConsumerName_WorkflowCorrelationId_Recei~",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "WorkflowCorrelationId", "ReceivedAt", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "WorkflowCorrelationId", "EnqueuedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transport_messages_ConsumerName_WorkflowCorrelationId_Recei~",
                schema: "messaging",
                table: "transport_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "WorkflowCorrelationId",
                schema: "messaging",
                table: "transport_messages");

            migrationBuilder.DropColumn(
                name: "WorkflowCorrelationId",
                schema: "messaging",
                table: "outbox_messages");
        }
    }
}
