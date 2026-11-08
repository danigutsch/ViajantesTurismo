using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class TourCurrencyEndpointTests
{
    [Theory]
    [InlineData("\"JPY\"")]
    [InlineData("\"GBP\"")]
    [InlineData("\"Euro\"")]
    [InlineData("\"UsDollar\"")]
    [InlineData("\"EU\"")]
    [InlineData("\"EURO\"")]
    [InlineData("\"E1R\"")]
    [InlineData("\"\"")]
    public async Task Creating_a_tour_with_an_unsupported_or_malformed_currency_code_is_a_currency_validation_error(string currencyJson)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ITourStore, FakeTourStore>());
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var content = TourRequestJson.WithCurrency(currencyJson);

        // Act
        using var response = await client.PostAsync(new Uri("/api/v1/tours/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("urrency", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("null")]
    public async Task Creating_a_tour_with_a_currency_that_is_not_a_string_is_rejected(string currencyJson)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ITourStore, FakeTourStore>());
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var content = TourRequestJson.WithCurrency(currencyJson);

        // Act
        using var response = await client.PostAsync(new Uri("/api/v1/tours/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
