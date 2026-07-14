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
            .UseNpgsql(providerOptions => providerOptions.MigrationsHistoryTable("__EFMigrationsHistory_Branding", schema: "public"))
            .Options;

        return new BrandingDbContext(options);
    }
}
