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

internal sealed class PaymentTestSession : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private AdminWriteDbContext Context => _provider.GetRequiredService<AdminWriteDbContext>();
    private PaymentTestSession(ServiceProvider provider) => _provider = provider;

    internal static PaymentTestSession Create(string connectionString, PaymentProtection protection, TimeProvider clock)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.AddApplication();
        var services = builder.Services;
        services.AddSingleton<PandaBear.Branding.IBrandingApiClient, PaymentTestBrandingClient>();
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

    public async Task Migrate(CancellationToken ct, string? target = null) => await Context.GetService<IMigrator>().MigrateAsync(target, ct);
    public async Task Seed(Tour tour, CancellationToken ct)
    {
        Context.Tours.Add(tour);
        foreach (var booking in tour.Bookings.Where(item => item.FinancialHistory is not null))
        {
            await _provider.GetRequiredService<IPaymentHistoryStore>().Save(tour, booking.Id, ct);
        }
        await Context.SaveEntities(ct);
    }
    public async Task SeedAccount(Domain.PaymentAccounts.PaymentAccount account, CancellationToken ct)
    {
        await _provider.GetRequiredService<IPaymentAccountStore>().Save(account, ct);
        await Context.SaveEntities(ct);
    }
    public PaymentInstructionWorkflow Instructions => _provider.GetRequiredService<PaymentInstructionWorkflow>();
    public Task<bool> HasPaymentTable(CancellationToken ct) => Context.Database.SqlQueryRaw<bool>(
        """
        SELECT to_regclass('public."Payment"') IS NOT NULL AS "Value"
        """).SingleAsync(ct);
    public async Task<int> AccountCount(CancellationToken ct) => await Context.Set<PaymentAccountRow>().CountAsync(ct);
    public async Task<int> AuditCount(CancellationToken ct) => await Context.Set<PaymentAuditRow>().CountAsync(ct);
    public async Task<byte[]> AccountCiphertext(Guid id, CancellationToken ct) => await Context.Set<PaymentAccountRow>().Where(row => row.Id == id).Select(row => row.ProtectedState).SingleAsync(ct);
    public Task<Tour?> ReloadTour(Guid bookingId, CancellationToken ct) => _provider.GetRequiredService<ITourStore>().GetByBookingId(bookingId, ct);
    public IPaymentAccountQueryService Accounts => _provider.GetRequiredService<IPaymentAccountQueryService>();
    public IPaymentFinancialQueryService Financials => _provider.GetRequiredService<IPaymentFinancialQueryService>();
    public IPaymentTransaction Transaction => _provider.GetRequiredService<IPaymentTransaction>();
    public AllocateBookingPaymentCommandHandler AllocateBookingPayment => _provider.GetRequiredService<AllocateBookingPaymentCommandHandler>();
    public ApproveBookingPaymentQuoteCommandHandler ApproveBookingPaymentQuote => _provider.GetRequiredService<ApproveBookingPaymentQuoteCommandHandler>();
    public ApproveBookingPaymentRefundCommandHandler ApproveBookingPaymentRefund => _provider.GetRequiredService<ApproveBookingPaymentRefundCommandHandler>();
    public ApproveBookingPaymentScheduleCommandHandler ApproveBookingPaymentSchedule => _provider.GetRequiredService<ApproveBookingPaymentScheduleCommandHandler>();
    public ApproveBookingReceiptReconciliationCommandHandler ApproveBookingReceiptReconciliation => _provider.GetRequiredService<ApproveBookingReceiptReconciliationCommandHandler>();
    public ApprovePaymentInstructionsCommandHandler ApprovePaymentInstructions => _provider.GetRequiredService<ApprovePaymentInstructionsCommandHandler>();
    public CorrectBookingPaymentRefundCommandHandler CorrectBookingPaymentRefund => _provider.GetRequiredService<CorrectBookingPaymentRefundCommandHandler>();
    public CreatePaymentAccountCommandHandler CreatePaymentAccount => _provider.GetRequiredService<CreatePaymentAccountCommandHandler>();
    public DraftBookingPaymentQuoteCommandHandler DraftBookingPaymentQuote => _provider.GetRequiredService<DraftBookingPaymentQuoteCommandHandler>();
    public DraftBookingPaymentRefundCommandHandler DraftBookingPaymentRefund => _provider.GetRequiredService<DraftBookingPaymentRefundCommandHandler>();
    public DraftBookingPaymentScheduleCommandHandler DraftBookingPaymentSchedule => _provider.GetRequiredService<DraftBookingPaymentScheduleCommandHandler>();
    public DraftBookingReceiptReconciliationCommandHandler DraftBookingReceiptReconciliation => _provider.GetRequiredService<DraftBookingReceiptReconciliationCommandHandler>();
    public FinalizePaymentInstructionsCommandHandler FinalizePaymentInstructions => _provider.GetRequiredService<FinalizePaymentInstructionsCommandHandler>();
    public GeneratePaymentInstructionsCommandHandler GeneratePaymentInstructions => _provider.GetRequiredService<GeneratePaymentInstructionsCommandHandler>();
    public RecordBookingReceiptCommandHandler RecordBookingReceipt => _provider.GetRequiredService<RecordBookingReceiptCommandHandler>();
    public ReleaseBookingPaymentAllocationsCommandHandler ReleaseBookingPaymentAllocations => _provider.GetRequiredService<ReleaseBookingPaymentAllocationsCommandHandler>();
    public RetirePaymentAccountCommandHandler RetirePaymentAccount => _provider.GetRequiredService<RetirePaymentAccountCommandHandler>();
    public ReverseBookingReceiptCommandHandler ReverseBookingReceipt => _provider.GetRequiredService<ReverseBookingReceiptCommandHandler>();
    public ReviewBookingFinancialRecordCommandHandler ReviewBookingFinancialRecord => _provider.GetRequiredService<ReviewBookingFinancialRecordCommandHandler>();
    public ReviewPaymentAccountCommandHandler ReviewPaymentAccount => _provider.GetRequiredService<ReviewPaymentAccountCommandHandler>();
    public ReviewPaymentInstructionsCommandHandler ReviewPaymentInstructions => _provider.GetRequiredService<ReviewPaymentInstructionsCommandHandler>();
    public UpdatePaymentAccountCommandHandler UpdatePaymentAccount => _provider.GetRequiredService<UpdatePaymentAccountCommandHandler>();
    public VerifyPaymentAccountCommandHandler VerifyPaymentAccount => _provider.GetRequiredService<VerifyPaymentAccountCommandHandler>();
    public VoidPaymentInstructionsCommandHandler VoidPaymentInstructions => _provider.GetRequiredService<VoidPaymentInstructionsCommandHandler>();
    public WithdrawBookingFinancialApprovalCommandHandler WithdrawBookingFinancialApproval => _provider.GetRequiredService<WithdrawBookingFinancialApprovalCommandHandler>();
    public WithdrawPaymentAccountVerificationCommandHandler WithdrawPaymentAccountVerification => _provider.GetRequiredService<WithdrawPaymentAccountVerificationCommandHandler>();
    public WithdrawPaymentInstructionApprovalCommandHandler WithdrawPaymentInstructionApproval => _provider.GetRequiredService<WithdrawPaymentInstructionApprovalCommandHandler>();
    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
