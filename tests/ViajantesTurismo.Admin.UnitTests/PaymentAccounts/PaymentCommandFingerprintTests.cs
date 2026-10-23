using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Application.Idempotency;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class PaymentCommandFingerprintTests
{
    [Fact]
    public void Financial_normalization_preserves_existing_request_fingerprints()
    {
        // Arrange
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var input = new CreatePaymentScheduleDto { ExpectedVersion = 0, ObligationEuro = 100.00m, Installments = [] };
        var expected = ApplicationRequestFingerprint.Create(bookingId.ToString("N"), string.Empty, "owner",
            """{"expectedVersion":0,"installments":[],"obligationEuro":100}""");

        // Act
        var actual = PaymentCommandFingerprint.Create(bookingId, null, "owner", input, PaymentFinancialContractJsonContext.Default.CreatePaymentScheduleDto);
        var equivalent = PaymentCommandFingerprint.Create(bookingId, null, "owner", input with
        {
            ObligationEuro = 100m
        }, PaymentFinancialContractJsonContext.Default.CreatePaymentScheduleDto);

        // Assert
        actual.ShouldBe(expected);
        equivalent.ShouldBe(expected);
    }

    [Fact]
    public void Financial_request_identity_remains_bound_to_booking_target_and_authenticated_actor()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var targetId = Guid.CreateVersion7();
        var input = new ApproveFinancialRecordDto { ExpectedVersion = 2, Confirmed = true, Reason = FinancialReasonDto.InitialObligation };

        // Act
        var original = PaymentCommandFingerprint.Create(bookingId, targetId, "owner", input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);
        var otherBooking = PaymentCommandFingerprint.Create(Guid.CreateVersion7(), targetId, "owner", input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);
        var otherTarget = PaymentCommandFingerprint.Create(bookingId, Guid.CreateVersion7(), "owner", input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);
        var otherActor = PaymentCommandFingerprint.Create(bookingId, targetId, "other-admin", input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);

        // Assert
        original.ShouldNotBe(otherBooking);
        original.ShouldNotBe(otherTarget);
        original.ShouldNotBe(otherActor);
    }
}
