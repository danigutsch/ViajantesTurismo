using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260704110000_StoreMediaObjectKeys")]
    partial class StoreMediaObjectKeys
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            CatalogDbContextModelSnapshot.BuildCatalogModel(modelBuilder);
#pragma warning restore 612, 618
        }
    }
}
