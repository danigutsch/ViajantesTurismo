namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Identifies the required, bounded business reason for a financial action.</summary>
public enum FinancialReasonDto
{
    /// <summary>No reason selected.</summary>
    None = 0,
    /// <summary>The initial approved obligation.</summary>
    InitialObligation = 1,
    /// <summary>A booking price or installment amendment.</summary>
    TermsChanged = 2,
    /// <summary>An explicit cancellation obligation adjustment; cancellation itself changes no debt.</summary>
    CancellationAdjustment = 3,
    /// <summary>A manually agreed quote.</summary>
    QuoteAgreed = 4,
    /// <summary>An exact receipt evidenced against frozen instruction terms.</summary>
    ExactReceipt = 5,
    /// <summary>A late receipt.</summary>
    LateReceipt = 6,
    /// <summary>A short receipt.</summary>
    ShortReceipt = 7,
    /// <summary>An excess receipt, retaining explicit customer credit.</summary>
    ExcessReceipt = 8,
    /// <summary>Fees explain the difference.</summary>
    FeeDifference = 9,
    /// <summary>Currency, reference or other evidenced terms differ.</summary>
    MismatchedReceipt = 10,
    /// <summary>Explicit staff-selected allocation or reassignment.</summary>
    AllocationSelected = 11,
    /// <summary>An erroneous receipt is reversed in full.</summary>
    ReceiptError = 12,
    /// <summary>A performed refund reverses selected EUR credit.</summary>
    RefundPerformed = 13,
    /// <summary>A refund record is corrected; this is not a return transfer.</summary>
    RefundRecordError = 14,
    /// <summary>An approval is explicitly withdrawn.</summary>
    ApprovalWithdrawn = 15,
    /// <summary>A draft is saved without approval.</summary>
    DraftSaved = 16,
    /// <summary>An actual receipt is recorded pending reconciliation.</summary>
    ReceiptRecorded = 17,
    /// <summary>An independent reviewer accepts the immutable revision.</summary>
    ReviewAccepted = 18,
    /// <summary>An independent reviewer requests attention without revoking approval.</summary>
    ReviewChangesRequested = 19,
    /// <summary>Generated frozen instruction terms.</summary>
    InstructionGenerated = 20,
    /// <summary>Explicitly approved the frozen instruction summary.</summary>
    InstructionApproved = 21,
    /// <summary>Finalized a freshly revalidated instruction.</summary>
    InstructionFinalized = 22,
    /// <summary>Issued an authenticated, freshly revalidated download.</summary>
    InstructionDownloaded = 23,
    /// <summary>Explicitly voided an instruction revision.</summary>
    InstructionVoided = 24
}
