using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SharedKernel.Domain.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using SharedKernel.Mediator;
using SharedKernel.Mediator.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Tours;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Test host that wires payment, document and tour services against a PostgreSQL database and exposes them for scenarios.</summary>
internal sealed class PaymentTestSession : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private AdminWriteDbContext Context => _provider.GetRequiredService<AdminWriteDbContext>();
    private PaymentTestSession(ServiceProvider provider) => _provider = provider;

    /// <summary>Builds a session whose services use <paramref name="connectionString" />, <paramref name="protection" /> and <paramref name="clock" />.</summary>
    internal static PaymentTestSession Create(string connectionString, PaymentProtection protection, TimeProvider clock)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.AddApplication();
        var services = builder.Services;
        services.AddMediator();
        services.AddSingleton<ViajantesTurismo.Admin.Application.Branding.IBrandingProjectionStore, PaymentTestBrandingProjection>();
        services.AddSingleton(clock);
        services.AddSingleton(protection);
        services.AddSingleton(new PaymentBusinessCalendar(clock, TimeZoneInfo.Utc));
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddAdminDispositionHolds();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        services.AddDbContext<AdminWriteDbContext>((provider, options) => options.UseNpgsql(connectionString));
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AdminWriteDbContext>());
        // This fixture omits messaging mappings. Keep its model out of EF's shared provider cache.
        services.AddDbContext<AdminReadDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableServiceProviderCaching(false));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AdminWriteDbContext>());
        services.AddScoped<IAfterRollback<DownloadPaymentInstructionsCommand, Result<FinalizedDocumentArtifact>>, PaymentInstructionDownloadRollbackAudit>();
        services.AddScoped<IPaymentAccountStore, PaymentAccountStore>();
        services.AddScoped<IPaymentAccountQueryService, PaymentAccountQueryService>();
        services.AddScoped<PaymentHistoryStore>();
        services.AddScoped<IPaymentHistoryStore>(provider => provider.GetRequiredService<PaymentHistoryStore>());
        services.AddTourPersistence();
        services.AddScoped<IDocumentStore, DocumentStore>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();
        services.Replace(ServiceDescriptor.Singleton<IDocumentSourceVersionReader, UnexpectedContractSourceVersionReader>());
        services.AddScoped<IPaymentInstructionStore, PaymentInstructionStore>();
        services.AddScoped<IPaymentFinancialQueryService, PaymentFinancialQueryService>();
        return new PaymentTestSession(services.BuildServiceProvider());
    }

    /// <summary>Applies EF migrations, up to <paramref name="target" /> when given, otherwise to the latest.</summary>
    public async Task Migrate(CancellationToken ct, string? target = null) => await Context.GetService<IMigrator>().MigrateAsync(target, ct);
    /// <summary>Adds the tour, saves payment history for bookings that have one, and commits.</summary>
    public async Task Seed(Tour tour, CancellationToken ct)
    {
        Context.Tours.Add(tour);
        foreach (var booking in tour.Bookings.Where(item => item.FinancialHistory is not null))
        {
            await _provider.GetRequiredService<IPaymentHistoryStore>().Save(tour, booking.Id, ct);
        }
        await Context.SaveEntities(ct);
    }
    /// <summary>Saves the payment account and commits.</summary>
    public async Task SeedAccount(ViajantesTurismo.Admin.Domain.PaymentAccounts.PaymentAccount account, CancellationToken ct)
    {
        await _provider.GetRequiredService<IPaymentAccountStore>().Save(account, ct);
        await Context.SaveEntities(ct);
    }
    /// <summary>Resolves the payment instruction workflow.</summary>
    public PaymentInstructionWorkflow Instructions => _provider.GetRequiredService<PaymentInstructionWorkflow>();

    /// <summary>Counts instruction documents loaded through the tracked write model.</summary>
    public int TrackedInstructionRevisions => Context.ChangeTracker.Entries<DocumentDraft>().Count(entry => entry.Entity.RequiresPaymentInstructionWorkflow);

    /// <summary>Reads persisted metadata-only audit evidence for an instruction.</summary>
    public Task<IReadOnlyList<PaymentInstructionAuditMetadata>> ReadInstructionAudits(Guid documentId, CancellationToken ct) =>
        _provider.GetRequiredService<IPaymentInstructionStore>().GetAudits(documentId, ct);

    /// <summary>Persists accumulated instruction audits through the same store as a command.</summary>
    public async Task SeedInstructionAudits(Guid bookingId, IEnumerable<PaymentInstructionAuditMetadata> audits, CancellationToken ct)
    {
        var store = _provider.GetRequiredService<IPaymentInstructionStore>();
        foreach (var audit in audits)
        {
            store.AddAudit(bookingId, audit);
        }

        await Context.SaveEntities(ct);
    }

    /// <summary>Dispatches a payment command through the production mediator pipeline.</summary>
    public ValueTask<Result<Guid>> Send(ICommand<Result<Guid>> command, CancellationToken ct) =>
        _provider.GetRequiredService<ISender>().Send(command, ct);

    /// <summary>Holds the booking row lock until the returned transaction is rolled back or disposed.</summary>
    public async Task<IDbContextTransaction> HoldBooking(Guid bookingId, CancellationToken ct)
    {
        var transaction = await Context.Database.BeginTransactionAsync(ct);
        try
        {
            _ = await Context.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT 1 FROM "Booking" WHERE "Id" = {bookingId} FOR UPDATE
                """, ct);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    /// <summary>Reads the metadata persisted on a finalized payment document revision.</summary>
    internal async Task<StoredArtifactProvenance> ReadArtifactProvenance(Guid documentId, CancellationToken ct) =>
        await Context.Database.SqlQuery<StoredArtifactProvenance>($"""
            SELECT "FinalizedArtifactRendererVersion" AS "RendererVersion",
                "FinalizedArtifactContentType" AS "ContentType",
                "FinalizedArtifactRetentionPolicy" AS "RetentionPolicy",
                "RetentionExpiresAt"
            FROM "DocumentDrafts"
            WHERE "Id" = {documentId}
            """).SingleAsync(ct);
    /// <summary>Counts persisted payment account rows.</summary>
    public async Task<int> AccountCount(CancellationToken ct) => await Context.Set<PaymentAccountRow>().CountAsync(ct);
    /// <summary>Counts persisted payment audit rows.</summary>
    public async Task<int> AuditCount(CancellationToken ct) => await Context.Set<PaymentAuditRow>().CountAsync(ct);
    /// <summary>Reads the single persisted financial details access audit for a test actor.</summary>
    public async Task<(Guid BookingId, long Revision, string ActorId, string Reason)> ReadFinancialAccessAudit(string actorId, CancellationToken ct)
    {
        var audit = await Context.Set<PaymentAuditRow>().AsNoTracking().SingleAsync(
            row => row.ActorId == actorId && row.Operation == "FinancialSensitiveAccess", ct);
        return (audit.ResourceId, audit.Revision, audit.ActorId, audit.Reason);
    }
    /// <summary>Reads the protected (encrypted) state bytes stored for the payment account with <paramref name="id" />.</summary>
    public async Task<byte[]> AccountCiphertext(Guid id, CancellationToken ct) => await Context.Set<PaymentAccountRow>().Where(row => row.Id == id).Select(row => row.ProtectedState).SingleAsync(ct);
    /// <summary>Loads the tour that contains the booking with <paramref name="bookingId" />, or null when none exists.</summary>
    public Task<Tour?> ReloadTour(Guid bookingId, CancellationToken ct) => _provider.GetRequiredService<ITourStore>().GetByBookingId(bookingId, ct);
    /// <summary>Resolves the payment account query service.</summary>
    public IPaymentAccountQueryService Accounts => _provider.GetRequiredService<IPaymentAccountQueryService>();
    /// <summary>Resolves the payment financial query service.</summary>
    public IPaymentFinancialQueryService Financials => _provider.GetRequiredService<IPaymentFinancialQueryService>();
    /// <summary>Disposes the underlying service provider.</summary>
    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
