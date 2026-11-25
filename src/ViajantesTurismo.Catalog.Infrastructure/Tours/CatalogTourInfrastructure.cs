using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SharedKernel.EventSourcing;
using SharedKernel.EventSourcing.Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Composes the event store, projection runner, and tour read model.</summary>
internal static class CatalogTourInfrastructure
{
    /// <summary>Registers the tour event and projection infrastructure.</summary>
    internal static TApplicationBuilder AddCatalogTourInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.TryAddScoped<ICatalogTourReadModelStore, EfCatalogTourReadModelStore>();
        builder.Services.TryAddScoped<ICatalogTourListReader, EfCatalogTourListReader>();
        builder.Services.TryAddScoped<ICatalogTourRetirementStore, EfCatalogTourRetirementStore>();
        builder.Services.AddSingleton<IEventSerializer, CatalogEventSerializer>();
        builder.Services.AddSingleton<IEventStore, PostgreSqlEventStore>();
        builder.Services.AddSingleton<IProjectionCheckpointStore, PostgreSqlProjectionCheckpointStore>();
        builder.Services.AddScoped<CatalogTourReadModelProjection>();
        builder.Services.AddScoped<IProjection>(services => services.GetRequiredService<CatalogTourReadModelProjection>());
        builder.Services.AddScoped<CatalogProjectionRunner>();

        return builder;
    }
}
