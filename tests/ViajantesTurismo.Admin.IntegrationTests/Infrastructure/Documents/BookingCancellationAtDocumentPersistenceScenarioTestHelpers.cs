using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

internal static class BookingCancellationAtDocumentPersistenceScenarioTestHelpers
{
    public static async Task<PostgresException?> CaptureCreateFailure(
        NpgsqlDataSource dataSource,
        CancellationToken ct)
    {
        try
        {
            await using var scenario = await BookingCancellationAtDocumentPersistenceScenario.Create(
                dataSource,
                Guid.CreateVersion7(),
                ct);
            return null;
        }
        catch (PostgresException exception)
        {
            return exception;
        }
    }

    public static async Task<ObjectDisposedException?> CaptureOpenFailure(
        NpgsqlDataSource dataSource,
        CancellationToken ct)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(ct);
            return null;
        }
        catch (ObjectDisposedException exception)
        {
            return exception;
        }
    }
}
