using System.Text.Json;
using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class GeneratedEndpointRouteMapTests
{
    [Theory]
    [InlineData("Admin", "v1")]
    [InlineData("Catalog", "v1")]
    [InlineData("Catalog", "catalog")]
    [InlineData("Catalog", "public-catalog")]
    [InlineData("Branding", "branding")]
    [InlineData("Branding", "public-branding")]
    public void Published_API_operations_have_unique_operation_names(string context, string document)
    {
        // Arrange
        var source = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", $"ViajantesTurismo.{context}.Contracts.Http", "OpenApi", $"{document}.openapi.json");
        using var artifact = JsonDocument.Parse(File.ReadAllText(source));

        // Act
        var operations = artifact.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(method => (Route: $"{method.Name} {path.Name}", Operation: method.Value)))
            .Where(item => item.Operation.ValueKind == JsonValueKind.Object && item.Operation.TryGetProperty("responses", out _))
            .ToArray();
        var unnamedRoutes = operations.Where(item => !item.Operation.TryGetProperty("operationId", out var id)
            || string.IsNullOrWhiteSpace(id.GetString())).Select(item => item.Route).ToArray();

        // Assert
        operations.ShouldNotBeEmpty();
        unnamedRoutes.ShouldBeEmpty();
        var names = operations.Select(item => item.Operation.GetProperty("operationId").GetString()).ToArray();
        names.Distinct(StringComparer.Ordinal).Count().ShouldBe(operations.Length);
    }

    [Theory]
    [InlineData("GET", "/api/v1/bookings/{bookingId:guid}/finances", "GetBookingFinancialHistory", "management API", "required")]
    [InlineData("GET", "/api/v1/payment-accounts", "ListPaymentAccounts", "management API", "required")]
    [InlineData("GET", "/api/v1/documents/bookings/{bookingId:guid}/payment-instructions", "ListBookingPaymentInstructions", "management API", "required")]
    [InlineData("POST", "/api/v1/documents/{id:guid}/holds", "PlaceDocumentHold", "management API", "required")]
    [InlineData("GET", "/api/v1/public/catalog/tours", "GetPublishedTours", "public API", "anonymous")]
    [InlineData("GET", "/documents/{id:guid}/download", "Download", "management web", "required")]
    public void The_route_map_preserves_full_paths_audiences_and_declared_authorization(
        string method, string path, string operation, string audience, string authorization)
    {
        // Arrange
        var document = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "docs", "architecture", "generated-endpoint-route-map.md");
        var expected = $"| `{method}` | `{path}` | {operation} | {audience} | {authorization} |";

        // Act
        var artifact = File.ReadAllText(document);

        // Assert
        artifact.ShouldContain(expected, StringComparison.Ordinal);
    }
}
