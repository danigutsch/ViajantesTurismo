using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[TestFocus(TestFocus.Regression)]
public sealed class AdminApiCrawlerPolicyTests
{
    [Fact]
    public async Task Robots_txt_disallows_admin_API_crawling()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.GetAsync(new Uri("/robots.txt", UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contentType = response.Content.Headers.ContentType.ShouldNotBeNull();
        contentType.MediaType.ShouldBe("text/plain");
        contentType.CharSet.ShouldBe("utf-8");
        body.ShouldBe("User-agent: *\nDisallow: /");
    }
}
