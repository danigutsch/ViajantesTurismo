using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Reads booking financial histories.</summary>
/// <param name="reads">The read database context.</param>
/// <param name="writes">The write context used for sensitive access audits.</param>
/// <param name="histories">Decodes protected histories.</param>
/// <param name="calendar">Supplies the business date.</param>
/// <param name="clock">The clock for audit timestamps.</param>
internal sealed class PaymentFinancialQueryService(AdminReadDbContext reads, AdminWriteDbContext writes, PaymentHistoryStore histories, PaymentBusinessCalendar calendar, TimeProvider clock) : IPaymentFinancialQueryService
{
    /// <summary>Returns the booking financial history, or null when the booking does not exist.</summary>
    public Task<GetBookingFinancialHistoryDto?> GetByBookingId(Guid bookingId, CancellationToken ct) => Read(bookingId, false, null, ct);
    /// <summary>Returns the booking financial history and records a sensitive access audit for the actor.</summary>
    public Task<GetBookingFinancialHistoryDto?> GetSensitiveDetails(Guid bookingId, string actorId, CancellationToken ct) => Read(bookingId, true, actorId, ct);

    private async Task<GetBookingFinancialHistoryDto?> Read(Guid bookingId, bool sensitive, string? actorId, CancellationToken ct)
    {
        var booking = await reads.Set<Booking>().SingleOrDefaultAsync(item => item.Id == bookingId, ct);
        if (booking is null)
        {
            return null;
        }
        var currency = await reads.Tours.Where(tour => tour.Id == booking.TourId).Select(tour => tour.Pricing.Currency).SingleAsync(ct);
        var row = await reads.Set<PaymentHistoryRow>().SingleOrDefaultAsync(item => item.BookingId == bookingId, ct);
        var history = row is null ? null : histories.Decode(row);
        if (sensitive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
            writes.Add(new PaymentAuditRow
            {
                Id = Guid.CreateVersion7(),
                ResourceId = bookingId,
                Revision = history?.Version ?? 0,
                ActorId = actorId,
                OccurredAt = clock.GetUtcNow(),
                Operation = "FinancialSensitiveAccess",
                Reason = "SensitiveAccess"
            });
            await writes.SaveEntities(ct);
        }
        var businessDate = calendar.Today;
        if (history is null)
        {
            return new GetBookingFinancialHistoryDto
            {
                BookingId = bookingId,
                Version = 0,
                ObligationCurrency = TourMapper.MapToCurrencyDto(currency),
                TermsStale = true,
                ApprovedObligationEuro = 0,
                OutstandingEuro = 0,
                UnallocatedEuroCredit = 0,
                BusinessDate = businessDate,
                Overdue = false,
                Schedules = [],
                Quotes = [],
                Receipts = [],
                Reconciliations = [],
                Allocations = [],
                Refunds = [],
                Reversals = [],
                Reviews = []
            };
        }
        var state = history.ExportState();
        return new GetBookingFinancialHistoryDto
        {
            BookingId = bookingId,
            Version = history.Version,
            ObligationCurrency = CurrencyDto.Euro,
            TermsStale = history.TermsAreStale(booking.TotalPrice),
            ApprovedObligationEuro = history.ApprovedSchedule?.Obligation.Amount ?? 0,
            OutstandingEuro = history.OutstandingEuro,
            UnallocatedEuroCredit = history.UnallocatedEuroCredit,
            BusinessDate = businessDate,
            Overdue = history.IsOverdue(businessDate),
            Schedules = [.. state.Schedules.Select(item => new GetPaymentScheduleDto
            {
                Id = item.Id,
                Revision = item.Number,
                ObligationEuro = item.Obligation,
                BookingPriceEuro = item.BookingPrice,
                Status = item.Approval is null ? PaymentScheduleStatusDto.Draft : item.Superseded ? PaymentScheduleStatusDto.Superseded : PaymentScheduleStatusDto.Approved,
                ApprovalWithdrawn = history.IsApprovalWithdrawn(item.Id),
                Approval = Approval(item.CreatedBy, item.CreatedAt, item.Approval),
                Installments = [.. item.Installments.Select(installment => new GetPaymentInstallmentDto
                {
                    Id = installment.Id,
                    EuroAmount = installment.Amount,
                    DueDate = installment.DueDate,
                    AllocatedEuro = history.GetInstallmentAllocatedEuro(installment.Id),
                    OutstandingEuro = installment.Amount - history.GetInstallmentAllocatedEuro(installment.Id)
                })]
            })],
            Quotes = [.. state.Quotes.Select(item => new GetPaymentQuoteDto
            {
                Id = item.Id,
                BrlPerEuro = item.BrlPerEuro,
                Source = Display(item.Source, sensitive),
                Reference = Display(item.Reference, sensitive),
                AgreedAt = item.AgreedAt,
                ExpiresAt = item.ExpiresAt,
                ForRefund = item.ForRefund,
                ApprovalWithdrawn = history.IsApprovalWithdrawn(item.Id),
                Approval = Approval(item.CreatedBy, item.CreatedAt, item.Approval)
            })],
            Receipts = [.. state.Receipts.Select(item => new GetBookingReceiptDto
            {
                Id = item.Id,
                SettlementCurrency = TourMapper.MapToCurrencyDto(item.Currency),
                Gross = item.Gross,
                Fees = item.Fees,
                Net = item.Gross - item.Fees,
                PaymentDate = item.PaymentDate,
                SettledAt = item.SettledAt,
                Reference = Display(item.Reference, sensitive),
                CreatedBy = item.CreatedBy,
                CreatedAt = item.CreatedAt,
                Method = BookingMapper.MapToPaymentMethodDto(item.Method),
                Reversed = history.IsReceiptReversed(item.Id),
                PendingReconciliation = history.IsReceiptPending(item.Id),
                ApprovedEuroCredit = history.GetReceiptApprovedEuro(item.Id),
                UnallocatedEuroCredit = history.GetReceiptUnallocatedEuro(item.Id),
                UnallocatedCashCredit = history.UnallocatedCashCredit(item.Id),
                RefundableCashBasis = history.GetReceiptRefundableCash(item.Id),
                SettlementDifference = history.GetReceiptSettlementDifference(item.Id)
            })],
            Reconciliations = [.. state.Reconciliations.Select(item => new GetReceiptReconciliationDto
            {
                Id = item.Id,
                ReceiptId = item.ReceiptId,
                EuroCredit = item.EuroCredit,
                QuoteId = item.QuoteId,
                InstructionId = item.InstructionId,
                Allocations = Selections(item.Allocations),
                Approval = Approval(item.CreatedBy, item.CreatedAt, item.Approval)
            })],
            Allocations = [.. state.Allocations.Select(item => new GetPaymentAllocationDto
            {
                Id = item.Id,
                ReceiptId = item.ReceiptId,
                InstallmentId = item.InstallmentId,
                OriginalEuroCredit = item.EuroAmount,
                ActiveEuroCredit = history.GetAllocationActiveEuro(item.Id)
            })],
            Refunds = [.. state.Refunds.Select(item => new GetPaymentRefundDto
            {
                Id = item.Id,
                ReceiptId = item.ReceiptId,
                OriginalCashBasis = item.OriginalCashBasis,
                EuroCreditReversed = item.EuroCreditReversed,
                EuroCashPayable = item.EuroCashPayable,
                ActualSettlement = item.ActualSettlement,
                FxDifference = item.FxDifference,
                QuoteId = item.QuoteId,
                Reference = Display(item.Reference, sensitive),
                PerformedAt = item.PerformedAt,
                Corrected = history.IsRefundCorrected(item.Id),
                ReversedAllocations = Selections(item.ReversedAllocations),
                Approval = Approval(item.CreatedBy, item.CreatedAt, item.Approval)
            })],
            Reversals =
            [
                .. state.Reversals.Select(item => new GetFinancialReversalDto
                            {
                                Id = item.Id,
                                TargetId = item.ReceiptId,
                                Operation = "ReceiptReversed",
                                ApprovedBy = item.Approval.ActorId,
                                ApprovedAt = item.Approval.ApprovedAt,
                                Reason = FinancialMappings.Map(item.Approval.Reason),
                                AllocationIds = item.AllocationIds
                            })
,
                .. state.RefundCorrections.Select(item => new GetFinancialReversalDto
                        {
                            Id = item.Id,
                            TargetId = item.RefundId,
                            Operation = "RefundRecordCorrected",
                            ApprovedBy = item.Approval.ActorId,
                            ApprovedAt = item.Approval.ApprovedAt,
                            Reason = FinancialMappings.Map(item.Approval.Reason)
                        }),
                .. state.Withdrawals.Select(item => new GetFinancialReversalDto
                        {
                            Id = item.Id,
                            TargetId = item.TargetId,
                            Operation = "ApprovalWithdrawn",
                            ApprovedBy = item.Approval.ActorId,
                            ApprovedAt = item.Approval.ApprovedAt,
                            Reason = FinancialMappings.Map(item.Approval.Reason)
                        }),
                .. state.Releases.Select(item => new GetFinancialReversalDto
                        {
                            Id = item.Id,
                            TargetId = item.AllocationId,
                            Operation = "AllocationReleased",
                            EuroAmount = item.EuroAmount,
                            ApprovedBy = item.Approval.ActorId,
                            ApprovedAt = item.Approval.ApprovedAt,
                            Reason = FinancialMappings.Map(item.Approval.Reason)
                        }),
            ],
            Reviews = [.. state.Reviews.Select(item => new GetFinancialReviewDto
            {
                TargetId = item.TargetId,
                TargetRevision = item.TargetRevision,
                ReviewerId = item.ReviewerId,
                ReviewedAt = item.ReviewedAt,
                ChangesRequested = item.ChangesRequested
            })]
        };
    }

    private static FinancialApprovalDto Approval(string createdBy, DateTimeOffset createdAt, FinancialApprovalState? approval) => new()
    {
        CreatedBy = createdBy,
        CreatedAt = createdAt,
        ApprovedBy = approval?.ActorId,
        ApprovedAt = approval?.ApprovedAt,
        ApprovalReason = approval is null ? null : FinancialMappings.Map(approval.Reason),
        SelfApproved = approval?.ActorId == createdBy
    };
    private static InstallmentAllocationDto[] Selections(PaymentSelectionState[] selections) => [.. selections.Select(item => new InstallmentAllocationDto { TargetId = item.TargetId, EuroAmount = item.EuroAmount })];
    private static string Display(string value, bool sensitive) => sensitive ? value : value.Length > 4 ? "…" + value[^4..] : new string('*', value.Length);
}
