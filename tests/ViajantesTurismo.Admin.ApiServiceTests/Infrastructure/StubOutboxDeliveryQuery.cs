using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Returns a fixture page and captures the last validated request.</summary>
/// <param name="page">The response metadata to return.</param>
internal sealed class StubOutboxDeliveryQuery(GetOutboxDeliveryPageDto page) : IOutboxDeliveryQuery
{
    /// <summary>Gets the captured request, if the endpoint called the query.</summary>
    internal OutboxDeliveryRequest? Request
    {
        get; private set;
    }

    /// <inheritdoc />
    public Task<GetOutboxDeliveryPageDto> ReadOutboxDelivery(OutboxDeliveryRequest request, CancellationToken ct)
    {
        Request = request;
        return Task.FromResult(page);
    }
}
