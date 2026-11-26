using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Creates synthetic payments for testing display and raw logging-state boundaries.</summary>
internal static class PaymentCommandDiagnosticExamples
{
    /// <summary>The synthetic payment actor.</summary>
    internal const string Actor = "synthetic-payment-actor";

    /// <summary>The synthetic recipient name.</summary>
    internal const string RecipientName = "Synthetic Payee";

    /// <summary>The synthetic recipient key.</summary>
    internal const string RecipientKey = "synthetic-payee@example.test";

    /// <summary>The document version used by the review fixture.</summary>
    internal const long DocumentVersion = 31;

    /// <summary>The fixed synthetic document identifier.</summary>
    internal static readonly Guid DocumentId = Guid.Parse("10000000-0000-4000-8000-000000000001");

    /// <summary>Provides a synthetic initialized key or the real default value.</summary>
    internal static IdempotencyKey Key(bool initialized) => initialized
        ? IdempotencyKey.From("synthetic-payment-idempotency-key")
        : default;

    /// <summary>Creates the requested real payment command using synthetic data.</summary>
    internal static IPaymentCommand Create(string commandName, bool initialized)
    {
        var key = Key(initialized);
        var target = new PaymentInstructionTarget(DocumentId, Guid.Empty, Guid.Empty, 1);
        return commandName switch
        {
            nameof(ReviewPaymentInstructionsCommand) => new ReviewPaymentInstructionsCommand(target,
                new ReviewPaymentInstructionsDto { ExpectedDocumentVersion = DocumentVersion, ChangesRequested = true }, Actor, key),
            nameof(CreatePaymentAccountCommand) => new CreatePaymentAccountCommand(new CreatePaymentAccountDto
            {
                Label = "synthetic-label",
                Recipient = new PaymentRecipientDto
                {
                    Kind = PaymentAccountKindDto.Pix,
                    SettlementCurrency = "BRL",
                    RecipientName = RecipientName,
                    PixKeyType = PixKeyTypeDto.Email,
                    PixKey = RecipientKey
                }
            }, Actor, key),
            nameof(DownloadPaymentInstructionsCommand) => new DownloadPaymentInstructionsCommand(target, Actor),
            _ => throw new ArgumentOutOfRangeException(nameof(commandName))
        };
    }
}
