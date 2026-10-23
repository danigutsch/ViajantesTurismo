using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Defines input-independent financial failures without payloads or recipient information.</summary>
public static class FinancialErrors
{
    /// <summary>Rejects invalid financial input without echoing values.</summary>
    public static Result Invalid(string field) => Result.Invalid(detail: "Invalid payment information.", field: field, message: "Review the payment information.");
    /// <summary>Rejects stale financial data or a lifecycle conflict.</summary>
    public static Result Changed() => Result.Conflict(detail: "Financial terms or balances changed. Reload and confirm the current revision.");
    /// <summary>Rejects an unavailable action without exposing financial payloads.</summary>
    public static Result Unavailable() => Result.Conflict(detail: "The financial action is unavailable in the current state.");
}
