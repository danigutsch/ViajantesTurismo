namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Identifies the retained financial records or action choices to page.</summary>
public enum BookingFinancialSection
{
    /// <summary>Retained schedule revisions.</summary>
    Schedules,
    /// <summary>Retained agreed quotes.</summary>
    Quotes,
    /// <summary>Retained receipts.</summary>
    Receipts,
    /// <summary>Retained receipt reconciliations.</summary>
    Reconciliations,
    /// <summary>Retained credit allocations.</summary>
    Allocations,
    /// <summary>Retained refund records.</summary>
    Refunds,
    /// <summary>Retained reversals, releases, corrections and approval withdrawals.</summary>
    Reversals,
    /// <summary>Retained independent reviews.</summary>
    Reviews,
    /// <summary>Receipts available to the financial forms.</summary>
    ReceiptChoices,
    /// <summary>Quotes recorded for original settlements.</summary>
    SettlementQuoteChoices,
    /// <summary>Quotes recorded for refunds.</summary>
    RefundQuoteChoices,
    /// <summary>Installments in the current approved schedule.</summary>
    ApprovedInstallmentChoices,
    /// <summary>Installments in the latest schedule revision.</summary>
    ProposedInstallmentChoices,
    /// <summary>Allocations with remaining active EUR credit.</summary>
    ActiveAllocationChoices,
    /// <summary>Installment terms belonging to a particular schedule revision.</summary>
    ScheduleInstallments,
    /// <summary>Selected allocations belonging to a reconciliation proposal.</summary>
    ReconciliationSelections,
    /// <summary>Selected allocations reversed by a refund.</summary>
    RefundSelections,
    /// <summary>Allocations linked to a receipt reversal.</summary>
    ReversalLinks,
    /// <summary>Approved original settlement quotes whose approvals have not been withdrawn.</summary>
    InstructionSettlementQuoteChoices,
    /// <summary>Installments with outstanding balances in the current approved schedule.</summary>
    InstructionOutstandingInstallmentChoices
}
