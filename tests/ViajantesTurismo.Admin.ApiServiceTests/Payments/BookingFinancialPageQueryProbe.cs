using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Captures the authorized booking, section and actor passed to financial read queries.</summary>
internal sealed class BookingFinancialPageQueryProbe : IPaymentFinancialQueryService
{
    /// <summary>Gets the last authorized projection read.</summary>
    public (Guid BookingId, BookingFinancialPageRequestDto Request, string? ActorId)? Read
    {
        get; private set;
    }

    /// <summary>Creates an endpoint test host with the requested workflow feature flag.</summary>
    public WebApplicationFactory<AdminApiHostEntryPoint> CreateHost(bool enabled) =>
        AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IPaymentFinancialQueryService>();
            services.AddSingleton<IPaymentFinancialQueryService>(this);
        }, configureConfiguration: configuration => configuration["Payments:Enabled"] = enabled.ToString());

    /// <inheritdoc />
    public Task<GetBookingFinancialHistoryDto?> GetByBookingId(Guid bookingId, CancellationToken ct) =>
        Task.FromResult<GetBookingFinancialHistoryDto?>(null);

    /// <inheritdoc />
    public Task<GetBookingFinancialHistoryDto?> GetSensitiveDetails(Guid bookingId, string actorId, CancellationToken ct) =>
        Task.FromResult<GetBookingFinancialHistoryDto?>(null);

    /// <inheritdoc />
    public Task<GetBookingFinancialPageDto?> GetPage(Guid bookingId, BookingFinancialPageRequestDto request, CancellationToken ct) =>
        Capture(bookingId, request, null);

    /// <inheritdoc />
    public Task<GetBookingFinancialPageDto?> GetSensitivePage(Guid bookingId, string actorId, BookingFinancialPageRequestDto request, CancellationToken ct) =>
        Capture(bookingId, request, actorId);

    private Task<GetBookingFinancialPageDto?> Capture(Guid bookingId, BookingFinancialPageRequestDto request, string? actorId)
    {
        Read = (bookingId, request, actorId);
        return Task.FromResult<GetBookingFinancialPageDto?>(new GetBookingFinancialPageDto
        {
            History = new GetBookingFinancialHistoryDto
            {
                BookingId = bookingId,
                Version = 12,
                ObligationCurrency = "EUR",
                TermsStale = true,
                ApprovedObligationEuro = 0,
                OutstandingEuro = 0,
                UnallocatedEuroCredit = 0,
                BusinessDate = new DateOnly(2030, 1, 5),
                Overdue = false,
                Schedules = [],
                Quotes = [],
                Receipts = [],
                Reconciliations = [],
                Allocations = [],
                Refunds = [],
                Reversals = [],
                Reviews = []
            },
            Windows = []
        });
    }
}
