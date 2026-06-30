using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistPublicMediaImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicMediaImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUri = table.Column<string>(type: "text", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Caption = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Attribution = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Copyright = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicMediaImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicMediaImageResponsiveVariants",
                columns: table => new
                {
                    PublicMediaImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicMediaImageResponsiveVariants", x => new { x.PublicMediaImageId, x.SortOrder });
                    table.ForeignKey(
                        name: "FK_PublicMediaImageResponsiveVariants_PublicMediaImages_Public~",
                        column: x => x.PublicMediaImageId,
                        principalTable: "PublicMediaImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicMediaImageTourLinks",
                columns: table => new
                {
                    CatalogTourId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicMediaImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCover = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicMediaImageTourLinks", x => new { x.PublicMediaImageId, x.CatalogTourId });
                    table.ForeignKey(
                        name: "FK_PublicMediaImageTourLinks_PublicMediaImages_PublicMediaImag~",
                        column: x => x.PublicMediaImageId,
                        principalTable: "PublicMediaImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder",
                table: "PublicMediaImageTourLinks",
                columns: new[] { "CatalogTourId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicMediaImageResponsiveVariants");

            migrationBuilder.DropTable(
                name: "PublicMediaImageTourLinks");

            migrationBuilder.DropTable(
                name: "PublicMediaImages");
        }
    }
}
