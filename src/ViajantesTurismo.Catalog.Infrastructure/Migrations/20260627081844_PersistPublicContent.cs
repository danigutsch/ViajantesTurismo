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
                    EnUsLanguage = table.Column<string>(type: "text", nullable: false),
                    EnUsTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EnUsBody = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    EnUsSeoTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EnUsMetaDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EnUsShareSummary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EnUsRequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                    PtBrLanguage = table.Column<string>(type: "text", nullable: false),
                    PtBrTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PtBrBody = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PtBrSeoTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PtBrMetaDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PtBrShareSummary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PtBrRequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                    PublicationState = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicContent", x => x.Id);
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
                name: "PublicContent");
        }
    }
}
