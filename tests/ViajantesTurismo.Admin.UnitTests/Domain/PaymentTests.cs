using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class PaymentTests
{
    [Fact]
    public void Invalid_amount_should_return_invalid_result()
    {
        // Arrange
        const decimal invalidAmount = 0m;

        // Act
        var result = PaymentErrors.InvalidAmount(invalidAmount);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Payment amount must be greater than zero", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("0", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("amount"));
        TestAssert.Equal("Payment amount must be greater than zero.", result.ErrorDetails.ValidationErrors["amount"][0]);
    }

    [Fact]
    public void Invalid_payment_method_should_return_invalid_result()
    {
        // Arrange
        const PaymentMethod invalidMethod = (PaymentMethod)999;

        // Act
        var result = PaymentErrors.InvalidPaymentMethod(invalidMethod);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Invalid payment method", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("999", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("Valid values are:", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("method"));
        TestAssert.Contains("Invalid payment method", result.ErrorDetails.ValidationErrors["method"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_payment_method_should_include_all_valid_values()
    {
        // Arrange
        const PaymentMethod invalidMethod = (PaymentMethod)999;

        // Act
        var result = PaymentErrors.InvalidPaymentMethod(invalidMethod);

        // Assert
        TestAssert.NotNull(result.ErrorDetails);
        var allValidMethods = Enum.GetNames<PaymentMethod>();
        foreach (var validMethod in allValidMethods)
        {
            TestAssert.Contains(validMethod, result.ErrorDetails.Detail, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Future_payment_date_should_return_invalid_result()
    {
        // Arrange
        var futureDate = new DateTime(2026, 12, 31, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = PaymentErrors.FuturePaymentDate(futureDate);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Payment date cannot be in the future", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("2026", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("paymentDate"));
        TestAssert.Equal("Payment date cannot be in the future.", result.ErrorDetails.ValidationErrors["paymentDate"][0]);
    }

    [Fact]
    public void Exceeds_remaining_balance_should_return_invalid_result()
    {
        // Arrange
        const decimal paymentAmount = 500.00m;
        const decimal remainingBalance = 300.00m;

        // Act
        var result = PaymentErrors.ExceedsRemainingBalance(paymentAmount, remainingBalance);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("exceeds remaining balance", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("500", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("300", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("amount"));
        TestAssert.Contains("cannot exceed remaining balance", result.ErrorDetails.ValidationErrors["amount"][0], StringComparison.Ordinal);
        TestAssert.Contains("300", result.ErrorDetails.ValidationErrors["amount"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_not_found_should_return_not_found_result()
    {
        // Arrange
        var paymentId = Guid.CreateVersion7();

        // Act
        var result = PaymentErrors.PaymentNotFound(paymentId);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Payment with ID", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains(paymentId.ToString(), result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("was not found", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }
}
