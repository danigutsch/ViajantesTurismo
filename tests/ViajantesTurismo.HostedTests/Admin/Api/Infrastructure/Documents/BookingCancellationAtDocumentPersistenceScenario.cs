using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

internal sealed class BookingCancellationAtDocumentPersistenceScenario : IAsyncDisposable
{
    private static readonly TimeSpan LockWaitTimeout = TimeSpan.FromSeconds(10);

    private readonly NpgsqlDataSource dataSource;
    private readonly Guid bookingId;
    private NpgsqlConnection? cancellationConnection;
    private NpgsqlTransaction? cancellationTransaction;
    private int? cancellationProcessId;

    private BookingCancellationAtDocumentPersistenceScenario(NpgsqlDataSource dataSource, Guid bookingId)
    {
        this.dataSource = dataSource;
        this.bookingId = bookingId;
    }

    public static Task<BookingCancellationAtDocumentPersistenceScenario> Create(
        string connectionString,
        Guid bookingId,
        CancellationToken ct) =>
        Create(NpgsqlDataSource.Create(connectionString), bookingId, ct);

    internal static async Task<BookingCancellationAtDocumentPersistenceScenario> Create(
        NpgsqlDataSource dataSource,
        Guid bookingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(ct);
            await using var command = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM \"Booking\" WHERE \"Id\" = @bookingId);",
                connection);
            command.Parameters.AddWithValue("bookingId", bookingId);
            var bookingExists = await command.ExecuteScalarAsync(ct);
            if (bookingExists is not true)
            {
                throw new InvalidOperationException("The booking cancellation scenario requires an existing booking.");
            }

            return new BookingCancellationAtDocumentPersistenceScenario(dataSource, bookingId);
        }
        catch
        {
            await dataSource.DisposeAsync();
            throw;
        }
    }

    public async Task HoldCancellation(CancellationToken ct)
    {
        if (cancellationTransaction is not null)
        {
            throw new InvalidOperationException("The booking cancellation transaction is already active.");
        }

        var connection = await dataSource.OpenConnectionAsync(ct);
        var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            await using var processIdCommand = new NpgsqlCommand("SELECT pg_backend_pid();", connection, transaction);
            var processId = await processIdCommand.ExecuteScalarAsync(ct);

            await using var cancellationCommand = new NpgsqlCommand(
                "UPDATE \"Booking\" SET \"Status\" = 'Cancelled' WHERE \"Id\" = @bookingId;",
                connection,
                transaction);
            cancellationCommand.Parameters.AddWithValue("bookingId", bookingId);
            var updatedRows = await cancellationCommand.ExecuteNonQueryAsync(ct);
            if (updatedRows != 1)
            {
                throw new InvalidOperationException("The booking cancellation scenario could not update the booking.");
            }

            cancellationConnection = connection;
            cancellationTransaction = transaction;
            cancellationProcessId = Convert.ToInt32(processId, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task WaitForBlockedDocumentPersistence(int expectedBlockedRequestCount, CancellationToken ct)
    {
        var processId = cancellationProcessId ?? throw new InvalidOperationException("The booking cancellation transaction is not active.");
        using var timeoutCts = new CancellationTokenSource(LockWaitTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            while (!linkedCts.IsCancellationRequested)
            {
                await using var connection = await dataSource.OpenConnectionAsync(linkedCts.Token);
                await using var command = new NpgsqlCommand(
                    """
                    WITH RECURSIVE blocked_requests(pid) AS (
                        SELECT blocked.pid
                        FROM pg_stat_activity AS blocked
                        WHERE @processId = ANY(pg_blocking_pids(blocked.pid))

                        UNION

                        SELECT blocked.pid
                        FROM pg_stat_activity AS blocked
                        INNER JOIN blocked_requests AS blocker
                            ON blocker.pid = ANY(pg_blocking_pids(blocked.pid))
                    )
                    SELECT COUNT(*) >= @expectedBlockedRequestCount
                    FROM blocked_requests;
                    """,
                    connection);
                command.Parameters.AddWithValue("processId", processId);
                command.Parameters.AddWithValue("expectedBlockedRequestCount", expectedBlockedRequestCount);
                var requestsAreBlocked = await command.ExecuteScalarAsync(linkedCts.Token);
                if (requestsAreBlocked is true)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25), linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException("Document persistence did not wait for the booking cancellation transaction.");
        }

        ct.ThrowIfCancellationRequested();
        throw new TimeoutException("Document persistence did not wait for the booking cancellation transaction.");
    }

    public async Task CommitCancellation(CancellationToken ct)
    {
        var transaction = cancellationTransaction ?? throw new InvalidOperationException("The booking cancellation transaction is not active.");
        var connection = cancellationConnection ?? throw new InvalidOperationException("The booking cancellation connection is not active.");

        await transaction.CommitAsync(ct);
        cancellationTransaction = null;
        cancellationConnection = null;
        cancellationProcessId = null;
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    public async Task RollbackCancellation(CancellationToken ct)
    {
        var transaction = cancellationTransaction ?? throw new InvalidOperationException("The booking cancellation transaction is not active.");
        var connection = cancellationConnection ?? throw new InvalidOperationException("The booking cancellation connection is not active.");

        await transaction.RollbackAsync(ct);
        cancellationTransaction = null;
        cancellationConnection = null;
        cancellationProcessId = null;
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    public async Task<string> GetBookingStatus(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT \"Status\" FROM \"Booking\" WHERE \"Id\" = @bookingId;");
        command.Parameters.AddWithValue("bookingId", bookingId);
        var status = await command.ExecuteScalarAsync(ct);
        return Convert.ToString(status, System.Globalization.CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("The booking status was not found.");
    }

    public async ValueTask DisposeAsync()
    {
        if (cancellationTransaction is not null)
        {
            await cancellationTransaction.RollbackAsync(CancellationToken.None);
            await cancellationTransaction.DisposeAsync();
        }

        if (cancellationConnection is not null)
        {
            await cancellationConnection.DisposeAsync();
        }

        await dataSource.DisposeAsync();
    }
}
