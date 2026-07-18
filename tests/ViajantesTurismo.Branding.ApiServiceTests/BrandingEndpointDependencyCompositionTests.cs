using System.Text;
using Microsoft.AspNetCore.TestHost;
using TestTraits = ViajantesTurismo.Branding.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.DependencyInjectionCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingEndpointDependencyCompositionTests
{
    [Fact]
    public async Task Mapped_branding_mutation_endpoint_dependencies_resolve_from_the_composed_host()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateProductionComposition();

        // Assert
        BrandingApiTestHost.VerifyMappedMutationDependencies(factory);
    }

    [Fact]
    public async Task Failing_branding_settings_store_is_activated_by_the_settings_endpoint()
    {
        // Arrange
        await using var baseFactory = BrandingApiTestHost.Create();
        await using var factory = baseFactory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsStore>(
                    _ => throw new InvalidOperationException("Expected endpoint dependency activation.")))));
        using var client = factory.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }
}
