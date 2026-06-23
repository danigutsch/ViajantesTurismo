using System.Text.Json;
using Aspire.Hosting.Testing;

namespace SharedKernel.EventSourcing.PostgreSQL.Tests;

public sealed class PostgreSqlEventStoreTests : IAsyncLifetime
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "eventstore";
    private static readonly TimeSpan ResourceStartupTimeout = TimeSpan.FromSeconds(90);

    private IDistributedApplicationTestingBuilder? appBuilder;
    private DistributedApplication? app;
    private string? connectionString;

    public async ValueTask InitializeAsync()
    {
        appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SharedKernel_EventSourcing_PostgreSQL_AppHost>();
        app = await appBuilder.BuildAsync();
        await app.StartAsync();

        using var cts = new CancellationTokenSource(ResourceStartupTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync(PostgreSqlResourceName, cts.Token);
        connectionString = await app.GetConnectionStringAsync(DatabaseResourceName, cts.Token)
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured.");
    }

    public async ValueTask DisposeAsync()
    {
        var application = app;
        var builder = appBuilder;
        app = null;
        appBuilder = null;
        connectionString = null;

        try
        {
            if (application is not null)
            {
                await application.StopAsync();
                await application.DisposeAsync();
            }
        }
        finally
        {
            if (builder is not null)
            {
                await builder.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task Append_With_No_Stream_Loads_Persisted_Events_In_Revision_Order()
    {
        // Arrange
        var options = CreateOptions();
        await using var store = new PostgreSqlEventStore(ConnectionString, new TestEventSerializer(), options);
        await store.Initialize(TestContext.Current.CancellationToken);
        var streamId = StreamId.From("catalog-tour-test-1");
        var events = new TestEvent[]
        {
            new("draft-created"),
            new("published"),
        };

        // Act
        await store.Append(streamId, ExpectedStreamRevision.NoStream, events, TestContext.Current.CancellationToken);
        var envelopes = await store.Load(streamId, afterRevision: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Collection(
            envelopes,
            first =>
            {
                Assert.Equal(1, first.Revision.Value);
                Assert.Equal(TestEventSerializer.EventType, first.EventType);
                var eventData = Assert.IsType<TestEvent>(first.Data);
                Assert.Equal("draft-created", eventData.Name);
            },
            second =>
            {
                Assert.Equal(2, second.Revision.Value);
                Assert.Equal(TestEventSerializer.EventType, second.EventType);
                var eventData = Assert.IsType<TestEvent>(second.Data);
                Assert.Equal("published", eventData.Name);
            });
    }

    [Fact]
    public async Task Append_With_Stale_Expected_Revision_Reports_Conflict()
    {
        // Arrange
        var options = CreateOptions();
        await using var store = new PostgreSqlEventStore(ConnectionString, new TestEventSerializer(), options);
        await store.Initialize(TestContext.Current.CancellationToken);
        var streamId = StreamId.From("catalog-tour-test-2");
        await store.Append(
            streamId,
            ExpectedStreamRevision.NoStream,
            [new TestEvent("draft-created")],
            TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ExpectedStreamRevisionConflictException>(
            () => store.Append(
                streamId,
                ExpectedStreamRevision.NoStream,
                [new TestEvent("published")],
                TestContext.Current.CancellationToken).AsTask());

        // Assert
        Assert.Equal(streamId, exception.StreamId);
        Assert.True(exception.ExpectedRevision.RequiresEmptyStream);
        var actualRevision = Assert.IsType<StreamRevision>(exception.ActualRevision);
        Assert.Equal(1, actualRevision.Value);
    }

    [Fact]
    public async Task Save_Upserts_Projection_Checkpoint()
    {
        // Arrange
        var options = CreateOptions();
        await using var store = new PostgreSqlProjectionCheckpointStore(ConnectionString, options);
        await store.Initialize(TestContext.Current.CancellationToken);
        var firstCheckpoint = new ProjectionCheckpoint("catalog-public-listing", 12);
        var secondCheckpoint = new ProjectionCheckpoint("catalog-public-listing", 27);

        // Act
        await store.Save(firstCheckpoint, TestContext.Current.CancellationToken);
        await store.Save(secondCheckpoint, TestContext.Current.CancellationToken);
        var savedCheckpoint = await store.GetCheckpoint("catalog-public-listing", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(savedCheckpoint);
        Assert.Equal("catalog-public-listing", savedCheckpoint.ProjectionName);
        Assert.Equal(27, savedCheckpoint.Position);
    }

    [Fact]
    public async Task Save_Does_Not_Move_Projection_Checkpoint_Backward()
    {
        // Arrange
        var options = CreateOptions();
        await using var store = new PostgreSqlProjectionCheckpointStore(ConnectionString, options);
        await store.Initialize(TestContext.Current.CancellationToken);
        var currentCheckpoint = new ProjectionCheckpoint("catalog-public-listing", 27);
        var staleCheckpoint = new ProjectionCheckpoint("catalog-public-listing", 12);

        // Act
        await store.Save(currentCheckpoint, TestContext.Current.CancellationToken);
        await store.Save(staleCheckpoint, TestContext.Current.CancellationToken);
        var savedCheckpoint = await store.GetCheckpoint("catalog-public-listing", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(savedCheckpoint);
        Assert.Equal(27, savedCheckpoint.Position);
    }

    [Fact]
    public async Task Append_With_Concurrent_No_Stream_Writers_Reports_Conflicts()
    {
        // Arrange
        var options = CreateOptions();
        await using var store = new PostgreSqlEventStore(ConnectionString, new TestEventSerializer(), options);
        await store.Initialize(TestContext.Current.CancellationToken);
        var streamId = StreamId.From("catalog-tour-concurrent");

        // Act
        var appendTasks = Enumerable.Range(1, 10)
            .Select(index => CaptureAppendResult(store, streamId, new TestEvent($"event-{index}")))
            .ToArray();
        var results = await Task.WhenAll(appendTasks);

        // Assert
        Assert.Equal(1, results.Count(result => result is null));
        Assert.Equal(9, results.OfType<ExpectedStreamRevisionConflictException>().Count());
        var envelopes = await store.Load(streamId, afterRevision: null, TestContext.Current.CancellationToken);
        var envelope = Assert.Single(envelopes);
        Assert.Equal(1, envelope.Position);
        Assert.Equal(1, envelope.Revision.Value);
    }

    [Fact]
    public async Task Load_After_Loads_Events_In_Global_Position_Order()
    {
        // Arrange
        var options = CreateOptions();
        await using var store = new PostgreSqlEventStore(ConnectionString, new TestEventSerializer(), options);
        await store.Initialize(TestContext.Current.CancellationToken);
        var firstStreamId = StreamId.From("catalog-tour-global-1");
        var secondStreamId = StreamId.From("catalog-tour-global-2");
        await store.Append(
            firstStreamId,
            ExpectedStreamRevision.NoStream,
            [new TestEvent("first")],
            TestContext.Current.CancellationToken);
        await store.Append(
            secondStreamId,
            ExpectedStreamRevision.NoStream,
            [new TestEvent("second")],
            TestContext.Current.CancellationToken);
        var firstEnvelope = Assert.Single(await store.Load(firstStreamId, afterRevision: null, TestContext.Current.CancellationToken));

        // Act
        var envelopes = await store.LoadAfter(firstEnvelope.Position, maxCount: 10, TestContext.Current.CancellationToken);

        // Assert
        var envelope = Assert.Single(envelopes);
        Assert.Equal(secondStreamId, envelope.StreamId);
        Assert.True(envelope.Position > firstEnvelope.Position);
        var eventData = Assert.IsType<TestEvent>(envelope.Data);
        Assert.Equal("second", eventData.Name);
    }

    private string ConnectionString => connectionString ?? throw new InvalidOperationException("Fixture is not initialized.");

    private static PostgreSqlEventSourcingOptions CreateOptions() => new()
    {
        Schema = $"es_{Guid.NewGuid():N}",
    };

    private sealed record TestEvent(string Name);

    private static async Task<Exception?> CaptureAppendResult(
        PostgreSqlEventStore store,
        StreamId streamId,
        TestEvent eventData)
    {
        try
        {
            await store.Append(
                streamId,
                ExpectedStreamRevision.NoStream,
                [eventData],
                TestContext.Current.CancellationToken);

            return null;
        }
        catch (ExpectedStreamRevisionConflictException exception)
        {
            return exception;
        }
    }

    private sealed class TestEventSerializer : IEventSerializer
    {
        public const string EventType = "test.event.v1";

        public string GetEventType(object eventData)
        {
            ArgumentNullException.ThrowIfNull(eventData);

            return eventData is TestEvent ? EventType : throw new InvalidOperationException("Unsupported event type.");
        }

        public string Serialize(object eventData)
        {
            ArgumentNullException.ThrowIfNull(eventData);

            return JsonSerializer.Serialize((TestEvent)eventData);
        }

        public object Deserialize(string eventType, string payloadJson)
        {
            if (!string.Equals(EventType, eventType, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported event type '{eventType}'.");
            }

            return JsonSerializer.Deserialize<TestEvent>(payloadJson)
                ?? throw new InvalidOperationException("Event payload could not be deserialized.");
        }
    }
}
