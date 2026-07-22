using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Branding;
using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Branding infrastructure services.
/// </summary>
public static class BrandingInfrastructureDependencyInjection
{
    private const string MigrationsHistoryTable = "__EFMigrationsHistory_Branding";

    /// <summary>
    /// Adds Branding infrastructure services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TBuilder AddBrandingInfrastructure<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<BrandingDbContext>(
            ResourceNames.CatalogDatabase,
            configureSettings: settings => settings.DisableTracing = true,
            configureDbContextOptions: options => ConfigureBrandingDatabaseOptions<BrandingDbContext, TBuilder>(builder, options));

        builder.Services.AddScoped<IBrandingSettingsStore, EfBrandingSettingsStore>();

        return builder;
    }

    private static void ConfigureBrandingDatabaseOptions<TContext, TBuilder>(
        TBuilder builder,
        DbContextOptionsBuilder options)
        where TContext : DbContext
        where TBuilder : IHostApplicationBuilder
    {
        options.UseNpgsql(providerOptions => providerOptions.MigrationsHistoryTable(
            MigrationsHistoryTable,
            schema: BrandingDbContext.MigrationsHistorySchemaName));

        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.ApplyDbContextOptionConfigurations<TContext>(options);
            return;
        }

        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
        builder.Services.ApplyDbContextOptionConfigurations<TContext>(options);
    }
}
