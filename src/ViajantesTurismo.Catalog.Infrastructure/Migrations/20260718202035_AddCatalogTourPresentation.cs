using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogTourPresentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CatalogTourReadModels",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Itinerary",
                table: "CatalogTourReadModels",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "PresentationPosition",
                table: "CatalogTourReadModels",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PublicationPosition",
                table: "CatalogTourReadModels",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                table: "CatalogTourReadModels",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                table: "CatalogTourReadModels",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "StreamVersion",
                table: "CatalogTourReadModels",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "CatalogTourReadModels",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "Itinerary",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "PresentationPosition",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "PublicationPosition",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "StreamVersion",
                table: "CatalogTourReadModels");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "CatalogTourReadModels");
        }
    }
}
