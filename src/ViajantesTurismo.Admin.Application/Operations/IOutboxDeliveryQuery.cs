using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Operations;

/// <summary>Reads bounded publication metadata from the Admin outbox.</summary>
public interface IOutboxDeliveryQuery
{
    /// <summary>Gets the requested page without claiming or publishing any outbox message.</summary>
    Task<GetOutboxDeliveryPageDto> ReadOutboxDelivery(OutboxDeliveryRequest request, CancellationToken ct);
}
