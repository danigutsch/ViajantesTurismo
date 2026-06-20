using System.Reflection;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public sealed class ErrorDocumentationCatalogTests
{
    [Fact]
    public void GetEntries_Collects_Generated_Error_Catalogs_From_Admin_Domain_And_Application()
    {
        // Arrange
        var catalogType = typeof(ViajantesTurismo.Admin.ApiService.ResultExtensions).Assembly
            .GetType("ViajantesTurismo.Admin.ApiService.ErrorDocumentationCatalog");
        Assert.NotNull(catalogType);

        var getEntries = catalogType.GetMethod(
            "GetEntries",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(getEntries);

        // Act
        var entries = (IReadOnlyList<GetErrorDocumentationDto>?)getEntries.Invoke(null, []);

        // Assert
        Assert.NotNull(entries);
        Assert.NotEmpty(entries);
        Assert.Contains(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Tours.TourErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "TourNotFound", StringComparison.Ordinal)
            && string.Equals(entry.Code, "not_found", StringComparison.Ordinal));
        Assert.Contains(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Application.Import.CsvErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "RequiredHeaderMissing", StringComparison.Ordinal)
            && string.Equals(entry.Code, "invalid", StringComparison.Ordinal));
        Assert.Contains(entries, static entry =>
            string.Equals(entry.ProviderType, "ViajantesTurismo.Admin.Domain.Customers.CustomerErrors", StringComparison.Ordinal)
            && string.Equals(entry.MemberName, "EmailAlreadyExists", StringComparison.Ordinal)
            && entry.HttpStatusCode == 409);
        Assert.All(entries, static entry => Assert.Equal("docs/errors/README.md", entry.DocumentationPath));
    }
}
