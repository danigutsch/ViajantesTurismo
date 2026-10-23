namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Defines payment account review outcome labels.</summary>
public enum PaymentAccountReviewOutcome
{
    /// <summary>No independent review.</summary>
    None = 0,
    /// <summary>The reviewed revision was accepted.</summary>
    Approved = 1,
    /// <summary>The reviewed revision needs attention; approval is unchanged.</summary>
    ChangesRequested = 2,
}
