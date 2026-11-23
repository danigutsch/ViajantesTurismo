using PandaBear.BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Bookings;
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
    /// <inheritdoc />
    public async Task<GetBookingFinancialHistoryDto?> GetByBookingId(Guid bookingId, CancellationToken ct) =>
        (await Read(bookingId, false, null, null, ct))?.History;

    /// <inheritdoc />
    public async Task<GetBookingFinancialHistoryDto?> GetSensitiveDetails(Guid bookingId, string actorId, CancellationToken ct) =>
        (await Read(bookingId, true, actorId, null, ct))?.History;

    /// <inheritdoc />
    public Task<GetBookingFinancialPageDto?> GetPage(Guid bookingId, BookingFinancialPageRequestDto request, CancellationToken ct) =>
        Read(bookingId, false, null, request, ct);

    /// <inheritdoc />
    public Task<GetBookingFinancialPageDto?> GetSensitivePage(Guid bookingId, string actorId, BookingFinancialPageRequestDto request, CancellationToken ct) =>
        Read(bookingId, true, actorId, request, ct);

    private async Task<GetBookingFinancialPageDto?> Read(Guid bookingId, bool sensitive, string? actorId, BookingFinancialPageRequestDto? request, CancellationToken ct)
    {
        var booking = await reads.Set<Booking>().SingleOrDefaultAsync(item => item.Id == bookingId, ct);
        if (booking is null)
        {
            return null;
        }
        var currency = await reads.Tours.Where(tour => tour.Id == booking.TourId).Select(tour => tour.Pricing.Currency).SingleAsync(ct);
        var row = await reads.Set<PaymentHistoryRow>().SingleOrDefaultAsync(item => item.BookingId == bookingId, ct);
        var history = row is null ? null : histories.Decode(row);
        var projection = request is null ? null : new BookingFinancialPageProjection(request);
        var summary = MapHistory(booking, currency, history, calendar.Today, sensitive, projection);
        if (projection?.ParentMissing == true)
        {
            return null;
        }
        var page = history is not null && projection is not null
            ? MapPage(history, summary, projection)
            : new GetBookingFinancialPageDto { History = summary, Windows = [] };
        if (sensitive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
            writes.Add(new PaymentAuditRow
            {
                Id = Guid.CreateVersion7(),
                ResourceId = bookingId,
                Revision = summary.Version,
                ActorId = actorId,
                OccurredAt = clock.GetUtcNow(),
                Operation = "FinancialSensitiveAccess",
                Reason = "SensitiveAccess"
            });
            await writes.SaveEntities(ct);
        }
        return page;
    }

    private static GetBookingFinancialHistoryDto MapHistory(Booking booking, Currency currency, BookingFinancialHistory? history, DateOnly businessDate, bool sensitive, BookingFinancialPageProjection? projection)
    {
        if (history is null)
        {
            return new GetBookingFinancialHistoryDto
            {
                BookingId = booking.Id,
                Version = 0,
                ObligationCurrency = currency.Code,
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
            BookingId = booking.Id,
            Version = history.Version,
            ObligationCurrency = Currency.Eur.Code,
            TermsStale = history.TermsAreStale(booking.TotalPrice),
            ApprovedObligationEuro = history.ApprovedSchedule?.Obligation.Amount ?? 0,
            OutstandingEuro = history.OutstandingEuro,
            UnallocatedEuroCredit = history.UnallocatedEuroCredit,
            BusinessDate = businessDate,
            Overdue = history.IsOverdue(businessDate),
            Schedules = [.. (projection?.Parents(state.Schedules, BookingFinancialSection.Schedules, BookingFinancialSection.ScheduleInstallments, item => item.Id) ?? state.Schedules).Select(item => new GetPaymentScheduleDto
            {
                Id = item.Id,
                Revision = item.Number,
                ObligationEuro = item.Obligation,
                BookingPriceEuro = item.BookingPrice,
                Status = item.Approval is null ? PaymentScheduleStatusDto.Draft : item.Superseded ? PaymentScheduleStatusDto.Superseded : PaymentScheduleStatusDto.Approved,
                ApprovalWithdrawn = history.IsApprovalWithdrawn(item.Id),
                Approval = Approval(item.CreatedBy, item.CreatedAt, item.Approval),
                Installments = [.. (projection?.Children(item.Installments, BookingFinancialSection.ScheduleInstallments, item.Id) ?? item.Installments).Select(installment => MapInstallment(history, installment))]
            })],
            Quotes = [.. (projection?.Items(state.Quotes, BookingFinancialSection.Quotes) ?? state.Quotes).Select(item => MapQuote(history, item, sensitive))],
            Receipts = [.. (projection?.Items(state.Receipts, BookingFinancialSection.Receipts) ?? state.Receipts).Select(item => MapReceipt(history, item, sensitive))],
            Reconciliations = [.. (projection?.Parents(state.Reconciliations, BookingFinancialSection.Reconciliations, BookingFinancialSection.ReconciliationSelections, item => item.Id) ?? state.Reconciliations).Select(item => new GetReceiptReconciliationDto
            {
                Id = item.Id,
                ReceiptId = item.ReceiptId,
                EuroCredit = item.EuroCredit,
                QuoteId = item.QuoteId,
                InstructionId = item.InstructionId,
                Allocations = Selections(projection?.Children(item.Allocations, BookingFinancialSection.ReconciliationSelections, item.Id) ?? item.Allocations),
                Approval = Approval(item.CreatedBy, item.CreatedAt, item.Approval)
            })],
            Allocations = [.. (projection?.Items(state.Allocations, BookingFinancialSection.Allocations) ?? state.Allocations).Select(item => MapAllocation(history, item))],
            Refunds = [.. (projection?.Parents(state.Refunds, BookingFinancialSection.Refunds, BookingFinancialSection.RefundSelections, item => item.Id) ?? state.Refunds).Select(item => new GetPaymentRefundDto
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
                ReversedAllocations = Selections(projection?.Children(item.ReversedAllocations, BookingFinancialSection.RefundSelections, item.Id) ?? item.ReversedAllocations),
                Approval = Approval(item.CreatedBy, item.CreatedAt, item.Approval)
            })],
            Reversals = MapReversals(state, projection),
            Reviews = [.. (projection?.Items(state.Reviews, BookingFinancialSection.Reviews) ?? state.Reviews).Select(item => new GetFinancialReviewDto
            {
                TargetId = item.TargetId,
                TargetRevision = item.TargetRevision,
                ReviewerId = item.ReviewerId,
                ReviewedAt = item.ReviewedAt,
                ChangesRequested = item.ChangesRequested
            })]
        };
    }

    private static GetBookingFinancialPageDto MapPage(BookingFinancialHistory history, GetBookingFinancialHistoryDto summary, BookingFinancialPageProjection projection)
    {
        var state = history.ExportState();
        var reviewable = summary.Schedules.Select(item => item.Id).Concat(summary.Quotes.Select(item => item.Id))
            .Concat(summary.Reconciliations.Select(item => item.Id)).Concat(summary.Refunds.Select(item => item.Id)).ToHashSet();
        var settlementChoices = projection.Requests(BookingFinancialSection.InstructionSettlementQuoteChoices)
            ? BookingFinancialSection.InstructionSettlementQuoteChoices : BookingFinancialSection.SettlementQuoteChoices;
        var installmentChoices = projection.Requests(BookingFinancialSection.InstructionOutstandingInstallmentChoices)
            ? BookingFinancialSection.InstructionOutstandingInstallmentChoices : BookingFinancialSection.ApprovedInstallmentChoices;
        return new GetBookingFinancialPageDto
        {
            History = summary,
            Windows = projection.Windows,
            AttentionRequested = reviewable.Count == 0 ? [] : [.. state.Reviews.Reverse().Where(item => reviewable.Contains(item.TargetId))
                .DistinctBy(item => item.TargetId).Where(item => item.ChangesRequested).Select(item => item.TargetId)],
            ReceiptChoices = [.. projection.Items(state.Receipts, BookingFinancialSection.ReceiptChoices).Select(item => MapReceipt(history, item, false))],
            SettlementQuoteChoices = [.. MapQuoteChoices(settlementChoices, false)],
            RefundQuoteChoices = [.. MapQuoteChoices(BookingFinancialSection.RefundQuoteChoices, true)],
            ApprovedInstallmentChoices = [.. MapInstallmentChoices(installmentChoices)],
            ProposedInstallmentChoices = [.. projection.Items(state.Schedules.LastOrDefault()?.Installments ?? [], BookingFinancialSection.ProposedInstallmentChoices).Select(item => MapInstallment(history, item))],
            ActiveAllocationChoices = projection.Includes(BookingFinancialSection.ActiveAllocationChoices)
                ? [.. projection.Items(state.Allocations.Where(item => history.GetAllocationActiveEuro(item.Id) > 0).ToArray(), BookingFinancialSection.ActiveAllocationChoices).Select(item => MapAllocation(history, item))]
                : []
        };

        IEnumerable<GetPaymentQuoteDto> MapQuoteChoices(BookingFinancialSection section, bool forRefund) =>
            projection.Includes(section)
                ? projection.Items(state.Quotes.Where(item => item.ForRefund == forRefund &&
                    (section != BookingFinancialSection.InstructionSettlementQuoteChoices ||
                     (item.Approval is not null && !history.IsApprovalWithdrawn(item.Id)))).ToArray(), section).Select(item => MapQuote(history, item, false))
                : [];

        IEnumerable<GetPaymentInstallmentDto> MapInstallmentChoices(BookingFinancialSection section)
        {
            if (!projection.Includes(section))
            {
                return [];
            }

            var installments = state.Schedules.LastOrDefault(item => item.Approval is not null && !item.Superseded)?.Installments ?? [];
            if (section == BookingFinancialSection.InstructionOutstandingInstallmentChoices)
            {
                installments = [.. installments.Where(item => item.Amount > history.GetInstallmentAllocatedEuro(item.Id))];
            }

            return projection.Items(installments, section).Select(item => MapInstallment(history, item));
        }
    }

    private static IReadOnlyList<GetFinancialReversalDto> MapReversals(BookingFinancialState state, BookingFinancialPageProjection? projection)
    {
        if (projection?.Requests(BookingFinancialSection.ReversalLinks) == true)
        {
            return [.. state.Reversals.Where(item => item.Id == projection.ParentId).Select(ReceiptReversal)];
        }

        var count = state.Reversals.Length + state.RefundCorrections.Length + state.Withdrawals.Length + state.Releases.Length;
        return [.. (projection?.Reversals(count) ?? Enumerable.Range(0, count)).Select(At)];

        GetFinancialReversalDto At(int index)
        {
            if (index < state.Reversals.Length)
            {
                return ReceiptReversal(state.Reversals[index]);
            }
            index -= state.Reversals.Length;
            if (index < state.RefundCorrections.Length)
            {
                var item = state.RefundCorrections[index];
                return Reversal(item.Id, item.RefundId, "RefundRecordCorrected", item.Approval);
            }
            index -= state.RefundCorrections.Length;
            if (index < state.Withdrawals.Length)
            {
                var item = state.Withdrawals[index];
                return Reversal(item.Id, item.TargetId, "ApprovalWithdrawn", item.Approval);
            }
            var release = state.Releases[index - state.Withdrawals.Length];
            return Reversal(release.Id, release.AllocationId, "AllocationReleased", release.Approval) with
            {
                EuroAmount = release.EuroAmount
            };
        }

        GetFinancialReversalDto ReceiptReversal(PaymentReceiptReversalState item) => Reversal(item.Id, item.ReceiptId, "ReceiptReversed", item.Approval) with
        {
            AllocationIds = projection is null ? item.AllocationIds : [.. projection.Children(item.AllocationIds, BookingFinancialSection.ReversalLinks, item.Id)]
        };
    }

    private static GetFinancialReversalDto Reversal(Guid id, Guid targetId, string operation, FinancialApprovalState approval) => new()
    {
        Id = id,
        TargetId = targetId,
        Operation = operation,
        ApprovedBy = approval.ActorId,
        ApprovedAt = approval.ApprovedAt,
        Reason = FinancialMappings.Map(approval.Reason)
    };

    private static GetPaymentInstallmentDto MapInstallment(BookingFinancialHistory history, PaymentInstallmentState item)
    {
        var allocated = history.GetInstallmentAllocatedEuro(item.Id);
        return new GetPaymentInstallmentDto
        {
            Id = item.Id,
            EuroAmount = item.Amount,
            DueDate = item.DueDate,
            AllocatedEuro = allocated,
            OutstandingEuro = item.Amount - allocated
        };
    }

    private static GetPaymentQuoteDto MapQuote(BookingFinancialHistory history, BookingQuoteState item, bool sensitive) => new()
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
    };

    private static GetBookingReceiptDto MapReceipt(BookingFinancialHistory history, PaymentReceiptState item, bool sensitive) => new()
    {
        Id = item.Id,
        SettlementCurrency = item.Currency.Code,
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
    };

    private static GetPaymentAllocationDto MapAllocation(BookingFinancialHistory history, PaymentAllocationState item) => new()
    {
        Id = item.Id,
        ReceiptId = item.ReceiptId,
        InstallmentId = item.InstallmentId,
        OriginalEuroCredit = item.EuroAmount,
        ActiveEuroCredit = history.GetAllocationActiveEuro(item.Id)
    };

    private static FinancialApprovalDto Approval(string createdBy, DateTimeOffset createdAt, FinancialApprovalState? approval) => new()
    {
        CreatedBy = createdBy,
        CreatedAt = createdAt,
        ApprovedBy = approval?.ActorId,
        ApprovedAt = approval?.ApprovedAt,
        ApprovalReason = approval is null ? null : FinancialMappings.Map(approval.Reason),
        SelfApproved = approval?.ActorId == createdBy
    };
    private static InstallmentAllocationDto[] Selections(IEnumerable<PaymentSelectionState> selections) => [.. selections.Select(item => new InstallmentAllocationDto { TargetId = item.TargetId, EuroAmount = item.EuroAmount })];
    private static string Display(string value, bool sensitive) => sensitive ? value : value.Length > 4 ? "…" + value[^4..] : new string('*', value.Length);
}
