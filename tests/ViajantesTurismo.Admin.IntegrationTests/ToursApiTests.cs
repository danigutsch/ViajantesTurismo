using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.IntegrationTests;

[Collection("Api collection")]
public sealed class ToursApiTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceScope _scope;

    public ToursApiTests(ApiFixture fixture)
    {
        _client = fixture.CreateClient();

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = new Seeder(dbContext);
        seeder.Seed();

        _scope = fixture.Services.CreateScope();
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
        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        // Act
        var response = await _client.GetAsync(location, TestContext.Current.CancellationToken);

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
        // Arrange
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

        // Assert
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
        const int invalidId = -1;

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
        var response = await _client.PutAsJsonAsync($"/tours/{invalidId}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Tour_Returns_BadRequest_For_Invalid_Data()
    {
        const string invalidIdentifier = "";
        var request = new CreateTourDto
        {
            Identifier = invalidIdentifier,
            Name = "Test Tour",
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7),
            Currency = CurrencyDto.Real,
            Price = 2000.00m,
            SingleRoomSupplementPrice = 500.00m,
            RegularBikePrice = 100.00m,
            EBikePrice = 200.00m,
            IncludedServices = ["Hotel", "Breakfast"]
        };

        // Act
        var response = await _client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Tour_Returns_Validation_Problem_For_Multiple_Errors()
    {
        // Arrange
        const string invalidIdentifier = "";
        const string invalidName = "";
        const decimal invalidPrice = 0.00m;
        var request = new CreateTourDto
        {
            Identifier = invalidIdentifier,
            Name = invalidName,
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7),
            Currency = CurrencyDto.Real,
            Price = invalidPrice,
            SingleRoomSupplementPrice = 500.00m,
            RegularBikePrice = 100.00m,
            EBikePrice = 200.00m,
            IncludedServices = ["Hotel"]
        };

        // Act
        var response = await _client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("identifier", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_Tour_Returns_BadRequest_For_Invalid_Price()
    {
        // Arrange
        const decimal negativeBasePrice = -100.00m;
        var request = new CreateTourDto
        {
            Identifier = "TEST2024",
            Name = "Test Tour",
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7),
            Currency = CurrencyDto.Real,
            Price = negativeBasePrice,
            SingleRoomSupplementPrice = 500.00m,
            RegularBikePrice = 100.00m,
            EBikePrice = 200.00m,
            IncludedServices = ["Hotel", "Breakfast"]
        };

        // Act
        var response = await _client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("price", content, StringComparison.OrdinalIgnoreCase);
    }
}
