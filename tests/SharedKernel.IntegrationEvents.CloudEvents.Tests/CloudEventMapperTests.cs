namespace SharedKernel.IntegrationEvents.CloudEvents.Tests;

public sealed class CloudEventMapperTests
{
    [Fact]
    public void ToCloudEvent_maps_typed_event_metadata_to_a_cloudevents_envelope()
    {
        var eventId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var integrationEvent = new TestIntegrationEvent(eventId, occurredAt, "tour-created");
        var source = new Uri("https://viajantes.example/admin/tours");
        var dataSchema = new Uri("https://viajantes.example/schemas/admin.tour.created.v1.json");
        var metadata = new CloudEventMetadata(source, "tour-42", "application/json", dataSchema);

        var envelope = CloudEventMapper.ToCloudEvent(integrationEvent, metadata);

        Assert.Equal(eventId.ToString("D"), envelope.Id);
        Assert.Equal(source, envelope.Source);
        Assert.Equal("admin.tour.created", envelope.Type);
        Assert.Equal("1.0", envelope.SpecVersion);
        Assert.Equal(occurredAt, envelope.Time);
        Assert.Equal("tour-42", envelope.Subject);
        Assert.Equal("application/json", envelope.DataContentType);
        Assert.Equal(dataSchema, envelope.DataSchema);
        Assert.Equal(1, envelope.Version);
        Assert.Same(integrationEvent, envelope.Data);
    }

    [Fact]
    public void ToCloudEvent_preserves_optional_null_metadata()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "tour-created");
        var metadata = new CloudEventMetadata(new Uri("https://viajantes.example/admin/tours"));

        // Act
        var envelope = CloudEventMapper.ToCloudEvent(integrationEvent, metadata);

        // Assert
        Assert.Null(envelope.Subject);
        Assert.Null(envelope.DataContentType);
        Assert.Null(envelope.DataSchema);
    }

    [Fact]
    public void ToCloudEvent_rejects_null_integration_events()
    {
        // Arrange
        var metadata = new CloudEventMetadata(new Uri("https://viajantes.example/admin/tours"));
        var method = typeof(CloudEventMapper).GetMethod(nameof(CloudEventMapper.ToCloudEvent));
        Assert.NotNull(method);
        var genericMethod = method.MakeGenericMethod(typeof(TestIntegrationEvent));

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => genericMethod.Invoke(null, [null, metadata]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("integrationEvent", argumentException.ParamName);
    }

    [Fact]
    public void ToCloudEvent_rejects_null_metadata()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "tour-created");
        var method = typeof(CloudEventMapper).GetMethod(nameof(CloudEventMapper.ToCloudEvent));
        Assert.NotNull(method);
        var genericMethod = method.MakeGenericMethod(typeof(TestIntegrationEvent));

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => genericMethod.Invoke(null, [integrationEvent, null]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("metadata", argumentException.ParamName);
    }
}
