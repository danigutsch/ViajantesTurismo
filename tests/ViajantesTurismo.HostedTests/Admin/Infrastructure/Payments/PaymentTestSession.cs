using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

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
        services.AddSingleton<Application.Branding.IBrandingProjectionStore, PaymentTestBrandingProjection>();
        services.AddSingleton(clock);
        services.AddSingleton(protection);
        services.AddSingleton(new PaymentBusinessCalendar(clock, TimeZoneInfo.Utc));
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        services.AddDbContext<AdminWriteDbContext>((provider, options) => options.UseNpgsql(connectionString));
        services.AddDbContext<AdminReadDbContext>(options => options.UseNpgsql(connectionString).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AdminWriteDbContext>());
        services.AddScoped<IPaymentTransaction, PaymentTransaction>();
        services.AddScoped<IPaymentAccountStore, PaymentAccountStore>();
        services.AddScoped<IPaymentAccountQueryService, PaymentAccountQueryService>();
        services.AddScoped<PaymentHistoryStore>();
        services.AddScoped<IPaymentHistoryStore>(provider => provider.GetRequiredService<PaymentHistoryStore>());
        services.AddScoped<ITourStore, TourStore>();
        services.AddScoped<IDocumentStore, DocumentStore>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();
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
    public async Task SeedAccount(Domain.PaymentAccounts.PaymentAccount account, CancellationToken ct)
    {
        await _provider.GetRequiredService<IPaymentAccountStore>().Save(account, ct);
        await Context.SaveEntities(ct);
    }
    /// <summary>Resolves the payment instruction workflow.</summary>
    public PaymentInstructionWorkflow Instructions => _provider.GetRequiredService<PaymentInstructionWorkflow>();
    /// <summary>Checks whether the legacy <c>Payment</c> table exists in the public schema.</summary>
    public Task<bool> HasPaymentTable(CancellationToken ct) => Context.Database.SqlQueryRaw<bool>(
        """
        SELECT to_regclass('public."Payment"') IS NOT NULL AS "Value"
        """).SingleAsync(ct);
    /// <summary>Counts persisted payment account rows.</summary>
    public async Task<int> AccountCount(CancellationToken ct) => await Context.Set<PaymentAccountRow>().CountAsync(ct);
    /// <summary>Counts persisted payment audit rows.</summary>
    public async Task<int> AuditCount(CancellationToken ct) => await Context.Set<PaymentAuditRow>().CountAsync(ct);
    /// <summary>Reads the protected (encrypted) state bytes stored for the payment account with <paramref name="id" />.</summary>
    public async Task<byte[]> AccountCiphertext(Guid id, CancellationToken ct) => await Context.Set<PaymentAccountRow>().Where(row => row.Id == id).Select(row => row.ProtectedState).SingleAsync(ct);
    /// <summary>Loads the tour that contains the booking with <paramref name="bookingId" />, or null when none exists.</summary>
    public Task<Tour?> ReloadTour(Guid bookingId, CancellationToken ct) => _provider.GetRequiredService<ITourStore>().GetByBookingId(bookingId, ct);
    /// <summary>Resolves the payment account query service.</summary>
    public IPaymentAccountQueryService Accounts => _provider.GetRequiredService<IPaymentAccountQueryService>();
    /// <summary>Resolves the payment financial query service.</summary>
    public IPaymentFinancialQueryService Financials => _provider.GetRequiredService<IPaymentFinancialQueryService>();
    /// <summary>Resolves the payment transaction.</summary>
    public IPaymentTransaction Transaction => _provider.GetRequiredService<IPaymentTransaction>();
    /// <summary>Resolves the handler that runs the allocate booking payment command.</summary>
    public AllocateBookingPaymentCommandHandler AllocateBookingPayment => _provider.GetRequiredService<AllocateBookingPaymentCommandHandler>();
    /// <summary>Resolves the handler that runs the approve booking payment quote command.</summary>
    public ApproveBookingPaymentQuoteCommandHandler ApproveBookingPaymentQuote => _provider.GetRequiredService<ApproveBookingPaymentQuoteCommandHandler>();
    /// <summary>Resolves the handler that runs the approve booking payment refund command.</summary>
    public ApproveBookingPaymentRefundCommandHandler ApproveBookingPaymentRefund => _provider.GetRequiredService<ApproveBookingPaymentRefundCommandHandler>();
    /// <summary>Resolves the handler that runs the approve booking payment schedule command.</summary>
    public ApproveBookingPaymentScheduleCommandHandler ApproveBookingPaymentSchedule => _provider.GetRequiredService<ApproveBookingPaymentScheduleCommandHandler>();
    /// <summary>Resolves the handler that runs the approve booking receipt reconciliation command.</summary>
    public ApproveBookingReceiptReconciliationCommandHandler ApproveBookingReceiptReconciliation => _provider.GetRequiredService<ApproveBookingReceiptReconciliationCommandHandler>();
    /// <summary>Resolves the handler that runs the approve payment instructions command.</summary>
    public ApprovePaymentInstructionsCommandHandler ApprovePaymentInstructions => _provider.GetRequiredService<ApprovePaymentInstructionsCommandHandler>();
    /// <summary>Resolves the handler that runs the correct booking payment refund command.</summary>
    public CorrectBookingPaymentRefundCommandHandler CorrectBookingPaymentRefund => _provider.GetRequiredService<CorrectBookingPaymentRefundCommandHandler>();
    /// <summary>Resolves the handler that runs the create payment account command.</summary>
    public CreatePaymentAccountCommandHandler CreatePaymentAccount => _provider.GetRequiredService<CreatePaymentAccountCommandHandler>();
    /// <summary>Resolves the handler that runs the draft booking payment quote command.</summary>
    public DraftBookingPaymentQuoteCommandHandler DraftBookingPaymentQuote => _provider.GetRequiredService<DraftBookingPaymentQuoteCommandHandler>();
    /// <summary>Resolves the handler that runs the draft booking payment refund command.</summary>
    public DraftBookingPaymentRefundCommandHandler DraftBookingPaymentRefund => _provider.GetRequiredService<DraftBookingPaymentRefundCommandHandler>();
    /// <summary>Resolves the handler that runs the draft booking payment schedule command.</summary>
    public DraftBookingPaymentScheduleCommandHandler DraftBookingPaymentSchedule => _provider.GetRequiredService<DraftBookingPaymentScheduleCommandHandler>();
    /// <summary>Resolves the handler that runs the draft booking receipt reconciliation command.</summary>
    public DraftBookingReceiptReconciliationCommandHandler DraftBookingReceiptReconciliation => _provider.GetRequiredService<DraftBookingReceiptReconciliationCommandHandler>();
    /// <summary>Resolves the handler that runs the finalize payment instructions command.</summary>
    public FinalizePaymentInstructionsCommandHandler FinalizePaymentInstructions => _provider.GetRequiredService<FinalizePaymentInstructionsCommandHandler>();
    /// <summary>Resolves the handler that runs the generate payment instructions command.</summary>
    public GeneratePaymentInstructionsCommandHandler GeneratePaymentInstructions => _provider.GetRequiredService<GeneratePaymentInstructionsCommandHandler>();
    /// <summary>Resolves the handler that runs the record booking receipt command.</summary>
    public RecordBookingReceiptCommandHandler RecordBookingReceipt => _provider.GetRequiredService<RecordBookingReceiptCommandHandler>();
    /// <summary>Resolves the handler that runs the release booking payment allocations command.</summary>
    public ReleaseBookingPaymentAllocationsCommandHandler ReleaseBookingPaymentAllocations => _provider.GetRequiredService<ReleaseBookingPaymentAllocationsCommandHandler>();
    /// <summary>Resolves the handler that runs the retire payment account command.</summary>
    public RetirePaymentAccountCommandHandler RetirePaymentAccount => _provider.GetRequiredService<RetirePaymentAccountCommandHandler>();
    /// <summary>Resolves the handler that runs the reverse booking receipt command.</summary>
    public ReverseBookingReceiptCommandHandler ReverseBookingReceipt => _provider.GetRequiredService<ReverseBookingReceiptCommandHandler>();
    /// <summary>Resolves the handler that runs the review booking financial record command.</summary>
    public ReviewBookingFinancialRecordCommandHandler ReviewBookingFinancialRecord => _provider.GetRequiredService<ReviewBookingFinancialRecordCommandHandler>();
    /// <summary>Resolves the handler that runs the review payment account command.</summary>
    public ReviewPaymentAccountCommandHandler ReviewPaymentAccount => _provider.GetRequiredService<ReviewPaymentAccountCommandHandler>();
    /// <summary>Resolves the handler that runs the review payment instructions command.</summary>
    public ReviewPaymentInstructionsCommandHandler ReviewPaymentInstructions => _provider.GetRequiredService<ReviewPaymentInstructionsCommandHandler>();
    /// <summary>Resolves the handler that runs the update payment account command.</summary>
    public UpdatePaymentAccountCommandHandler UpdatePaymentAccount => _provider.GetRequiredService<UpdatePaymentAccountCommandHandler>();
    /// <summary>Resolves the handler that runs the verify payment account command.</summary>
    public VerifyPaymentAccountCommandHandler VerifyPaymentAccount => _provider.GetRequiredService<VerifyPaymentAccountCommandHandler>();
    /// <summary>Resolves the handler that runs the void payment instructions command.</summary>
    public VoidPaymentInstructionsCommandHandler VoidPaymentInstructions => _provider.GetRequiredService<VoidPaymentInstructionsCommandHandler>();
    /// <summary>Resolves the handler that runs the withdraw booking financial approval command.</summary>
    public WithdrawBookingFinancialApprovalCommandHandler WithdrawBookingFinancialApproval => _provider.GetRequiredService<WithdrawBookingFinancialApprovalCommandHandler>();
    /// <summary>Resolves the handler that runs the withdraw payment account verification command.</summary>
    public WithdrawPaymentAccountVerificationCommandHandler WithdrawPaymentAccountVerification => _provider.GetRequiredService<WithdrawPaymentAccountVerificationCommandHandler>();
    /// <summary>Resolves the handler that runs the withdraw payment instruction approval command.</summary>
    public WithdrawPaymentInstructionApprovalCommandHandler WithdrawPaymentInstructionApproval => _provider.GetRequiredService<WithdrawPaymentInstructionApprovalCommandHandler>();
    /// <summary>Disposes the underlying service provider.</summary>
    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
