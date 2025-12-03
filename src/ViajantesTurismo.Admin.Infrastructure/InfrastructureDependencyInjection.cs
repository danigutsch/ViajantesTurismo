using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure.CompiledModels;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Provides extension methods for setting up the Infrastructure layer services in the application.
/// </summary>
public static class InfrastructureDependencyInjection
{
    /// <summary>
    /// Adds the Infrastructure layer services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<AdminWriteDbContext>(
            ResourceNames.Database,
            configureDbContextOptions: options =>
            {
                options.UseModel(AdminWriteDbContextModel.Instance);

                if (builder.Environment.IsDevelopment())
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });

        builder.AddNpgsqlDbContext<AdminReadDbContext>(
            ResourceNames.Database,
            configureDbContextOptions: options =>
            {
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                if (builder.Environment.IsDevelopment())
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });

        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AdminWriteDbContext>());
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
        builder.AddNpgsqlDbContext<AdminWriteDbContext>(
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

    /// <summary>
    /// Adds the seeding services to the service collection, including the database context and seeder implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSeeding(this IServiceCollection services) => services.AddScoped<ISeeder, Seeder>();
}
