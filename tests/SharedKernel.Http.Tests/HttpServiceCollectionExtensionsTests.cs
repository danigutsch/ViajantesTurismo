namespace SharedKernel.Http.Tests;

public sealed class HttpServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHttpClientDefaults_registers_http_client_factory_defaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpClientDefaults();
        using var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient("shared-kernel-http-tests");
        client.ShouldNotBeNull();
    }

    [Fact]
    public void AddHttpClientDefaults_throws_when_services_is_null()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act
        Action act = () => HttpServiceCollectionExtensions.AddHttpClientDefaults(services!);

        // Assert
        act.ShouldThrow<ArgumentNullException>();
    }
}
