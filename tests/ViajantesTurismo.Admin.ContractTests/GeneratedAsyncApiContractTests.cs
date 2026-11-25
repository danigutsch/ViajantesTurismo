using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.ContractTests.Infrastructure;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.RepositoryDocumentation.AsyncApi;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>Checks the published document against the current event contracts and AsyncAPI references.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.DriftGuardCategory)]
[Trait(TestTraitNames.SurfaceName, TestTraits.AsyncApiSurface)]
public sealed class GeneratedAsyncApiContractTests
{
    [Fact]
    public void The_generated_document_identifies_its_generator_source_and_refresh_commands()
    {
        // Arrange
        const string toolDirectory = "scripts/ViajantesTurismo.AsyncApi.Tool";
        const string artifactPath = "docs/asyncapi.json";
        const string generatorName = "ViajantesTurismo.AsyncApi.Tool";
        const string sourcePath = "src/ViajantesTurismo.RepositoryDocumentation/AsyncApi/AsyncApiDocument.cs";
        var guidance = File.ReadAllText(Path.Combine(ContractTestRepository.RootPath, "docs", "ASYNC_CONTRACTS.md"));
        using var document = JsonDocument.Parse(AsyncApiContractArtifact.Read());

        // Act
        var provenance = document.RootElement.GetProperty("x-viajantes").GetProperty("provenance");
        var refreshCommand = provenance.GetProperty("refreshCommand").GetString().ShouldNotBeNull();
        var verifyCommand = provenance.GetProperty("verifyCommand").GetString().ShouldNotBeNull();

        // Assert
        provenance.GetProperty("generator").GetString().ShouldBe(generatorName);
        provenance.GetProperty("source").GetString().ShouldBe(sourcePath);
        refreshCommand.ShouldBe($"dotnet run --project {toolDirectory} -- generate {artifactPath}");
        verifyCommand.ShouldBe($"dotnet run --project {toolDirectory} -- verify {artifactPath}");
        guidance.ShouldContain(refreshCommand, StringComparison.Ordinal);
        guidance.ShouldContain(verifyCommand, StringComparison.Ordinal);
    }

    [Fact]
    public void The_checked_in_document_matches_the_generator_output()
    {
        // Arrange
        var artifact = AsyncApiContractArtifact.Read();

        // Act
        var generated = AsyncApiDocument.Render();

        // Assert
        artifact.ShouldBe(generated);
    }

