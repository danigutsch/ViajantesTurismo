using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ViajantesTurismo.Branding.Infrastructure;

internal sealed class BrandingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BrandingDbContext>
{
    public BrandingDbContextDesignTimeFactory()
    {
    }

    public BrandingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=branding-design-time",
                providerOptions => providerOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Branding",
                    schema: BrandingDbContext.MigrationsHistorySchemaName))
            .Options;

        return new BrandingDbContext(options);
    }
}
