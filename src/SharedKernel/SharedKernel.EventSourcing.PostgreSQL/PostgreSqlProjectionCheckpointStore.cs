using Npgsql;

namespace SharedKernel.EventSourcing.PostgreSQL;

/// <summary>
/// Persists projection checkpoints in PostgreSQL.
/// </summary>
public sealed class PostgreSqlProjectionCheckpointStore : IProjectionCheckpointStore, IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly PostgreSqlEventSourcingOptions options;
    private readonly bool ownsDataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProjectionCheckpointStore" /> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public PostgreSqlProjectionCheckpointStore(
        string connectionString)
        : this(connectionString, options: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProjectionCheckpointStore" /> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="options">The provider options.</param>
    public PostgreSqlProjectionCheckpointStore(
        string connectionString,
        PostgreSqlEventSourcingOptions? options)
        : this(NpgsqlDataSource.Create(connectionString), options, ownsDataSource: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProjectionCheckpointStore" /> class.
    /// </summary>
    /// <param name="dataSource">The PostgreSQL data source.</param>
    public PostgreSqlProjectionCheckpointStore(
        NpgsqlDataSource dataSource)
        : this(dataSource, options: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProjectionCheckpointStore" /> class.
    /// </summary>
    /// <param name="dataSource">The PostgreSQL data source.</param>
    /// <param name="options">The provider options.</param>
    public PostgreSqlProjectionCheckpointStore(
        NpgsqlDataSource dataSource,
        PostgreSqlEventSourcingOptions? options)
        : this(dataSource, options, ownsDataSource: false)
    {
    }

    private PostgreSqlProjectionCheckpointStore(
        NpgsqlDataSource dataSource,
        PostgreSqlEventSourcingOptions? options,
        bool ownsDataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        this.dataSource = dataSource;
        this.options = options ?? new PostgreSqlEventSourcingOptions();
        this.ownsDataSource = ownsDataSource;
    }

    /// <summary>
    /// Creates the provider schema and tables if they do not already exist.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public ValueTask Initialize(CancellationToken ct) => PostgreSqlEventSourcingSchema.Initialize(dataSource, options, ct);

    /// <inheritdoc />
    public async ValueTask<ProjectionCheckpoint?> GetCheckpoint(string projectionName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);

        var schema = PostgreSqlNames.Schema(options);
        var sql = $"""
            SELECT position
            FROM {schema}.projection_checkpoints
            WHERE projection_name = @projectionName;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("projectionName", projectionName);
        var result = await command.ExecuteScalarAsync(ct);

        return result is null or DBNull ? null : new ProjectionCheckpoint(projectionName, (long)result);
    }

    /// <inheritdoc />
    public async ValueTask Save(ProjectionCheckpoint checkpoint, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        var schema = PostgreSqlNames.Schema(options);
        var sql = $"""
            INSERT INTO {schema}.projection_checkpoints (projection_name, position, updated_at_utc)
            VALUES (@projectionName, @position, @updatedAt)
            ON CONFLICT (projection_name)
            DO UPDATE SET position = GREATEST(projection_checkpoints.position, EXCLUDED.position),
                          updated_at_utc = EXCLUDED.updated_at_utc;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("projectionName", checkpoint.ProjectionName);
        command.Parameters.AddWithValue("position", checkpoint.Position);
        command.Parameters.AddWithValue("updatedAt", DateTimeOffset.UtcNow);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (ownsDataSource)
        {
            await dataSource.DisposeAsync();
        }
    }
}
