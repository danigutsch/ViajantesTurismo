using ViajantesTurismo.Management.Web.Components.Pages.Payments;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class PaymentFormModelRedactionTests
{
    public static TheoryData<object, string> FormModels => new()
    {
        { new PaymentAllocationFormRow { TargetId = Guid.NewGuid(), EuroAmount = 123.45m }, "PaymentAllocationFormRow [redacted]" },
        { new PaymentInstallmentFormRow { EuroAmount = 123.45m, DueDate = new DateOnly(2030, 1, 2) }, "PaymentInstallmentFormRow [redacted]" },
        { new PaymentScheduleFormModel { ObligationEuro = 123.45m, Installments = [new PaymentInstallmentFormRow { EuroAmount = 123.45m }] }, "PaymentScheduleFormModel [redacted]" },
        { new PaymentQuoteFormModel { BrlPerEuro = 6.1m, Source = "secret-source", Reference = "secret-quote-ref" }, "PaymentQuoteFormModel [redacted]" },
        { new PaymentReceiptFormModel { Gross = 123.45m, Fees = 1m, Reference = "secret-receipt-ref" }, "PaymentReceiptFormModel [redacted]" },
        { new PaymentReconciliationFormModel { ReceiptId = Guid.NewGuid(), EuroCredit = 123.45m, QuoteId = Guid.NewGuid(), InstructionId = Guid.NewGuid() }, "PaymentReconciliationFormModel [redacted]" },
        { new PaymentRefundFormModel { ActualSettlement = 123.45m, Reference = "secret-refund-ref" }, "PaymentRefundFormModel [redacted]" },
        { new PaymentAccountFormModel { Label = "secret-label", RecipientName = "Secret Person", Iban = "DE89370400440532013000", PixKey = "secret@example.test", TaxId = "12345678900" }, "PaymentAccountFormModel [redacted]" },
    };

    [Theory]
    [MemberData(nameof(FormModels))]
    public void Form_models_do_not_expose_their_financial_details_when_formatted_as_text(object model, string expectedText)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(model);

        // Act
        var text = model.ToString();

        // Assert
        text.ShouldBe(expectedText);
    }
}
