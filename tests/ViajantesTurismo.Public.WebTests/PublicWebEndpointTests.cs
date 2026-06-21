using PublicWebProgram = Program;

namespace ViajantesTurismo.Public.WebTests;

public sealed class PublicWebEndpointTests
{
    [Fact]
    public async Task Root_Returns_Public_Landing_Page()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Viajantes Turismo", content, StringComparison.Ordinal);
        Assert.Contains("Public travel discovery experience coming soon.", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Default_Health_Endpoint_Returns_Success(string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Error_Endpoint_Returns_Problem_Response()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync(new Uri("/Error", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Production_Root_Returns_Public_Landing_Page()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_Default_Health_Endpoint_Is_Not_Exposed(string path)
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<PublicWebProgram> CreateFactory(string? environment = null)
    {
        var factory = new WebApplicationFactory<PublicWebProgram>();
        return environment is null
            ? factory
            : factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
    }
}
