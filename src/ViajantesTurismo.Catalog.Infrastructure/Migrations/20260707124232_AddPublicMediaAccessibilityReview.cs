using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicMediaAccessibilityReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAiGenerated",
                table: "PublicMediaImages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDecorative",
                table: "PublicMediaImages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHumanReview",
                table: "PublicMediaImages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PublicMediaImageAccessibilityTexts",
                columns: table => new
                {
                    PublicMediaImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Caption = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    IsDecorative = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicMediaImageAccessibilityTexts", x => new { x.PublicMediaImageId, x.Language });
                    table.ForeignKey(
                        name: "FK_PublicMediaImageAccessibilityTexts_PublicMediaImages_Public~",
                        column: x => x.PublicMediaImageId,
                        principalTable: "PublicMediaImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicMediaImageAccessibilityTexts");

            migrationBuilder.DropColumn(
                name: "IsAiGenerated",
                table: "PublicMediaImages");

            migrationBuilder.DropColumn(
                name: "IsDecorative",
                table: "PublicMediaImages");

            migrationBuilder.DropColumn(
                name: "RequiresHumanReview",
                table: "PublicMediaImages");
        }
    }
}
