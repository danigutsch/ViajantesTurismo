using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class PaymentTests
{
    [Fact]
    public void Invalid_Amount_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal invalidAmount = 0m;

        // Act
        var result = PaymentErrors.InvalidAmount(invalidAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Payment amount must be greater than zero", result.ErrorDetails.Detail);
        Assert.Contains("0", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("amount"));
        Assert.Equal("Payment amount must be greater than zero.", result.ErrorDetails.ValidationErrors["amount"][0]);
    }

    [Fact]
    public void Invalid_Amount_With_Negative_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal invalidAmount = -50.00m;

        // Act
        var result = PaymentErrors.InvalidAmount(invalidAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Payment amount must be greater than zero", result.ErrorDetails.Detail);
        Assert.Contains("-50", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Invalid_Payment_Method_Should_Return_Invalid_Result()
    {
        // Arrange
        const PaymentMethod invalidMethod = (PaymentMethod)999;

        // Act
        var result = PaymentErrors.InvalidPaymentMethod(invalidMethod);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid payment method", result.ErrorDetails.Detail);
        Assert.Contains("999", result.ErrorDetails.Detail);
        Assert.Contains("Valid values are:", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("method"));
        Assert.Contains("Invalid payment method", result.ErrorDetails.ValidationErrors["method"][0]);
    }

    [Fact]
    public void Invalid_Payment_Method_Should_Include_All_Valid_Values()
    {
        // Arrange
        const PaymentMethod invalidMethod = (PaymentMethod)999;

        // Act
        var result = PaymentErrors.InvalidPaymentMethod(invalidMethod);

        // Assert
        Assert.NotNull(result.ErrorDetails);
        var allValidMethods = Enum.GetNames<PaymentMethod>();
        foreach (var validMethod in allValidMethods)
        {
            Assert.Contains(validMethod, result.ErrorDetails.Detail);
        }
    }

    [Fact]
    public void Future_Payment_Date_Should_Return_Invalid_Result()
    {
        // Arrange
        var futureDate = new DateTime(2026, 12, 31, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = PaymentErrors.FuturePaymentDate(futureDate);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Payment date cannot be in the future", result.ErrorDetails.Detail);
        Assert.Contains("2026", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("paymentDate"));
        Assert.Equal("Payment date cannot be in the future.", result.ErrorDetails.ValidationErrors["paymentDate"][0]);
    }

    [Fact]
    public void Exceeds_Remaining_Balance_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal paymentAmount = 500.00m;
        const decimal remainingBalance = 300.00m;

        // Act
        var result = PaymentErrors.ExceedsRemainingBalance(paymentAmount, remainingBalance);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("exceeds remaining balance", result.ErrorDetails.Detail);
        Assert.Contains("500", result.ErrorDetails.Detail);
        Assert.Contains("300", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("amount"));
        Assert.Contains("cannot exceed remaining balance", result.ErrorDetails.ValidationErrors["amount"][0]);
        Assert.Contains("300", result.ErrorDetails.ValidationErrors["amount"][0]);
    }

    [Fact]
    public void Payment_Not_Found_Should_Return_Not_Found_Result()
    {
        // Arrange
        const long paymentId = 12345L;

        // Act
        var result = PaymentErrors.PaymentNotFound(paymentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Payment with ID", result.ErrorDetails.Detail);
        Assert.Contains("12345", result.ErrorDetails.Detail);
        Assert.Contains("was not found", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Payment_Not_Found_With_Different_Id_Should_Include_Correct_Id()
    {
        // Arrange
        const long paymentId = 999L;

        // Act
        var result = PaymentErrors.PaymentNotFound(paymentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("999", result.ErrorDetails.Detail);
    }
}
