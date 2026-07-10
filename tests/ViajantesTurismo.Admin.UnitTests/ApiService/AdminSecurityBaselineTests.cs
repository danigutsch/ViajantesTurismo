using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Testing.Assertions;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class AdminSecurityBaselineTests
{
    [Fact]
    public void Configures_admin_cors_policy_with_allowed_origins()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Cors:AllowedOrigins:0"] = "https://admin.example.com"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddAdminSecurityBaseline(configuration);
        using var provider = services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        // Assert
        var policy = corsOptions.GetPolicy(AdminSecurityBaseline.CorsPolicyName).ShouldNotBeNull();
        policy.Origins.ShouldContain("https://admin.example.com");
    }

    [Fact]
    public async Task Configures_admin_import_rate_limit_policy()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
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
