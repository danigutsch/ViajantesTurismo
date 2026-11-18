using Npgsql;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Bookings;

/// <summary>Holds row locks on two bookings so tests can observe concurrent confirmations queueing on the tour capacity lock.</summary>
internal sealed class BookingCapacityConcurrencyScenario : IAsyncDisposable
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private readonly NpgsqlDataSource dataSource;
    private readonly Guid firstBookingId;
    private readonly Guid secondBookingId;
    private readonly Guid tourId;
    private NpgsqlConnection? blockingConnection;
    private NpgsqlTransaction? blockingTransaction;
    private int? blockingProcessId;

    private BookingCapacityConcurrencyScenario(
        NpgsqlDataSource dataSource,
        Guid firstBookingId,
        Guid secondBookingId,
        Guid tourId)
    {
        this.dataSource = dataSource;
        this.firstBookingId = firstBookingId;
        this.secondBookingId = secondBookingId;
        this.tourId = tourId;
    }

    /// <summary>Creates the scenario from a connection string, requiring both bookings to belong to one tour.</summary>
    public static async Task<BookingCapacityConcurrencyScenario> Create(
        string connectionString,
        Guid firstBookingId,
        Guid secondBookingId,
        CancellationToken ct)
    {
        return await Create(
            NpgsqlDataSource.Create(connectionString),
            firstBookingId,
            secondBookingId,
            ct);
    }

    /// <summary>Creates the scenario from a data source, requiring both bookings to belong to one tour; disposes the data source on failure.</summary>
    internal static async Task<BookingCapacityConcurrencyScenario> Create(
        NpgsqlDataSource dataSource,
        Guid firstBookingId,
        Guid secondBookingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        try
        {
            await using var command = dataSource.CreateCommand(
                """
                SELECT "TourId"
                FROM "Booking"
                WHERE "Id" IN (@firstBookingId, @secondBookingId)
                GROUP BY "TourId"
                HAVING COUNT(*) = 2;
                """);
            command.Parameters.AddWithValue("firstBookingId", firstBookingId);
            command.Parameters.AddWithValue("secondBookingId", secondBookingId);
            var tourIdValue = await command.ExecuteScalarAsync(ct);
            var tourId = tourIdValue is Guid value
                ? value
                : throw new InvalidOperationException(
                    "The capacity concurrency scenario requires two bookings on one Tour.");

            return new BookingCapacityConcurrencyScenario(
                dataSource,
                firstBookingId,
                secondBookingId,
                tourId);
        }
        catch
        {
            await dataSource.DisposeAsync();
            throw;
        }
    }

    /// <summary>Opens a transaction that locks both booking rows until released.</summary>
    public async Task HoldBookingWrites(CancellationToken ct)
    {
        if (blockingTransaction is not null)
        {
            throw new InvalidOperationException("Booking writes are already held.");
        }

        var connection = await dataSource.OpenConnectionAsync(ct);
        var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            await using var processIdCommand = new NpgsqlCommand("SELECT pg_backend_pid();", connection, transaction);
            var processId = await processIdCommand.ExecuteScalarAsync(ct);

            await using var bookingLockCommand = new NpgsqlCommand(
                """
                UPDATE "Booking"
                SET "Status" = "Status"
                WHERE "Id" IN (@firstBookingId, @secondBookingId);
                """,
                connection,
                transaction);
            bookingLockCommand.Parameters.AddWithValue("firstBookingId", firstBookingId);
            bookingLockCommand.Parameters.AddWithValue("secondBookingId", secondBookingId);
            var updatedRows = await bookingLockCommand.ExecuteNonQueryAsync(ct);
            if (updatedRows != 2)
            {
                throw new InvalidOperationException("The capacity concurrency scenario could not lock both bookings.");
            }

            blockingConnection = connection;
            blockingTransaction = transaction;
            blockingProcessId = Convert.ToInt32(processId, global::System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>Waits until two other requests are blocked on the held writes or the tour capacity lock.</summary>
    public async Task WaitForConcurrentRequests(CancellationToken ct)
    {
        await WaitForRequests(2, ct);
    }

    /// <summary>Waits until one other request is blocked on the held writes or the tour capacity lock.</summary>
    public async Task WaitForBookingWrite(CancellationToken ct)
    {
        await WaitForRequests(1, ct);
    }

    private async Task WaitForRequests(int expectedRequestCount, CancellationToken ct)
    {
        var processId = blockingProcessId
            ?? throw new InvalidOperationException("Booking writes are not held.");
        using var timeoutCts = new CancellationTokenSource(WaitTimeout);
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
                    SELECT COUNT(*) >= @expectedRequestCount
                    FROM blocked_requests
                    WHERE pid IS NOT NULL
                      AND pid <> @processId;
                    """,
                    connection);
                command.Parameters.AddWithValue("processId", processId);
                command.Parameters.AddWithValue("expectedRequestCount", expectedRequestCount);
                var requestsAreWaiting = await command.ExecuteScalarAsync(linkedCts.Token);
                if (requestsAreWaiting is true)
                {
                    return;
                }

                await Task.Yield();
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException("Concurrent booking confirmations did not reach the persistence barrier.");
        }

        ct.ThrowIfCancellationRequested();
        throw new TimeoutException("Concurrent booking confirmations did not reach the persistence barrier.");
    }

    /// <summary>Checks whether a separate session can immediately take the tour row lock.</summary>
    public async Task<bool> CanLockTour(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT "Id"
            FROM "Tours"
            WHERE "Id" = @tourId
            FOR UPDATE NOWAIT;
            """);
        command.Parameters.AddWithValue("tourId", tourId);

        try
        {
            var lockedId = await command.ExecuteScalarAsync(ct);
            if (lockedId is not Guid id || id != tourId)
            {
                throw new InvalidOperationException("The capacity concurrency scenario could not find the tour.");
            }

            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            return false;
        }
    }

    /// <summary>Rolls back the holding transaction and releases the booking rows.</summary>
    public async Task ReleaseBookingWrites(CancellationToken ct)
    {
        var transaction = blockingTransaction
            ?? throw new InvalidOperationException("Booking writes are not held.");
        var connection = blockingConnection
            ?? throw new InvalidOperationException("Booking writes are not held.");

        await transaction.RollbackAsync(ct);
        blockingTransaction = null;
        blockingConnection = null;
        blockingProcessId = null;
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    /// <summary>Rolls back any held transaction and disposes the connection and data source.</summary>
    public async ValueTask DisposeAsync()
    {
        if (blockingTransaction is not null)
        {
            await blockingTransaction.RollbackAsync(CancellationToken.None);
            await blockingTransaction.DisposeAsync();
        }

        if (blockingConnection is not null)
        {
            await blockingConnection.DisposeAsync();
        }

        await dataSource.DisposeAsync();
    }
}
