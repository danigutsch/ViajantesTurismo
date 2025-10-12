using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.IntegrationTests;

[Collection("Api collection")]
public sealed class ToursApiTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly ApiFixture _fixture;
    private readonly IServiceScope _scope;

    public ToursApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();

        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = new Seeder(dbContext);
        seeder.Seed();

        _scope = _fixture.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _client.Dispose();
        _dbContext.Dispose();
        _scope.Dispose();
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
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new CreateTourDto
        {
            Identifier = $"CUBA-{uniqueId}",
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

        var tour = _dbContext.Tours.FirstOrDefault(t => t.Identifier == request.Identifier);
        Assert.NotNull(tour);
    }

    [Fact]
    public async Task Can_Get_Tour_By_Id()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new CreateTourDto
        {
            Identifier = $"CUBA-{uniqueId}",
            Name = "Isla de Cuba",
            StartDate = new DateTime(2025, 10, 10).ToUniversalTime(),
            EndDate = new DateTime(2025, 10, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2600.00m,
            SingleRoomSupplementPrice = 350.00m,
            RegularBikePrice = 160.00m,
            EBikePrice = 260.00m,
            IncludedServices = ["Hotel", "Breakfast", "City Tour"]
        };
        var createResponse = await _client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdTour = _dbContext.Tours.FirstOrDefault(tour => tour.Identifier == request.Identifier);
        Assert.NotNull(createdTour);

        // Act
        var response = await _client.GetAsync(new Uri($"/tours/{createdTour.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tourDto = await response.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tourDto);
        Assert.Equal(request.Identifier, tourDto.Identifier);
        Assert.Equal(request.Name, tourDto.Name);
        Assert.Equal(request.Price, tourDto.Price);
        Assert.Equal(request.Currency, tourDto.Currency);
        Assert.Equal(request.IncludedServices, tourDto.IncludedServices);
    }

    [Fact]
    public async Task Get_Tour_By_Id_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        // Act
        var response = await _client.GetAsync(new Uri($"/tours/{invalidId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Update_Tour()
    {
        // Arrange: create a tour to update
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new CreateTourDto
        {
            Identifier = $"CUBA-{uniqueId}",
            Name = "Isla de Cuba",
            StartDate = new DateTime(2026, 10, 10).ToUniversalTime(),
            EndDate = new DateTime(2026, 10, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2700.00m,
            SingleRoomSupplementPrice = 360.00m,
            RegularBikePrice = 170.00m,
            EBikePrice = 270.00m,
            IncludedServices = ["Hotel", "Breakfast", "City Tour"]
        };
        var postResponse = await _client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var location = postResponse.Headers.Location;
        Assert.NotNull(location);
        var idStr = location.OriginalString.Split('/').Last();
        Assert.True(int.TryParse(idStr, out var tourId));

        var updateRequest = new UpdateTourDto
        {
            Identifier = $"CUBA-{uniqueId}-UPDATED",
            Name = "Cuba Updated",
            StartDate = new DateTime(2026, 11, 10).ToUniversalTime(),
            EndDate = new DateTime(2026, 11, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2800.00m,
            SingleRoomSupplementPrice = 370.00m,
            RegularBikePrice = 180.00m,
            EBikePrice = 280.00m,
            IncludedServices = ["Hotel", "Breakfast", "City Tour", "Dinner"]
        };

        // Act
        var putResponse = await _client.PutAsJsonAsync($"/tours/{tourId}", updateRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // Validate update via GET
        var getResponse = await _client.GetAsync(new Uri($"/tours/{tourId}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var tourDto = await getResponse.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tourDto);
        Assert.Equal(updateRequest.Identifier, tourDto.Identifier);
        Assert.Equal(updateRequest.Name, tourDto.Name);
        Assert.Equal(updateRequest.Price, tourDto.Price);
        Assert.Equal(updateRequest.Currency, tourDto.Currency);
        Assert.Equal(updateRequest.IncludedServices, tourDto.IncludedServices);
    }

    [Fact]
    public async Task Update_Tour_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        var updateRequest = new UpdateTourDto
        {
            Identifier = "INVALID",
            Name = "Invalid Tour",
            StartDate = new DateTime(2027, 1, 1).ToUniversalTime(),
            EndDate = new DateTime(2027, 1, 10).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 1000.00m,
            SingleRoomSupplementPrice = 100.00m,
            RegularBikePrice = 50.00m,
            EBikePrice = 80.00m,
            IncludedServices = ["None"]
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/tours/-1", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
