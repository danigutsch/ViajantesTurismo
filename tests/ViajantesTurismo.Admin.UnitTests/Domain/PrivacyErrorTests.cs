using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class PrivacyErrorTests
{
    [Fact]
    public void Customer_and_booking_linked_errors_do_not_expose_identifiers()
    {
        // Arrange
        var identifier = Guid.CreateVersion7();
        var identifierText = identifier.ToString();

        // Act
        var errors = new[]
        {
            CustomerErrors.CustomerNotFound(identifier),
            BookingErrors.BookingNotFound(identifier),
            BookingErrors.CannotModifyCancelledOrCompletedBooking(identifier, BookingStatus.Cancelled),
            BookingErrors.PrincipalAndCompanionCannotBeSame(identifier),
            PaymentErrors.PaymentNotFound(identifier),
            TourErrors.BookingNotFound(identifier),
            TourErrors.TourNotFound(identifier),
            TourErrors.CannotRemoveNonPendingBooking(identifier, BookingStatus.Confirmed),
            DocumentErrors.DocumentNotFound(identifier)
        };

        // Assert
        errors.ShouldAllSatisfy(error =>
        {
            var detail = error.ErrorDetails?.Detail
                ?? throw new InvalidOperationException("Expected an error detail.");
            detail.ShouldNotContain(identifierText, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Financial_and_age_errors_expose_only_bounded_rule_messages()
    {
        // Arrange
        var cases = new (Result Error, string Detail, string Message)[]
        {
            (PaymentErrors.InvalidAmount(-123456m), "Payment amount must be greater than zero.", "Payment amount must be greater than zero."),
            (PaymentErrors.FuturePaymentDate(new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc)), "Payment date cannot be in the future.", "Payment date cannot be in the future."),
            (PaymentErrors.ExceedsRemainingBalance(123456m, 654321m), "Payment amount exceeds remaining balance.", "Payment amount cannot exceed remaining balance."),
            (BookingErrors.ZeroOrNegativeBasePrice(-123456m), "Base price must be greater than zero.", "Base price must be greater than zero."),
            (BookingErrors.BasePriceExceedsMaximum(654321m, 123456m), "Base price exceeds the maximum allowed value.", "Base price exceeds the maximum allowed value."),
            (BookingErrors.NegativeBikePrice(-123456m), "Bike price cannot be negative.", "Bike price cannot be negative."),
            (BookingErrors.BikePriceExceedsMaximum(654321m, 123456m), "Bike price exceeds the maximum allowed value.", "Bike price exceeds the maximum allowed value."),
            (BookingErrors.NegativeRoomCost(-123456m), "Room additional cost cannot be negative.", "Room additional cost cannot be negative."),
            (BookingErrors.RoomCostExceedsMaximum(654321m, 123456m), "Room additional cost exceeds the maximum allowed value.", "Room additional cost exceeds the maximum allowed value."),
            (DiscountErrors.NegativeDiscountAmount(-123456m), "Discount amount cannot be negative.", "Discount amount cannot be negative."),
            (DiscountErrors.PercentageExceedsMaximum(654321m, 100m), "Percentage discount cannot exceed the maximum allowed value.", "Percentage discount cannot exceed the maximum allowed value."),
            (DiscountErrors.AbsoluteDiscountExceedsSubtotal(654321m, 123456m), "Absolute discount amount cannot exceed subtotal.", "Discount amount cannot exceed subtotal."),
            (DiscountErrors.FinalPriceNotPositive(-123456m), "Final price after discount must be greater than zero.", "Final price after discount must be greater than zero."),
            (TourErrors.InvalidPrice("private-price", -123456m), "Tour price must be greater than or equal to zero.", "Tour price must be greater than or equal to zero."),
            (TourErrors.PriceTooHigh("private-price", 123456m, 654321m), "Tour price exceeds the maximum allowed value.", "Tour price exceeds the maximum allowed value."),
            (CustomerErrors.AgeTooYoung(7), "Customer must meet the minimum age requirement.", "Customer must meet the minimum age requirement.")
        };

        // Act
        var results = cases.Select(testCase =>
        {
            var details = testCase.Error.ErrorDetails
                ?? throw new InvalidOperationException("Expected an error detail.");
            var validationErrors = details.ValidationErrors
                ?? throw new InvalidOperationException("Expected validation errors.");
            var message = validationErrors.Values.SelectMany(static values => values).ShouldHaveSingleItem();
            return (
                ActualDetail: details.Detail,
                ActualMessage: message,
                ExpectedDetail: testCase.Detail,
                ExpectedMessage: testCase.Message);
        }).ToArray();

        // Assert
        results.ShouldAllSatisfy(result =>
        {
            result.ActualDetail.ShouldBe(result.ExpectedDetail);
            result.ActualMessage.ShouldBe(result.ExpectedMessage);
        });
    }
}
