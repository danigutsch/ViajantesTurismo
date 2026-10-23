using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes DraftBookingPaymentSchedule through the owning Tour and an atomic financial transaction.</summary>
public sealed class DraftBookingPaymentScheduleCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(DraftBookingPaymentScheduleCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var installments = FinancialMappings.Installments(input.Installments);
        if (installments.IsFailure)
        {
            return Task.FromResult(installments.ConvertError<IReadOnlyList<PaymentInstallment>, Guid>());
        }
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, null, command.ActorId, input, PaymentFinancialContractJsonContext.Default.CreatePaymentScheduleDto);
        return executor.Execute(command.BookingId, "DraftBookingPaymentSchedule", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            return Task.FromResult(tour.DraftBookingPaymentSchedule(command.BookingId, input.ExpectedVersion, input.ObligationEuro, installments.Value, command.ActorId, now));
        }, ct);
    }
}
