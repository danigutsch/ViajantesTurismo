using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Provides typed access to the Admin messaging-health endpoint.</summary>
public interface IMessagingHealthApiClient
{
    /// <summary>Gets an aggregate health snapshot for Admin-owned messaging.</summary>
    Task<HttpOutcome<GetMessagingHealthDto>> ReadMessagingHealth(CancellationToken ct);

    /// <summary>Gets a bounded, metadata-only page of unfinished Catalog deliveries.</summary>
    Task<HttpOutcome<GetCatalogDeliveryPageDto>> ReadCatalogDelivery(CatalogDeliveryFilters filters, CancellationToken ct);

    /// <summary>Gets a bounded, metadata-only page of pending Admin outbox publication.</summary>
    Task<HttpOutcome<GetOutboxDeliveryPageDto>> ReadOutboxDelivery(OutboxDeliveryFilters filters, CancellationToken ct);
}
