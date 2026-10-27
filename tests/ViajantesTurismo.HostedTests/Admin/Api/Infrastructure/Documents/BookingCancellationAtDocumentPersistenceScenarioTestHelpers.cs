using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

/// <summary>Captures failures raised while creating or opening resources for the booking cancellation scenario.</summary>
internal static class BookingCancellationAtDocumentPersistenceScenarioTestHelpers
{
    /// <summary>Tries to create the scenario for a random booking id and returns the <see cref="PostgresException"/> if one is thrown, otherwise null.</summary>
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

    /// <summary>Tries to open a connection and returns the <see cref="ObjectDisposedException"/> if the data source is disposed, otherwise null.</summary>
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
