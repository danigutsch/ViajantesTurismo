using Npgsql;

namespace ViajantesTurismo.HostedTests.Admin.System.Operations;

/// <summary>Holds the observer connection and transaction until the dashboard assertions finish.</summary>
internal sealed class CatalogGalleryDashboardWakeFence(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
{
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await using var observer = connection;
        await transaction.DisposeAsync();
    }
}
