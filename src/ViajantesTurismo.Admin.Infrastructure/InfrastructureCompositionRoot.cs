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
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<ApplicationDbContext>(
            ResourceNames.Database,
            configureDbContextOptions: options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<IQueryService, QueryService>();
        builder.Services.AddScoped<ITourStore, TourStore>();
        builder.Services.AddScoped<ICustomerStore, CustomerStore>();

        return builder;
    }

    /// <summary>
    /// Adds the seeding services to the application builder, including the database context and seeder implementation.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddSeeding<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<ApplicationDbContext>(
            ResourceNames.Database,
            configureDbContextOptions: options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });
        builder.Services.AddScoped<ISeeder, Seeder>();

        return builder;
    }
}