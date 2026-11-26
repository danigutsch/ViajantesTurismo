using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingWorkflowCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowCorrelationId",
                schema: "branding",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id",
                schema: "branding",
                table: "outbox_messages",
                columns: new[] { "WorkflowCorrelationId", "EnqueuedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id",
                schema: "branding",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "WorkflowCorrelationId",
                schema: "branding",
                table: "outbox_messages");
        }
    }
}
