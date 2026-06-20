using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.RecordPayment;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.ApiService;

internal static class BookingsRecordPaymentEndpoint
{
    public static void MapRecordPaymentEndpoint(this RouteGroupBuilder bookingsGroup)
    {
        ArgumentNullException.ThrowIfNull(bookingsGroup);

        bookingsGroup.MapPost("/{id:guid}/payments", RecordPayment)
            .WithAdminMetadata("RecordPayment", "Records a payment for a booking.", "Records a payment.");
    }

    private static async Task<Results<Created<GetPaymentDto>, NotFound<ProblemDetails>, ValidationProblem>> RecordPayment(
        [FromRoute] Guid id,
        [FromBody] CreatePaymentDto dto,
        [FromServices] RecordPaymentCommandHandler handler,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var command = new RecordPaymentCommand(
            id,
            dto.Amount,
            dto.PaymentDate,
            dto.Method,
            dto.ReferenceNumber,
            dto.Notes);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            var failure = result.ConvertError();
            return AdminEndpointResults.MatchNotFoundValidationFailure<Results<Created<GetPaymentDto>, NotFound<ProblemDetails>, ValidationProblem>>(
                failure,
                () => failure.ToNotFound(),
                () => failure.ToValidationProblem());
        }

        var paymentId = result.Value;

        var updatedBooking = await queryService.GetBookingById(id, ct);

        if (updatedBooking is null)
        {
            return BookingErrors.BookingNotFound(id).ToNotFound();
        }

        var paymentDto = updatedBooking.Payments.FirstOrDefault(p => p.Id == paymentId);

        if (paymentDto is null)
        {
            return PaymentErrors.PaymentNotFound(paymentId).ToNotFound();
        }

        return TypedResults.Created($"/bookings/{id}/payments/{paymentId}", paymentDto);
    }
}
