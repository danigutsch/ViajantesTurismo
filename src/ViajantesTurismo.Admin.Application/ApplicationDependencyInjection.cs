using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Application.Payments;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;

namespace ViajantesTurismo.Admin.Application;

/// <summary>
/// Provides extension methods for setting up the Application layer services in the application.
/// </summary>
public static class ApplicationDependencyInjection
{
    /// <summary>
    /// Adds the Application layer services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddApplication<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<PaymentInstructionWorkflow>();
        builder.Services.AddScoped<BookingPaymentOperations>();
        builder.Services.AddScoped<PaymentAccountOperations>();
        builder.Services.AddScoped<PaymentCommandIdempotency>();
        builder.Services.AddCustomerImport();
        builder.Services.AddScoped<DocumentBrandingSource>();
        builder.Services.AddScoped<IDocumentSourceVersionReader, DocumentSourceVersionReader>();
        builder.Services.AddScoped<DocumentCommandIdempotency>();
        builder.Services.AddScoped<DocumentAuditWriter>();
        builder.Services.AddScoped<GetFinalizedDocumentArtifactHandler>();
        builder.Services.AddScoped<DocumentHoldGate>();
        builder.Services.AddScoped<ListDocumentHoldsQueryHandler>();
        builder.Services.AddScoped<GetDocumentHoldQueryHandler>();
        builder.Services.AddScoped<ListDocumentHoldOverviewQueryHandler>();
        builder.Services.AddScoped<DocumentPurgeRunner>();
        builder.Services.AddDomainEventProcessing();

        return builder;
    }

    /// <summary>
    /// Adds domain event dispatching services used by write-side persistence.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDomainEventProcessing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGeneratedAuditTrailMappings();
        services.AddGeneratedIntegrationEvents();

        return services;
    }
}
