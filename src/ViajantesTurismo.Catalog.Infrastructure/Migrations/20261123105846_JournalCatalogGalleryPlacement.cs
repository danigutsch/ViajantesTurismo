using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JournalCatalogGalleryPlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogGalleryPlacementIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogTourId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    PhotoIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CoverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CanPublish = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogGalleryPlacementIntents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogGalleryPlacementIntents_CatalogTourId_Sequence",
                table: "CatalogGalleryPlacementIntents",
                columns: new[] { "CatalogTourId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogGalleryPlacementIntents");
        }
    }
}
