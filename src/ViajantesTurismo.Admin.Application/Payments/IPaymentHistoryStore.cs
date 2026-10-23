using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Stages a booking's protected financial history and metadata audits in the Tour's unit of work.</summary>
public interface IPaymentHistoryStore
{
    /// <summary>Stages the changed booking history; commits only with its financial command.</summary>
    Task Save(Tour tour, Guid bookingId, CancellationToken ct);
}
