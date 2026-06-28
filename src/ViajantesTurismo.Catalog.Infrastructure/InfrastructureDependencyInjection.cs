using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Catalog infrastructure services.
/// </summary>
public static class InfrastructureDependencyInjection
{
    /// <summary>
    /// Adds Catalog infrastructure services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<CatalogDbContext>(
            ResourceNames.Database,
            configureDbContextOptions: options => ConfigureDevelopmentDatabaseOptions(builder, options));

        builder.Services.AddCatalogApplication();
        builder.Services.AddScoped<IPublicContentStore, EfPublicContentStore>();

        return builder;
    }

    private static void ConfigureDevelopmentDatabaseOptions<TApplicationBuilder>(
        TApplicationBuilder builder,
        DbContextOptionsBuilder options)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
}
