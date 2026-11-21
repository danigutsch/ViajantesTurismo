using PandaBear.Testing;
using ViajantesTurismo.Admin.ContractTests.Infrastructure;
using static ViajantesTurismo.Admin.ContractTests.AdminHypermediaOpenApiAssertions;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>Verifies the link contract in the Admin boundary and versioned artifacts.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
[Trait(TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
public sealed class AdminHypermediaOpenApiContractTests
{
    [Theory]
    [InlineData("customers", "customers", "GetCustomerDto", "CustomerDetailsDto")]
    [InlineData("bookings", "bookings", "GetBookingDto", "GetBookingDto")]
    [InlineData("tours", "tours", "GetTourDto", "GetTourDto")]
    [InlineData("v1", "customers", "GetCustomerDto", "CustomerDetailsDto")]
    [InlineData("v1", "bookings", "GetBookingDto", "GetBookingDto")]
    [InlineData("v1", "tours", "GetTourDto", "GetTourDto")]
    public void List_detail_and_create_responses_describe_resource_links(
        string documentName, string collection, string summaryType, string detailType)
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot(documentName).AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var schemas = document["components"].ShouldNotBeNull()["schemas"].ShouldNotBeNull().AsObject();
        var collectionPath = $"/api/v1/{collection}";

        // Act
        var list = GetResponse(paths, collectionPath, "get", "200");
        var detail = GetResponse(paths, $"{collectionPath}/{{id}}", "get", "200");
        var created = GetResponse(paths, collectionPath, "post", "201");
        var listSchema = GetJsonSchema(list);

        // Assert
        listSchema["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("array");
        listSchema["items"].ShouldNotBeNull()["$ref"].ShouldNotBeNull().GetValue<string>()
            .ShouldBe($"#/components/schemas/{summaryType}");
        GetJsonSchema(detail)["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe($"#/components/schemas/{detailType}");
        GetJsonSchema(created)["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe($"#/components/schemas/{summaryType}");
        var location = created["headers"].ShouldNotBeNull()["Location"].ShouldNotBeNull();
        location["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        location["schema"].ShouldNotBeNull()["format"].ShouldNotBeNull().GetValue<string>().ShouldBe("uri-reference");

        AssertLinkProperty(schemas[summaryType].ShouldNotBeNull());
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
    public void Filtered_booking_lists_use_the_same_linked_booking_schema(string documentName, string filter, string routeId)
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot(documentName).AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();

        // Act
        var schema = GetJsonSchema(GetResponse(paths, $"/api/v1/bookings/{filter}/{{{routeId}}}", "get", "200"));

        // Assert
        schema["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("array");
        schema["items"].ShouldNotBeNull()["$ref"].ShouldNotBeNull().GetValue<string>()
            .ShouldBe("#/components/schemas/GetBookingDto");
    }
}
