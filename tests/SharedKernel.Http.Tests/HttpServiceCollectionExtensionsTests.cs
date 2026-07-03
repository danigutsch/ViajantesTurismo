namespace SharedKernel.Http.Tests;

public sealed class HttpServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHttpClientDefaults_registers_http_client_factory_defaults()
    {
        // Act
        using var client = HttpClientDefaultsTestServices.CreateClient("shared-kernel-http-tests");

        // Assert
        client.ShouldNotBeNull();
    }

    [Fact]
    public void AddHttpClientDefaults_throws_when_services_is_null()
    {
        // Arrange
#nullable disable
        ServiceCollection services = null;

        // Act
        Action act = () => HttpServiceCollectionExtensions.AddHttpClientDefaults(services);
#nullable restore

        // Assert
        act.ShouldThrow<ArgumentNullException>();
    }
}
