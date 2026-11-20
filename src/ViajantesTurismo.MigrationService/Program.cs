using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;
using ViajantesTurismo.MigrationService;
using ViajantesTurismo.MigrationService.GovernedMigrations;
using ViajantesTurismo.Resources;
using PandaBear.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

if (args is ["plan", ..])
{
    return await RunGovernedCommand(args, MigrationPlanCommand.Run);
}

if (args is ["execute" or "contract" or "recover", ..])
{
    return await RunGovernedCommand(args, MigrationExecutionCommand.Run);
}

return await MigrationProcess.Run(() =>
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.AddSharedKernelServiceDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();

    builder.AddAdminDatabaseInitialization();
    builder.AddBrandingInfrastructure(addOutboxRelay: false);
    builder.AddCatalogDatabaseInitialization();
    builder.Services.AddManagementSecurityPersistence(
        builder.Configuration.GetConnectionString(ResourceNames.SecurityDatabase)
        ?? throw new InvalidOperationException("The security database connection string is required."));

    builder.Services.AddSingleton<DatabaseInitializationWorker>();

    return builder.Build();
});

static async Task<int> RunGovernedCommand(
    string[] args, Func<string[], TextWriter, TextWriter, CancellationToken, Task<int>> command)
{
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += Cancel;
    try
    {
        return await command(args, Console.Out, Console.Error, cancellation.Token);
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        return 130;
    }
    finally
    {
        Console.CancelKeyPress -= Cancel;
    }

    void Cancel(object? sender, ConsoleCancelEventArgs eventArgs)
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    }
}
