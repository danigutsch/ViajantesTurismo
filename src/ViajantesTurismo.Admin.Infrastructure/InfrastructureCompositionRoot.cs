using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Provides extension methods for setting up the infrastructure services in the application.
/// </summary>
public static class InfrastructureCompositionRoot
{
    /// <summary>
    /// Adds the infrastructure services to the application builder, including the PostgreSQL database context.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns></returns>
    public static TApplicationBuilder AddInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<ApplicationDbContext>(ResourceNames.Database);
        builder.Services.AddScoped<IUnitOfWork, ApplicationDbContext>();
        builder.Services.AddScoped<IQueryService, QueryService>();
        builder.Services.AddScoped<ITourStore, TourStore>();

        return builder;
    }
}
