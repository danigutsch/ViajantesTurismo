using PandaBear.Testing;
using ViajantesTurismo.Management.WebTests.Services;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Verifies the country service registration shares one loaded country list across scopes.</summary>
[TestScope(TestScope.Unit)]
public sealed class CountryServiceCollectionExtensionsTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AddCountryService_shares_loaded_countries_across_scopes()
    {
        // Arrange
        CountryServiceTestsHelpers.WriteCountriesJson(_tempDir, """{"br": {"name": "Brazil"}}""");
        await using var host = new CountryServiceRegistrationHost(_tempDir);

        // Act
        var first = await host.GetCountriesFromNewScope(Xunit.TestContext.Current.CancellationToken);
        var second = await host.GetCountriesFromNewScope(Xunit.TestContext.Current.CancellationToken);

        // Assert
        second.ShouldBeSameAs(first);
    }
}
