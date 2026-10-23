using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class FinancialHistoryDeletionTests
{
    [Fact]
    public void Pending_booking_with_receipts_cannot_be_removed()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        scenario.RecordReceipt(1m, 0m, installment);

        // Act
        var result = scenario.Tour.RemoveBooking(scenario.Booking.Id);

        // Assert
        result.IsFailure.ShouldBeTrue();
        scenario.Booking.FinancialHistory.ShouldNotBeNull();
        scenario.Booking.HasFinancialHistory.ShouldBeTrue();
        scenario.Tour.Bookings.Count.ShouldBe(1);
    }

    [Fact]
    public void Parent_tour_with_a_cancelled_booking_receipt_cannot_be_deleted()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        scenario.RecordReceipt(1m, 0m, installment);
        scenario.Tour.CancelBooking(scenario.Booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var result = scenario.Tour.Delete();

        // Assert
        result.IsFailure.ShouldBeTrue();
        scenario.Booking.FinancialHistory.ShouldNotBeNull();
        scenario.Booking.HasFinancialHistory.ShouldBeTrue();
        scenario.History.ExportState().Receipts.Length.ShouldBe(1);
    }
}
