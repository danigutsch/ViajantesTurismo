using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Operations;

/// <summary>Reads aggregate health for Admin-owned messaging.</summary>
public interface IMessagingHealthQuery
{
    /// <summary>Reads a snapshot from the durable transport without loading individual messages.</summary>
    Task<GetMessagingHealthDto> ReadMessagingHealth(CancellationToken ct);
}
