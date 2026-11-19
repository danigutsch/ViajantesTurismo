extern alias AdminWorker;

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBear.BuildingBlocks;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.Infrastructure.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;
using AdminRetirementHandler = AdminWorker::ViajantesTurismo.Admin.IntegrationEventWorker.CatalogTourRetirementAppliedIntegrationHandler;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Exercises the real Admin mutation transaction and query model over a migrated database.</summary>
internal sealed class TourRetirementPostgreSqlScenario(ServiceProvider provider) : IAsyncDisposable
{
    /// <summary>Creates a test-owned host and seeds an existing Tour and customer.</summary>
    internal static async Task<(TourRetirementPostgreSqlScenario Scenario, Guid TourId, Guid CustomerId)> Create(
        string connectionString, CancellationToken ct, IInterceptor? interceptor = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString;
        builder.Services.AddSingleton(TimeProvider.System);
        builder.AddApplication();
        builder.AddInfrastructure(addRuntimeBackgroundServices: false);
        builder.Services.Replace(ServiceDescriptor.Singleton(_ => new PaymentProtection(new EphemeralDataProtectionProvider())));
        if (interceptor is not null)
        {
            builder.Services.ConfigureDbContext<AdminWriteDbContext>(options => options.AddInterceptors(interceptor));
        }

        var services = builder.Services.BuildServiceProvider();
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
            await db.Database.MigrateAsync(ct);
            var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Eur)));
            var customer = EntityBuilders.BuildCustomer(null);
            db.Tours.Add(tour);
            db.Customers.Add(customer);
            await db.SaveEntities(ct);
            return (new TourRetirementPostgreSqlScenario(services), tour.Id, customer.Id);
        }
        catch
        {
            await services.DisposeAsync();
            throw;
        }
    }

    /// <summary>Retires the Tour through the shared Result transaction behavior.</summary>
    internal async Task<Result> Retire(
        DeleteTourCommand command, CancellationToken ct, Func<Result, Task<Result>>? afterSave = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var handler = new DeleteTourCommandHandler(services.GetRequiredService<ITourCommandStore>(),
            services.GetRequiredService<IUnitOfWork>());
        return await Run(services, command, async () =>
        {
            var result = await handler.Handle(command, ct);
            return afterSave is null ? result : await afterSave(result);
        }, ct);
    }

    /// <summary>Creates a booking through the shared Result transaction behavior.</summary>
    internal async Task<Result<Guid>> CreateBooking(
        Guid tourId, Guid customerId, CancellationToken ct, Func<Task>? afterSave = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var command = new CreateBookingCommand(tourId, customerId, BikeTypeDto.Regular,
            null, null, RoomTypeDto.DoubleOccupancy, DiscountTypeDto.None, 0m, null, null);
        var handler = new CreateBookingCommandHandler(services.GetRequiredService<ITourCommandStore>(),
            services.GetRequiredService<ICustomerStore>(), services.GetRequiredService<IUnitOfWork>(),
            services.GetRequiredService<TimeProvider>());
        return await Run(services, command, async () =>
        {
            var result = await handler.Handle(command, ct);
            if (afterSave is not null)
            {
                await afterSave();
            }

            return result;
        }, ct);
    }

    /// <summary>Applies a Catalog acknowledgement through the worker handler and durable completion store.</summary>
    internal async Task Acknowledge(Guid tourId, Guid retirementId, DateTimeOffset appliedAt, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var handler = new AdminRetirementHandler(new TourRetirementCompletion(db));
        await handler.Handle(new CatalogTourRetirementAppliedIntegrationEvent(
            Guid.CreateVersion7(), appliedAt, tourId, retirementId), ct);
    }

    /// <summary>Seeds a completed booking, its approved schedule, and one finalized contract and audit record.</summary>
    internal async Task<(Guid BookingId, Guid DocumentId)> SeedHistoricalBooking(
        Guid tourId, Guid customerId, DateTimeOffset now, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AdminWriteDbContext>();
        var tour = await services.GetRequiredService<ITourStore>().GetById(tourId, ct);
        tour.ShouldNotBeNull();
        var request = TourBookingRequest.CreateSingle(customerId, BikeType.Regular, RoomType.DoubleOccupancy);
        var booking = tour.AddBooking(request, now.UtcDateTime).Value;
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        tour.CompleteBooking(booking.Id).IsSuccess.ShouldBeTrue();
        var installment = PaymentInstallment.Create(100m, DateOnly.FromDateTime(now.AddDays(10).UtcDateTime)).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "payments-admin", now).Value;
        var approval = FinancialApproval.Create("payments-admin", now, FinancialReason.InitialObligation, true).Value;
        tour.ApproveBookingPaymentSchedule(booking.Id, booking.FinancialHistory?.Version ?? 0, schedule, approval).IsSuccess.ShouldBeTrue();
        await services.GetRequiredService<IPaymentHistoryStore>().Save(tour, booking.Id, ct);
        await db.SaveEntities(ct);

        var document = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(now.UtcDateTime, booking.Id);
        db.DocumentLineages.Add(DocumentLineage.Restore([document]));
        db.DocumentAuditRecords.Add(DocumentAuditInfrastructureTestData.CreateRecord(now.UtcDateTime, document.Id, booking.Id));
        await db.SaveEntities(ct);
        return (booking.Id, document.Id);
    }

    /// <summary>Reads historical booking, protected payment and retained document records from fresh contexts.</summary>
    internal async Task<(GetBookingDto? Booking, long FinancialVersion, int PaymentAudits, Guid LineageId, int Artifacts, Guid[] DocumentAudits)> ReadHistory(
        Guid bookingId, Guid documentId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AdminWriteDbContext>();
        var booking = await services.GetRequiredService<IQueryService>().GetBookingById(bookingId, ct);
        var financialVersion = await db.Set<PaymentHistoryRow>().Where(row => row.BookingId == bookingId)
            .Select(row => row.Version).SingleAsync(ct);
        var paymentAudits = await db.Set<PaymentAuditRow>().CountAsync(row => row.ResourceId == bookingId, ct);
        var lineageId = await db.DocumentDrafts.Where(draft => draft.Id == documentId)
            .Select(draft => draft.DocumentLineageId).SingleAsync(ct);
        var artifacts = await db.Set<DocumentArtifactBody>().CountAsync(body => body.DocumentId == documentId, ct);
        var documentAudits = await db.DocumentAuditRecords.Where(record => record.DocumentId == documentId)
            .OrderBy(record => record.Id).Select(record => record.Id).ToArrayAsync(ct);
        return (booking, financialVersion, paymentAudits, lineageId, artifacts, documentAudits);
    }

    /// <summary>Reads the active and historical views, committed audit evidence and retirement outbox events.</summary>
    internal async Task<TourRetirementState> Read(Guid tourId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AdminWriteDbContext>();
        var queries = services.GetRequiredService<IQueryService>();
        var active = await queries.GetActiveTourById(tourId, ct);
        var historical = await queries.GetTourById(tourId, ct);
        var activeList = await queries.GetAllTours(ct);
        var status = await queries.GetTourRetirement(tourId, ct);
        var audits = await db.TourRetirementAuditRecords.Where(audit => audit.TourId == tourId).ToArrayAsync(ct);
        var payloads = await db.Database.SqlQuery<string>($"""
            SELECT "Payload" AS "Value"
            FROM messaging.outbox_messages
            WHERE "EventType" = {AdminTourRetiredIntegrationEvent.EventType}
            """).ToArrayAsync(ct);
        var events = payloads.Select(payload =>
            JsonSerializer.Deserialize(payload, AdminIntegrationEventJsonContext.Default.AdminTourRetiredIntegrationEvent)
            ?? throw new InvalidOperationException("Expected a tour retirement event.")).ToArray();
        var bookings = await db.Tours.Where(tour => tour.Id == tourId)
            .SelectMany(tour => tour.Bookings).Select(booking => booking.Id).ToArrayAsync(ct);
        return new TourRetirementState(active, historical, activeList, status, audits, events, bookings);
    }

    /// <summary>Lets the first transaction write, observes the contender queue, then releases the first commit.</summary>
    internal async Task<(Result Retirement, Result<Guid> Booking)> RaceBookingAndRetirement(
        DeleteTourCommand command, Guid customerId, bool retirementFirst, CancellationToken ct)
    {
        var firstSaved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Result> retirement;
        Task<Result<Guid>> booking;
        try
        {
            if (retirementFirst)
            {
                retirement = Retire(command, ct, async result =>
                {
                    firstSaved.TrySetResult();
                    await releaseFirst.Task.WaitAsync(ct);
                    return result;
                });
                await firstSaved.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
                booking = CreateBooking(command.TourId, customerId, ct);
            }
            else
            {
                booking = CreateBooking(command.TourId, customerId, ct, async () =>
                {
                    firstSaved.TrySetResult();
                    await releaseFirst.Task.WaitAsync(ct);
                });
                await firstSaved.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
                retirement = Retire(command, ct);
            }

            await WaitForBlockedSession(ct);
        }
        finally
        {
            releaseFirst.TrySetResult();
        }

        return (await retirement, await booking);
    }

    /// <summary>Waits for a separate session in this test database to queue on the first command's Tour lock.</summary>
    private async Task WaitForBlockedSession(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        do
        {
            if (await db.Database.SqlQueryRaw<bool>(
                """
                SELECT EXISTS (
                    SELECT 1 FROM pg_stat_activity
                    WHERE datname = current_database()
                      AND wait_event_type = 'Lock'
                      AND query LIKE '%FOR UPDATE%') AS "Value"
                """).SingleAsync(timeout.Token))
            {
                return;
            }
        }
        while (await timer.WaitForNextTickAsync(timeout.Token));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await provider.DisposeAsync();

    private static async Task<TResponse> Run<TCommand, TResponse>(
        IServiceProvider services, TCommand command, Func<ValueTask<TResponse>> next, CancellationToken ct)
        where TCommand : ICommand<TResponse>
        where TResponse : struct, IResultLike<TResponse>
    {
        var behavior = new AdminTransactionBehavior<TCommand, TResponse>(
            services.GetRequiredService<DbContext>(),
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<ILogger<AdminTransactionBehavior<TCommand, TResponse>>>());
        return await behavior.Handle(command, () => next(), ct);
    }
}
