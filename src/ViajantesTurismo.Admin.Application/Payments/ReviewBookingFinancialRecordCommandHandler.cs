using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes ReviewBookingFinancialRecord through the owning Tour and an atomic financial transaction.</summary>
public sealed class ReviewBookingFinancialRecordCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(ReviewBookingFinancialRecordCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.TargetId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.ReviewFinancialRecordDto);
        return executor.Execute(command.BookingId, "ReviewBookingFinancialRecord", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            return Task.FromResult(tour.ReviewBookingFinancialRecord(command.BookingId, input.ExpectedVersion, command.TargetId, input.ChangesRequested, command.ActorId, now));
        }, ct);
    }
}
