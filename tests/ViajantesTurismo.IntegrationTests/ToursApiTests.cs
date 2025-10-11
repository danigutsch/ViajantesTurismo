using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Tools;

namespace ViajantesTurismo.IntegrationTests;

[Collection("Api collection")]
public sealed class ToursApiTests
{
    private readonly HttpClient _client;
    private readonly ApiFixture _fixture;

    public ToursApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();

        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = new AdminContextSeeder(dbContext);
        seeder.Seed();
    }

    [Fact]
    public async Task Can_Get_Tours()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tours = await response.Content.ReadFromJsonAsync<GetTourDto[]>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tours);
        Assert.NotEmpty(tours);
    }

    [Fact]
    public async Task Can_Create_Tour()
    {
        // Arrange
        var request = new CreateTourDto()
        {
            Identifier = "CUBA2024",
            Name = "Isla de Cuba",
            StartDate = new DateTime(2024, 10, 10).ToUniversalTime(),
            EndDate = new DateTime(2024, 10, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2500.00m,
            SingleRoomSupplementPrice = 300.00m,
            RegularBikePrice = 150.00m,
            EBikePrice = 250.00m,
            IncludedServices = ["Hotel", "Breakfast", "City Tour"]
        };

        // Act
        var response = await _client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
