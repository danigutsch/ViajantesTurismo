using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes DraftBookingPaymentSchedule through the owning Tour and an atomic financial transaction.</summary>
public sealed class DraftBookingPaymentScheduleCommandHandler(BookingPaymentOperations executor, TimeProvider clock) : ICommandHandler<DraftBookingPaymentScheduleCommand, Result<Guid>>
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public ValueTask<Result<Guid>> Handle(DraftBookingPaymentScheduleCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var installments = FinancialMappings.Installments(input.Installments);
        if (installments.IsFailure)
        {
            return ValueTask.FromResult(installments.ConvertError<IReadOnlyList<PaymentInstallment>, Guid>());
        }
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, null, command.ActorId, input, PaymentFinancialContractJsonContext.Default.CreatePaymentScheduleDto);
        return executor.Execute(command.BookingId, command.OperationScope, command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            return Task.FromResult(tour.DraftBookingPaymentSchedule(command.BookingId, input.ExpectedVersion, input.ObligationEuro, installments.Value, command.ActorId, now));
        }, ct);
    }
}
