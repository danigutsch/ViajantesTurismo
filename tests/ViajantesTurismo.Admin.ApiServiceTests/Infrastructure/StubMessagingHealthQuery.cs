using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Returns the configured result to the authorized messaging-health route.</summary>
internal sealed class StubMessagingHealthQuery(Task<GetMessagingHealthDto> result) : IMessagingHealthQuery
{
    /// <inheritdoc />
    public async Task<GetMessagingHealthDto> ReadMessagingHealth(CancellationToken ct) =>
        await result.WaitAsync(ct);
}
