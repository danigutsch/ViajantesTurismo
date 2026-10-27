using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Builds a tour with one single-rider booking for payment-account tests.</summary>
internal sealed class PaymentScenario
{
    /// <summary>Fixed UTC instant used as the current time in scenarios.</summary>
    public static DateTimeOffset Now => new(2030, 1, 5, 12, 0, 0, TimeSpan.Zero);
    /// <summary>Fixed installment due date used in scenarios.</summary>
    public static DateOnly DueDate => new(2030, 1, 10);
    /// <summary>The tour holding the booking.</summary>
    public Tour Tour
    {
        get;
    }
    /// <summary>The booking whose financial history is exercised.</summary>
    public Booking Booking
    {
        get;
    }
    /// <summary>The booking financial history; throws when it has not been created.</summary>
    public BookingFinancialHistory History => Booking.FinancialHistory ?? throw new InvalidOperationException("Financial history has not been created.");

    /// <summary>Creates the tour and adds one regular-bike, double-occupancy booking.</summary>
    public PaymentScenario()
    {
        Tour = EntityBuilders.BuildTour(new TourOptions(
            Schedule: new TourScheduleOptions(new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 2, 7, 0, 0, 0, DateTimeKind.Utc)),
            Pricing: new TourPricingOptions(BasePrice: 99m, RegularBikePrice: 1m, Currency: Currency.Euro)));
        Booking = Tour.AddBooking(TourBookingRequest.CreateSingle(Guid.CreateVersion7(), BikeType.Regular, RoomType.DoubleOccupancy), Now.UtcDateTime).Value;
    }
    /// <summary>Drafts and approves a one-installment payment schedule and returns that installment.</summary>
    public PaymentInstallment ApproveSchedule(decimal amount = 100m)
    {
        var installment = PaymentInstallment.Create(amount, DueDate).Value;
        var draft = Tour.DraftBookingPaymentSchedule(Booking.Id, Booking.FinancialHistory?.Version ?? 0, amount, [installment], "owner", Now).Value;
        Tour.ApproveBookingPaymentSchedule(Booking.Id, History.Version, draft, FinancialApproval.Create("owner", Now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        return installment;
    }
    /// <summary>Records a receipt allocated to the installment, capped at its amount, and returns the receipt id.</summary>
    public Guid RecordReceipt(decimal gross, decimal fees, PaymentInstallment installment)
    {
        return Tour.RecordBookingReceipt(Booking.Id, History.Version, ReceiptSettlement.Create(Currency.Euro, gross, fees).Value,
            new DateOnly(2030, 1, 5), Now, PaymentMethod.BankTransfer, "receipt", [InstallmentAllocation.Create(installment.Id, Math.Min(gross, installment.Amount.Amount)).Value], "owner", Now, new DateOnly(2030, 1, 5)).Value;
    }
}
