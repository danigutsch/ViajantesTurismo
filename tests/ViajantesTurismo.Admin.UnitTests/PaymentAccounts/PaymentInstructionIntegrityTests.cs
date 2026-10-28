using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentInstructionIntegrityTests
{
    [Fact]
    public void Restore_rejects_manipulated_installment_rounding_even_when_totals_balance()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var now = PaymentScenario.Now;
        var one = PaymentInstallment.Create(50m, PaymentScenario.DueDate).Value;
        var two = PaymentInstallment.Create(50m, PaymentScenario.DueDate.AddDays(1)).Value;
        var schedule = scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, 0, 100m, [one, two], "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentSchedule(scenario.Booking.Id, scenario.History.Version, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var original = scenario.Tour.CreateBookingPaymentInstruction(scenario.Booking.Id, account, [one.Id, two.Id], null, "reference", now).Value.ExportState();
        var corrupt = original with
        {
            Installments = [original.Installments[0] with { SettlementAmount = 49m, RoundingAdjustment = -1m }, original.Installments[1] with { SettlementAmount = 51m, RoundingAdjustment = 1m }]
        };

        // Act
        var result = FrozenPaymentInstruction.Restore(corrupt);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Clock_before_verification_cannot_generate_or_deliver_an_instruction()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var now = PaymentScenario.Now;
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var instruction = scenario.Tour.CreateBookingPaymentInstruction(scenario.Booking.Id, account, [installment.Id], null, "reference", now).Value;

        // Act
        var before = scenario.Tour.CreateBookingPaymentInstruction(scenario.Booking.Id, account, [installment.Id], null, "reference", now.AddTicks(-1));
        var delivered = scenario.Tour.CanDeliverBookingPaymentInstruction(instruction, account, now.AddTicks(-1));

        // Assert
        before.IsFailure.ShouldBeTrue();
        delivered.ShouldBeFalse();
    }

    [Theory]
    [InlineData(100, 0, "reference", true, true)]
    [InlineData(99, 0, "reference", true, false)]
    [InlineData(101, 0, "reference", true, false)]
    [InlineData(100, 1, "reference", true, false)]
    [InlineData(100, 0, "different", true, false)]
    [InlineData(100, 0, "reference", false, false)]
    public void Exact_receipt_requires_frozen_amount_fees_reference_and_evidenced_instant(decimal gross, decimal fees, string reference, bool evidence, bool expected)
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var now = PaymentScenario.Now;
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var instruction = scenario.Tour.CreateBookingPaymentInstruction(scenario.Booking.Id, account, [installment.Id], null, "reference", now).Value;

        // Act
        var exact = instruction.MatchesReceipt(scenario.Booking.Id, Admin.Domain.Tours.Currency.Euro, gross, fees, new DateOnly(2030, 1, 5), evidence ? now : null, reference, 100m, null);
        var late = instruction.MatchesReceipt(scenario.Booking.Id, Admin.Domain.Tours.Currency.Euro, gross, fees, PaymentScenario.DueDate.AddDays(1), evidence ? now : null, reference, 100m, null);

        // Assert
        exact.ShouldBe(expected);
        late.ShouldBeFalse();
    }
}
