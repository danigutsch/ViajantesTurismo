using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProjectCatalogGalleryPlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GalleryCoverId",
                table: "CatalogTourReadModels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid[]>(
                name: "GalleryPhotoIds",
                table: "CatalogTourReadModels",
                type: "uuid[]",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GalleryPosition",
                table: "CatalogTourReadModels",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryCoverId",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "GalleryPhotoIds",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "GalleryPosition",
                table: "CatalogTourReadModels");
        }
    }
}
