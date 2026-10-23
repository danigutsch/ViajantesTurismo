using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Defines input-independent recipient account failures.</summary>
public static class PaymentAccountErrors
{
    /// <summary>Rejects invalid account input without including submitted values.</summary>
    /// <param name="field">The affected non-sensitive field name.</param>
    /// <returns>A validation failure.</returns>
    public static Result Invalid(string field) =>
        Result.Invalid(detail: "Invalid payment account information.", field: field, message: "Review the payment account information.");

    /// <summary>Rejects a stale edit or lifecycle action.</summary>
    /// <returns>A conflict requiring a refreshed confirmation summary.</returns>
    public static Result Changed() => Result.Conflict(detail: "The payment account changed. Reload and confirm the current revision.");

    /// <summary>Rejects an action that is unavailable in the current lifecycle state.</summary>
    /// <returns>A lifecycle conflict.</returns>
    public static Result Unavailable() => Result.Conflict(detail: "This action is unavailable for the current payment account state.");

    /// <summary>Rejects a creator's attempt to provide an independent review.</summary>
    /// <returns>A conflict; self-verification remains available.</returns>
    public static Result IndependentReviewerRequired() =>
        Result.Conflict(detail: "The revision creator cannot provide an independent review. Explicit self-verification remains available.");
}
