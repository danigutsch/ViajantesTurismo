using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Provides predefined payment-related error results.
/// </summary>
public static class PaymentErrors
{
    /// <summary>
    /// Indicates that the payment amount is invalid (zero or negative).
    /// </summary>
    /// <param name="amount">The invalid payment amount.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidAmount(decimal amount) =>
        Result.Invalid(
            detail: $"Payment amount must be greater than zero. Received: {amount}.",
            field: "amount",
            message: "Payment amount must be greater than zero.");

    /// <summary>
    /// Indicates that an invalid payment method value was provided.
    /// </summary>
    /// <param name="method">The invalid payment method value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidPaymentMethod(PaymentMethod method) =>
        Result.Invalid(
            detail: $"Invalid payment method: {method}. Valid values are: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.",
            field: "method",
            message: $"Invalid payment method: {method}.");

    /// <summary>
    /// Indicates that the payment date cannot be in the future.
    /// </summary>
    /// <param name="date">The invalid payment date.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result FuturePaymentDate(DateOnly date) =>
        Result.Invalid(
            detail: $"Payment date cannot be in the future. Received: {date}.",
            field: "paymentDate",
            message: "Payment date cannot be in the future.");

    /// <summary>
    /// Indicates that the payment amount would exceed the remaining balance.
    /// </summary>
    /// <param name="paymentAmount">The attempted payment amount.</param>
    /// <param name="remainingBalance">The remaining balance on the booking.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result ExceedsRemainingBalance(decimal paymentAmount, decimal remainingBalance) =>
        Result.Invalid(
            detail: $"Payment amount {paymentAmount:C} exceeds remaining balance {remainingBalance:C}.",
            field: "amount",
            message: $"Payment amount cannot exceed remaining balance of {remainingBalance:C}.");

    /// <summary>
    /// Indicates that a payment with the specified ID was not found.
    /// </summary>
    /// <param name="id">The ID of the payment that was not found.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result PaymentNotFound(long id) => Result.NotFound(detail: $"Payment with ID {id} was not found.");
}