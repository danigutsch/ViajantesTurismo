using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistPublicContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceLanguage = table.Column<string>(type: "text", nullable: false),
                    PublicationState = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicContentVariants",
                columns: table => new
                {
                    Language = table.Column<string>(type: "text", nullable: false),
                    PublicContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SeoTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ShareSummary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicContentVariants", x => new { x.PublicContentId, x.Language });
                    table.ForeignKey(
                        name: "FK_PublicContentVariants_PublicContent_PublicContentId",
                        column: x => x.PublicContentId,
                        principalTable: "PublicContent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicContent_Key",
                table: "PublicContent",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicContentVariants");

            migrationBuilder.DropTable(
                name: "PublicContent");
        }
    }
}
