using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Api;

namespace ViajantesTurismo.Admin.ApiServiceTests.Customers.Import;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class AdminImportRateLimitOverKestrelTests
{
    [Fact]
    public async Task Configures_admin_import_rate_limit_policy()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddAdminSecurityBaseline(new ConfigurationBuilder().Build());
        await using var app = builder.Build();
        app.UseRateLimiter();
        app.MapGet("/import", () => Results.Ok()).RequireRateLimiting(AdminSecurityBaseline.ImportRateLimitPolicy);
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.ShouldHaveSingleItem()) };

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/import", UriKind.Relative), TestContext.Current.CancellationToken);
        using var secondResponse = await client.GetAsync(new Uri("/import", UriKind.Relative), TestContext.Current.CancellationToken);
        using var thirdResponse = await client.GetAsync(new Uri("/import", UriKind.Relative), TestContext.Current.CancellationToken);
        using var fourthResponse = await client.GetAsync(new Uri("/import", UriKind.Relative), TestContext.Current.CancellationToken);
        using var fifthResponse = await client.GetAsync(new Uri("/import", UriKind.Relative), TestContext.Current.CancellationToken);
        using var limitedResponse = await client.GetAsync(new Uri("/import", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        thirdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        fourthResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        fifthResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
