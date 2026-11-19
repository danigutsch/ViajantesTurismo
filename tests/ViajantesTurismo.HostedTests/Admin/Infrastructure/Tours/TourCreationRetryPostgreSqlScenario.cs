using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Runs the tour creation handler and its transaction behavior against an isolated database.</summary>
internal static class TourCreationRetryPostgreSqlScenario
{
    /// <summary>Injects one serialization failure after the tour and outbox writes, then reads committed state in a fresh scope.</summary>
    internal static async Task<(
        Result<Guid> Result, int SerializationFailures, int HandlerAttempts, Guid[] TourIds, AdminTourCreatedIntegrationEvent[] OutboxEvents)> CreateTour(
        string connectionString,
        CreateTourCommand command,
        CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString;
        builder.AddApplication();
        builder.AddInfrastructure(addRuntimeBackgroundServices: false);
        var interceptor = new TourCreationCommitFailureInterceptor();
        builder.Services.ConfigureDbContext<AdminWriteDbContext>(options => options.AddInterceptors(interceptor));

        await using var provider = builder.Services.BuildServiceProvider();
        await using var writeScope = provider.CreateAsyncScope();
        var services = writeScope.ServiceProvider;
        var writeContext = services.GetRequiredService<AdminWriteDbContext>();
        await writeContext.Database.MigrateAsync(ct);

        var handler = new CreateTourCommandHandler(services.GetRequiredService<ITourStore>(), services.GetRequiredService<IUnitOfWork>());
        var behavior = new AdminTransactionBehavior<CreateTourCommand, Result<Guid>>(
            services.GetRequiredService<DbContext>(),
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<ILogger<AdminTransactionBehavior<CreateTourCommand, Result<Guid>>>>());

        var attempts = 0;
        interceptor.Arm();
        var response = await behavior.Handle(command, () =>
        {
            attempts++;
            return handler.Handle(command, ct);
        }, ct);

        await using var inspectionScope = provider.CreateAsyncScope();
        var persisted = inspectionScope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var tourIds = await persisted.Tours.Select(tour => tour.Id).ToArrayAsync(ct);
        var payloads = await persisted.Database.SqlQueryRaw<string>(
            """
            SELECT "Payload" AS "Value"
            FROM messaging.outbox_messages
            """).ToArrayAsync(ct);
        var events = payloads.Select(payload => JsonSerializer.Deserialize(payload, AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent)
            ?? throw new InvalidOperationException("Expected one serialized tour creation event.")).ToArray();

        return (response, interceptor.Failures, attempts, tourIds, events);
    }
}
