using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes DraftBookingPaymentQuote through the owning Tour and an atomic financial transaction.</summary>
public sealed class DraftBookingPaymentQuoteCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(DraftBookingPaymentQuoteCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, null, command.ActorId, input, PaymentFinancialContractJsonContext.Default.CreatePaymentQuoteDto);
        return executor.Execute(command.BookingId, "DraftBookingPaymentQuote", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            var quote = AgreedFxQuote.Create(input.BrlPerEuro, input.Source, input.Reference, input.AgreedAt, input.ExpiresAt, now);
            if (quote.IsFailure)
            {
                return Task.FromResult(quote.ConvertError<AgreedFxQuote, Guid>());
            }
            return Task.FromResult(tour.DraftBookingPaymentQuote(command.BookingId, input.ExpectedVersion, quote.Value, input.ForRefund, command.ActorId, now));
        }, ct);
    }
}
