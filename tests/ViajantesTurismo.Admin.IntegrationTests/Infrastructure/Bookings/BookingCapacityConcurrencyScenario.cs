using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Bookings;

internal sealed class BookingCapacityConcurrencyScenario : IAsyncDisposable
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private readonly NpgsqlDataSource dataSource;
    private readonly Guid firstBookingId;
    private readonly Guid secondBookingId;
    private readonly long tourCapacityLockKey;
    private NpgsqlConnection? blockingConnection;
    private NpgsqlTransaction? blockingTransaction;
    private int? blockingProcessId;

    private BookingCapacityConcurrencyScenario(
        NpgsqlDataSource dataSource,
        Guid firstBookingId,
        Guid secondBookingId,
        long tourCapacityLockKey)
    {
        this.dataSource = dataSource;
        this.firstBookingId = firstBookingId;
        this.secondBookingId = secondBookingId;
        this.tourCapacityLockKey = tourCapacityLockKey;
    }

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
            if (tourIdValue is not Guid tourId)
            {
                throw new InvalidOperationException("The capacity concurrency scenario requires two bookings on one Tour.");
            }

            return new BookingCapacityConcurrencyScenario(
                dataSource,
                firstBookingId,
                secondBookingId,
                CreateTourCapacityLockKey(tourId));
        }
        catch
        {
            await dataSource.DisposeAsync();
            throw;
        }
    }

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
            blockingProcessId = Convert.ToInt32(processId, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task WaitForConcurrentRequests(CancellationToken ct)
    {
        await WaitForRequests(2, ct);
    }

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
                    ),
                    target_tour_advisory_waiters(pid) AS (
                        SELECT advisory.pid
                        FROM pg_locks AS advisory
                        WHERE advisory.locktype = 'advisory'
                          AND advisory.database = (
                              SELECT oid
                              FROM pg_database
                              WHERE datname = current_database())
                          AND advisory.objsubid = 1
                          AND NOT advisory.granted
                          AND ((advisory.classid::bigint << 32) | advisory.objid::bigint)
                              = @tourCapacityLockKey
                    ),
                    target_request_pids(pid) AS (
                        SELECT pid FROM blocked_requests
                        UNION
                        SELECT pid FROM target_tour_advisory_waiters
                    )
                    SELECT COUNT(*) >= @expectedRequestCount
                    FROM target_request_pids
                    WHERE pid IS NOT NULL
                      AND pid <> @processId;
                    """,
                    connection);
                command.Parameters.AddWithValue("processId", processId);
                command.Parameters.AddWithValue("tourCapacityLockKey", tourCapacityLockKey);
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

    private static long CreateTourCapacityLockKey(Guid tourId)
    {
        var lockIdentity = string.Concat(
            "admin:tour-capacity:",
            tourId.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(lockIdentity));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
