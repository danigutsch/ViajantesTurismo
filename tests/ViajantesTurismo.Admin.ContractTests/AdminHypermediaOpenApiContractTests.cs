using SharedKernel.Testing;
using ViajantesTurismo.Admin.ContractTests.Infrastructure;
using static ViajantesTurismo.Admin.ContractTests.AdminHypermediaOpenApiAssertions;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>Verifies the link contract in the Admin boundary and versioned artifacts.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
[Trait(TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
public sealed class AdminHypermediaOpenApiContractTests
{
    [Theory]
    [InlineData("customers", "customers", "GetCustomerPageDto", "GetCustomerListItemDto", "GetCustomerDto", "CustomerDetailsDto")]
    [InlineData("bookings", "bookings", "GetBookingPageDto", "GetBookingDto", "GetBookingDto", "GetBookingDto")]
    [InlineData("tours", "tours", "GetTourPageDto", "GetTourListItemDto", "GetTourDto", "GetTourDto")]
    [InlineData("v1", "customers", "GetCustomerPageDto", "GetCustomerListItemDto", "GetCustomerDto", "CustomerDetailsDto")]
    [InlineData("v1", "bookings", "GetBookingPageDto", "GetBookingDto", "GetBookingDto", "GetBookingDto")]
    [InlineData("v1", "tours", "GetTourPageDto", "GetTourListItemDto", "GetTourDto", "GetTourDto")]
    public void Page_detail_and_create_responses_describe_resource_links(
        string documentName, string collection, string pageType, string summaryType, string createType, string detailType)
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot(documentName).AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var schemas = document["components"].ShouldNotBeNull()["schemas"].ShouldNotBeNull().AsObject();
        var collectionPath = $"/api/v1/{collection}";

        // Act
        var list = GetResponse(paths, $"{collectionPath}/page", "get", "200");
        var detail = GetResponse(paths, $"{collectionPath}/{{id}}", "get", "200");
        var created = GetResponse(paths, collectionPath, "post", "201");
        var itemArray = schemas[pageType].ShouldNotBeNull()["properties"].ShouldNotBeNull()["items"].ShouldNotBeNull();

        // Assert
        paths[collectionPath].ShouldNotBeNull()["get"].ShouldBeNull();
        GetJsonSchema(list)["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe($"#/components/schemas/{pageType}");
        itemArray["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("array");
        itemArray["items"].ShouldNotBeNull()["$ref"].ShouldNotBeNull().GetValue<string>()
            .ShouldBe($"#/components/schemas/{summaryType}");
        GetJsonSchema(detail)["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe($"#/components/schemas/{detailType}");
        GetJsonSchema(created)["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe($"#/components/schemas/{createType}");
        var location = created["headers"].ShouldNotBeNull()["Location"].ShouldNotBeNull();
        location["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        location["schema"].ShouldNotBeNull()["format"].ShouldNotBeNull().GetValue<string>().ShouldBe("uri-reference");

        AssertLinkProperty(schemas[summaryType].ShouldNotBeNull());
        AssertLinkProperty(schemas[createType].ShouldNotBeNull());
        AssertLinkProperty(schemas[detailType].ShouldNotBeNull());
        var linkFields = schemas["HypermediaLink"].ShouldNotBeNull()["properties"].ShouldNotBeNull().AsObject();
        var linkNames = linkFields.Select(field => field.Key).Order(StringComparer.Ordinal).ToArray();
        string[] expectedLinkNames = ["href", "method", "rel"];
        linkNames.ShouldBe(expectedLinkNames);
        foreach (var field in linkFields)
        {
            field.Value.ShouldNotBeNull()["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("string");
        }
    }

    [Theory]
    [InlineData("bookings", "tour", "tourId")]
    [InlineData("bookings", "customer", "customerId")]
    [InlineData("v1", "tour", "tourId")]
    [InlineData("v1", "customer", "customerId")]
    public void Filtered_booking_pages_use_the_same_linked_booking_schema(string documentName, string filter, string routeId)
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot(documentName).AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var schemas = document["components"].ShouldNotBeNull()["schemas"].ShouldNotBeNull().AsObject();
        var historyPath = $"/api/v1/bookings/{filter}/{{{routeId}}}";

        // Act
        var schema = GetJsonSchema(GetResponse(paths, $"{historyPath}/page", "get", "200"));
        var itemArray = schemas["GetBookingHistoryPageDto"].ShouldNotBeNull()["properties"].ShouldNotBeNull()["items"].ShouldNotBeNull();

        // Assert
        paths.ContainsKey(historyPath).ShouldBeFalse();
        schema["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe("#/components/schemas/GetBookingHistoryPageDto");
        itemArray["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("array");
        itemArray["items"].ShouldNotBeNull()["$ref"].ShouldNotBeNull().GetValue<string>()
            .ShouldBe("#/components/schemas/GetBookingDto");
    }
}
