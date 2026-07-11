using System.Reflection;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public sealed class ErrorDocumentationCatalogTests
{
    [Fact]
    public void GetEntries_collects_generated_error_catalogs_from_admin_domain_and_application()
    {
        // Arrange
        var catalogType = typeof(ResultExtensions).Assembly
            .GetType("ViajantesTurismo.Admin.ApiService.Errors.ErrorDocumentationCatalog");
        _ = TestAssert.NotNull(catalogType);

        var getEntries = catalogType.GetMethod(
            "GetEntries",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        _ = TestAssert.NotNull(getEntries);

        // Act
        var entries = (IReadOnlyList<GetErrorDocumentationDto>?)getEntries.Invoke(null, []);

        // Assert
        _ = TestAssert.NotNull(entries);
        TestAssert.NotEmpty(entries);
        TestAssert.Contains(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Tours.TourErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "TourNotFound", StringComparison.Ordinal)
            && string.Equals(entry.Code, "not_found", StringComparison.Ordinal));
        TestAssert.Contains(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Application.Import.CsvErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "RequiredHeaderMissing", StringComparison.Ordinal)
            && string.Equals(entry.Code, "invalid", StringComparison.Ordinal));
        TestAssert.Contains(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Customers.CustomerErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "EmailAlreadyExists", StringComparison.Ordinal)
            && entry.HttpStatusCode == 409);
        TestAssert.All(entries, static entry => TestAssert.Equal("docs/errors/README.md", entry.DocumentationPath));
    }
}
