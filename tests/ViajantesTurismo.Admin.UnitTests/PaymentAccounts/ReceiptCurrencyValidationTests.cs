using PandaBear.Idempotency;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class ReceiptCurrencyValidationTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("JPY")]
    [InlineData("E1R")]
    [InlineData("Euro")]
    [InlineData("")]
    public async Task Receipts_settle_only_in_eur_or_brl_and_report_a_settlement_error_otherwise(string code)
    {
        // Arrange
        var calendar = new PaymentBusinessCalendar(TimeProvider.System, TimeZoneInfo.Utc);
        var handler = new RecordBookingReceiptCommandHandler(null!, TimeProvider.System, calendar);
        var input = new CreateBookingReceiptDto
        {
            ExpectedVersion = 0,
            SettlementCurrency = code,
            Gross = 100m,
            Fees = 0m,
            PaymentDate = new DateOnly(2030, 1, 5),
            Method = PaymentMethodDto.BankTransfer,
            Reference = "receipt",
            ProposedAllocations = []
        };

        // Act
        var result = await handler.Handle(new RecordBookingReceiptCommand(Guid.CreateVersion7(), input, "owner", IdempotencyKey.From("receipt-key-0001")), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["settlement"]);
    }
}
