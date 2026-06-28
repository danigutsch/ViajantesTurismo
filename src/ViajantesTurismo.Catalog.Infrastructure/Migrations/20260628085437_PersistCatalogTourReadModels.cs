using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistCatalogTourReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogTourReadModels",
                columns: table => new
                {
                    CatalogTourId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminTourId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogTourReadModels", x => x.CatalogTourId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogTourReadModels_AdminTourId",
                table: "CatalogTourReadModels",
                column: "AdminTourId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogTourReadModels_Slug",
                table: "CatalogTourReadModels",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogTourReadModels");
        }
    }
}
