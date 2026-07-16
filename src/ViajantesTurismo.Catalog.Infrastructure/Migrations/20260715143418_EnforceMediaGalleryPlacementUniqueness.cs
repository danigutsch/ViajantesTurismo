using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceMediaGalleryPlacementUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder",
                table: "PublicMediaImageTourLinks");

            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT
                        "PublicMediaImageId",
                        "CatalogTourId",
                        "IsCover",
                        (ROW_NUMBER() OVER (
                            PARTITION BY "CatalogTourId"
                            ORDER BY "DisplayOrder", "PublicMediaImageId") - 1)::integer AS "NormalizedDisplayOrder",
                        ROW_NUMBER() OVER (
                            PARTITION BY "CatalogTourId", "IsCover"
                            ORDER BY "DisplayOrder", "PublicMediaImageId") AS "CoverRank"
                    FROM "PublicMediaImageTourLinks"
                ),
                duplicate_display_order_tours AS (
                    SELECT "CatalogTourId"
                    FROM "PublicMediaImageTourLinks"
                    GROUP BY "CatalogTourId"
                    HAVING COUNT(*) <> COUNT(DISTINCT "DisplayOrder")
                )
                UPDATE "PublicMediaImageTourLinks" AS placement
                SET
                    "DisplayOrder" = CASE
                        WHEN duplicate_display_order_tours."CatalogTourId" IS NOT NULL
                            THEN ranked."NormalizedDisplayOrder"
                        ELSE placement."DisplayOrder"
                    END,
                    "IsCover" = CASE
                        WHEN ranked."IsCover" AND ranked."CoverRank" > 1 THEN FALSE
                        ELSE placement."IsCover"
                    END
                FROM ranked
                LEFT JOIN duplicate_display_order_tours
                    ON duplicate_display_order_tours."CatalogTourId" = ranked."CatalogTourId"
                WHERE placement."PublicMediaImageId" = ranked."PublicMediaImageId"
                  AND placement."CatalogTourId" = ranked."CatalogTourId"
                  AND (
                      duplicate_display_order_tours."CatalogTourId" IS NOT NULL
                      OR (ranked."IsCover" AND ranked."CoverRank" > 1)
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "UX_PublicMediaImageTourLinks_CatalogTourId_Cover",
                table: "PublicMediaImageTourLinks",
                column: "CatalogTourId",
                unique: true,
                filter: "\"IsCover\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "UX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder",
                table: "PublicMediaImageTourLinks",
                columns: new[] { "CatalogTourId", "DisplayOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PublicMediaImageTourLinks_CatalogTourId_Cover",
                table: "PublicMediaImageTourLinks");

            migrationBuilder.DropIndex(
                name: "UX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder",
                table: "PublicMediaImageTourLinks");

            migrationBuilder.CreateIndex(
                name: "IX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder",
                table: "PublicMediaImageTourLinks",
                columns: new[] { "CatalogTourId", "DisplayOrder" });
        }
    }
}
