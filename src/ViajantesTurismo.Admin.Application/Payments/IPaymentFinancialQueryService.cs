using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Projects financial history without exposing recipient identifiers through ordinary responses.</summary>
public interface IPaymentFinancialQueryService
{
    /// <summary>Gets masked financial history and balances.</summary>
    Task<GetBookingFinancialHistoryDto?> GetByBookingId(Guid bookingId, CancellationToken ct);
    /// <summary>Gets full evidence references after explicit Admin authorization, auditing access before returning.</summary>
    Task<GetBookingFinancialHistoryDto?> GetSensitiveDetails(Guid bookingId, string actorId, CancellationToken ct);
}
