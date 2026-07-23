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
        .WithTracing(tracingBuilder => { tracingBuilder.AddSource(DatabaseInitializationWorker.ActivitySourceName); });

    builder.AddServiceDefaults();

    builder.AddAdminDatabaseInitialization();
    builder.AddBrandingInfrastructure();
    builder.AddCatalogDatabaseInitialization();
    builder.Services.AddManagementSecurityPersistence(
        builder.Configuration.GetConnectionString(ResourceNames.SecurityDatabase)
        ?? throw new InvalidOperationException("The security database connection string is required."));

    builder.Services.AddSingleton<DatabaseInitializationWorker>();

    return builder.Build();
});
