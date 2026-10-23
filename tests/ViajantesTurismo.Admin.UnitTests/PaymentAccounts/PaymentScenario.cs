using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

internal sealed class PaymentScenario
{
    public static DateTimeOffset Now => new(2030, 1, 5, 12, 0, 0, TimeSpan.Zero);
    public static DateOnly DueDate => new(2030, 1, 10);
    public Tour Tour
    {
        get;
    }
    public Booking Booking
    {
        get;
    }
    public BookingFinancialHistory History => Booking.FinancialHistory ?? throw new InvalidOperationException("Financial history has not been created.");

    public PaymentScenario()
    {
        Tour = EntityBuilders.BuildTour(new TourOptions(
            Schedule: new TourScheduleOptions(new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 2, 7, 0, 0, 0, DateTimeKind.Utc)),
            Pricing: new TourPricingOptions(BasePrice: 99m, RegularBikePrice: 1m, Currency: Currency.Euro)));
        Booking = Tour.AddBooking(TourBookingRequest.CreateSingle(Guid.CreateVersion7(), BikeType.Regular, RoomType.DoubleOccupancy), Now.UtcDateTime).Value;
    }
    public PaymentInstallment ApproveSchedule(decimal amount = 100m)
    {
        var installment = PaymentInstallment.Create(amount, DueDate).Value;
        var draft = Tour.DraftBookingPaymentSchedule(Booking.Id, Booking.FinancialHistory?.Version ?? 0, amount, [installment], "owner", Now).Value;
        Tour.ApproveBookingPaymentSchedule(Booking.Id, History.Version, draft, FinancialApproval.Create("owner", Now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        return installment;
    }
    public Guid RecordReceipt(decimal gross, decimal fees, PaymentInstallment installment)
    {
        return Tour.RecordBookingReceipt(Booking.Id, History.Version, ReceiptSettlement.Create(Currency.Euro, gross, fees).Value,
            new DateOnly(2030, 1, 5), Now, PaymentMethod.BankTransfer, "receipt", [InstallmentAllocation.Create(installment.Id, Math.Min(gross, installment.Amount.Amount)).Value], "owner", Now, new DateOnly(2030, 1, 5)).Value;
    }
}
