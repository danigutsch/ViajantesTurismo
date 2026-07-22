using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;
using ViajantesTurismo.MigrationService;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

return await MigrationProcess.Run(() =>
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracingBuilder => { tracingBuilder.AddSource(MigrationRunner.ActivitySourceName); });

    builder.AddServiceDefaults();

    builder.Services.AddDomainEventProcessing();
    builder.AddAdminSeeding();
    builder.AddBrandingInfrastructure();
    builder.AddCatalogSeeding();
    builder.Services.AddManagementSecurityPersistence(
        builder.Configuration.GetConnectionString(ResourceNames.SecurityDatabase)
        ?? throw new InvalidOperationException("The security database connection string is required."));

    builder.Services.AddSingleton<MigrationRunner>();

    return builder.Build();
});