    [Fact]
    public void Every_durable_serializer_contract_has_a_documented_message()
    {
        // Arrange
        var contexts = new[]
        {
            typeof(AdminIntegrationEventJsonContext),
            typeof(BrandingIntegrationEventJsonContext),
            typeof(CatalogIntegrationEventJsonContext),
        };
        var expected = contexts.SelectMany(context => context.GetProperties())
            .Select(property => property.PropertyType)
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonTypeInfo<>))
            .Select(type => type.GenericTypeArguments[0])
            .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();
        using var document = JsonDocument.Parse(AsyncApiContractArtifact.Read());
        var messages = document.RootElement.GetProperty("components").GetProperty("messages");

        // Act
        var documented = messages.EnumerateObject()
            .Select(message => message.Value.GetProperty("x-viajantes").GetProperty("sourceType").GetString())
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        documented.ShouldBe(expected);
    }

    [Fact]
    public void The_published_payload_names_match_the_runtime_serializers()
    {
        // Arrange
        using var document = JsonDocument.Parse(AsyncApiContractArtifact.Read());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        (string Schema, JsonTypeInfo Contract)[] contracts =
        [
            ("AdminTourCreatedIntegrationEventV1", AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent),
            ("AdminTourRetiredIntegrationEventV1", AdminIntegrationEventJsonContext.Default.AdminTourRetiredIntegrationEvent),
            ("BrandingSettingsChangedIntegrationEventV1", BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent),
            ("MediaImageOriginalStoredIntegrationEventV1", CatalogIntegrationEventJsonContext.Default.MediaImageOriginalStoredIntegrationEvent),
            ("CatalogTourRetirementAppliedIntegrationEventV1", CatalogIntegrationEventJsonContext.Default.CatalogTourRetirementAppliedIntegrationEvent),
        ];

        // Act
        var propertyNames = contracts.Select(contract =>
            (Actual: schemas.GetProperty(contract.Schema).GetProperty("properties").EnumerateObject()
                .Select(property => property.Name).Order(StringComparer.Ordinal).ToArray(),
             Expected: contract.Contract.Properties.Select(property => property.Name).Order(StringComparer.Ordinal).ToArray())).ToArray();

        // Assert
        foreach (var (actual, expected) in propertyNames)
        {
            actual.ShouldBe(expected);
        }
    }

    [Fact]
    public void The_document_includes_the_durable_branding_channel()
    {
        // Arrange
        using var document = JsonDocument.Parse(AsyncApiContractArtifact.Read());
        var root = document.RootElement;
        var channels = root.GetProperty("channels");

        // Act
        var present = channels.TryGetProperty(BrandingSettingsChangedIntegrationEvent.EventType, out var channel);

        // Assert
        present.ShouldBeTrue();
        channel.GetProperty("address").GetString().ShouldBe(BrandingSettingsChangedIntegrationEvent.EventType);
        var message = root.GetProperty("components").GetProperty("messages").GetProperty("BrandingSettingsChangedV1");
        var version = message.GetProperty("headers").GetProperty("properties").GetProperty("eventVersion").GetProperty("const");
        version.GetInt32().ShouldBe(BrandingSettingsChangedIntegrationEvent.EventVersion);
        var branding = message.GetProperty("x-viajantes");
        branding.GetProperty("consumerNames")[0].GetString().ShouldBe(IntegrationEventConsumerNames.Admin);
        branding.GetProperty("outboxOwner").GetString().ShouldBe("BrandingDbContext");
        branding.GetProperty("inboxOwner").GetString().ShouldBe("AdminWriteDbContext");
        branding.GetProperty("transportConsumer").GetString().ShouldBe("AdminBrandingTransportDbContext");
    }

    [Fact]
    public void Branding_payload_describes_a_relative_logo_URI_and_omittable_logo_fields()
    {
        // Arrange
        var branding = new BrandingSettingsChangedIntegrationEvent(
            Guid.Empty, DateTimeOffset.UnixEpoch, Guid.Empty, 1, "Test", "#000000", "#ffffff", "#ffffff", "#000000",
            "serif", "sans-serif", new Uri("/logo", UriKind.Relative));
        using var document = JsonDocument.Parse(AsyncApiContractArtifact.Read());
        var schema = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("BrandingSettingsChangedIntegrationEventV1");

        // Act
        var payload = JsonSerializer.SerializeToElement(branding, BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);

        // Assert
        var required = schema.GetProperty("required").EnumerateArray().Select(property => property.GetString()).ToArray();
        var emitted = payload.EnumerateObject().Select(property => property.Name).ToArray();
        required.ShouldBe(emitted);
        var properties = schema.GetProperty("properties");
        payload.GetProperty(nameof(BrandingSettingsChangedIntegrationEvent.LogoUri)).GetString().ShouldBe("/logo");
        properties.GetProperty(nameof(BrandingSettingsChangedIntegrationEvent.LogoUri)).GetProperty("format").GetString().ShouldBe("uri-reference");
        properties.GetProperty(nameof(BrandingSettingsChangedIntegrationEvent.LogoContent)).GetProperty("contentEncoding").GetString().ShouldBe("base64");
    }

    [Fact]
    public void Catalog_topology_lists_the_idempotent_inbox_and_remote_transport_separately()
    {
        // Arrange
        using var document = JsonDocument.Parse(AsyncApiContractArtifact.Read());
        var messages = document.RootElement.GetProperty("components").GetProperty("messages");

        // Act
        var adminTour = messages.GetProperty("AdminTourCreatedV1").GetProperty("x-viajantes");
        var media = messages.GetProperty("MediaImageOriginalStoredV1").GetProperty("x-viajantes");

        // Assert
        adminTour.GetProperty("inboxOwner").GetString().ShouldBe("CatalogDbContext");
        adminTour.GetProperty("transportConsumer").GetString().ShouldBe("CatalogIntegrationTransportDbContext");
        media.GetProperty("inboxOwner").GetString().ShouldBe("CatalogDbContext");
        media.TryGetProperty("transportConsumer", out _).ShouldBeFalse();
    }

    [Fact]
    public void Operation_messages_reference_the_messages_in_the_associated_channel()
    {
        // Arrange
        using var document = JsonDocument.Parse(AsyncApiContractArtifact.Read());
        var root = document.RootElement;
        var channels = root.GetProperty("channels");

        // Act
        var operations = root.GetProperty("operations").EnumerateObject().ToArray();

        // Assert
        foreach (var operation in operations)
        {
            var channelReference = operation.Value.GetProperty("channel").GetProperty("$ref").GetString().ShouldNotBeNull();
            var channelName = channelReference["#/channels/".Length..];
            var allowedMessages = channels.GetProperty(channelName).GetProperty("messages").EnumerateObject()
                .Select(message => $"{channelReference}/messages/{message.Name}").ToArray();
            foreach (var message in operation.Value.GetProperty("messages").EnumerateArray())
            {
                var reference = message.GetProperty("$ref").GetString().ShouldNotBeNull();
                allowedMessages.ShouldContain(reference);
            }
        }
    }
}
