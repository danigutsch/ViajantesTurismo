using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
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
            CustomerErrors.CustomerNotFound(),
            BookingErrors.BookingNotFound(),
            BookingErrors.CannotModifyCancelledOrCompletedBooking( BookingStatus.Cancelled),
            BookingErrors.PrincipalAndCompanionCannotBeSame(),
            TourErrors.BookingNotFound(),
            TourErrors.TourNotFound(),
            TourErrors.CannotRemoveNonPendingBooking( BookingStatus.Confirmed),
            DocumentErrors.DocumentNotFound()
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
            (Admin.Domain.Payments.FinancialErrors.Invalid("receipt"), "Invalid payment information.", "Review the payment information."),
            (BookingErrors.ZeroOrNegativeBasePrice(), "Base price must be greater than zero.", "Base price must be greater than zero."),
            (BookingErrors.BasePriceExceedsMaximum(), "Base price exceeds the maximum allowed value.", "Base price exceeds the maximum allowed value."),
            (BookingErrors.NegativeBikePrice(), "Bike price cannot be negative.", "Bike price cannot be negative."),
            (BookingErrors.BikePriceExceedsMaximum(), "Bike price exceeds the maximum allowed value.", "Bike price exceeds the maximum allowed value."),
            (BookingErrors.NegativeRoomCost(), "Room additional cost cannot be negative.", "Room additional cost cannot be negative."),
            (BookingErrors.RoomCostExceedsMaximum(), "Room additional cost exceeds the maximum allowed value.", "Room additional cost exceeds the maximum allowed value."),
            (DiscountErrors.NegativeDiscountAmount(), "Discount amount cannot be negative.", "Discount amount cannot be negative."),
            (DiscountErrors.PercentageExceedsMaximum(), "Percentage discount cannot exceed the maximum allowed value.", "Percentage discount cannot exceed the maximum allowed value."),
            (DiscountErrors.AbsoluteDiscountExceedsSubtotal(), "Absolute discount amount cannot exceed subtotal.", "Discount amount cannot exceed subtotal."),
            (DiscountErrors.FinalPriceNotPositive(), "Final price after discount must be greater than zero.", "Final price after discount must be greater than zero."),
            (TourErrors.InvalidPrice("private-price"), "Tour price must be greater than or equal to zero.", "Tour price must be greater than or equal to zero."),
            (TourErrors.PriceTooHigh("private-price"), "Tour price exceeds the maximum allowed value.", "Tour price exceeds the maximum allowed value."),
            (CustomerErrors.AgeTooYoung(), "Customer must meet the minimum age requirement.", "Customer must meet the minimum age requirement.")
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
