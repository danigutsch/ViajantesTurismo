using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Projects masked staff accounts and explicitly audited sensitive Admin details.</summary>
public interface IPaymentAccountQueryService
{
    /// <summary>Lists masked accounts.</summary>
    Task<IReadOnlyList<GetPaymentAccountDto>> List(CancellationToken ct);
    /// <summary>Gets a masked account by stable identifier.</summary>
    Task<GetPaymentAccountDto?> GetById(Guid id, CancellationToken ct);
    /// <summary>Gets sensitive details after the API has enforced Admin authorization, recording access before returning.</summary>
    Task<GetPaymentAccountDetailsDto?> GetSensitiveDetails(Guid id, string actorId, CancellationToken ct);
}
