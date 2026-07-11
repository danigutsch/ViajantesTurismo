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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Payment amount must be greater than zero", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("0", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("amount")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["amount"][0]).ShouldBe("Payment amount must be greater than zero.");
    }

    [Fact]
    public void Invalid_payment_method_should_return_invalid_result()
    {
        // Arrange
        const PaymentMethod invalidMethod = (PaymentMethod)999;

        // Act
        var result = PaymentErrors.InvalidPaymentMethod(invalidMethod);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid payment method", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("999", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("Valid values are:", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("method")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["method"][0]).ShouldContain("Invalid payment method", StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_payment_method_should_include_all_valid_values()
    {
        // Arrange
        const PaymentMethod invalidMethod = (PaymentMethod)999;

        // Act
        var result = PaymentErrors.InvalidPaymentMethod(invalidMethod);

        // Assert
        (result.ErrorDetails).ShouldNotBeNull();
        var allValidMethods = Enum.GetNames<PaymentMethod>();
        foreach (var validMethod in allValidMethods)
        {
            (result.ErrorDetails.Detail).ShouldContain(validMethod, StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Payment date cannot be in the future", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("2026", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("paymentDate")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["paymentDate"][0]).ShouldBe("Payment date cannot be in the future.");
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("exceeds remaining balance", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("500", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("300", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("amount")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["amount"][0]).ShouldContain("cannot exceed remaining balance", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors["amount"][0]).ShouldContain("300", StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_not_found_should_return_not_found_result()
    {
        // Arrange
        var paymentId = Guid.CreateVersion7();

        // Act
        var result = PaymentErrors.PaymentNotFound(paymentId);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Payment with ID", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain(paymentId.ToString(), StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("was not found", StringComparison.Ordinal);
    }
}
