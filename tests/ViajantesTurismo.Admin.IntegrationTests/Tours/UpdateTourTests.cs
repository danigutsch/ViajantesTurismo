using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class UpdateTourTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Tour()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);

        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: $"{tour.Identifier}-UPDATED", name: "Cuba Updated", startDate: new DateTime(2026, 11, 10).ToUniversalTime(),
            endDate: new DateTime(2026, 11, 20).ToUniversalTime(), currency: CurrencyDto.Real, basePrice: 2800.00m, doubleRoomSupplement: 370.00m, regularBikePrice: 180.00m, eBikePrice: 280.00m,
            includedServices: ["Hotel", "Breakfast", "City Tour", "Dinner"]);

        // Act
        var putResponse = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // Assert
        var getResponse = await Client.GetAsync(new Uri($"/tours/{tour.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
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
    public async Task Update_Tour_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: "INVALID", name: "Invalid Tour", startDate: new DateTime(2027, 1, 1).ToUniversalTime(),
            endDate: new DateTime(2027, 1, 10).ToUniversalTime(), currency: CurrencyDto.Real, basePrice: 1000.00m, doubleRoomSupplement: 100.00m, regularBikePrice: 50.00m, eBikePrice: 80.00m,
            includedServices: ["None"]);

        // Act
        var response = await Client.PutAsJsonAsync($"/tours/{invalidId}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Tour_With_Empty_Name()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: tour.Identifier, name: "Updated Tour");
        updateRequest = updateRequest with { Name = "" };

        // Act
        var response = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Tour_With_Invalid_Date_Range()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var startDate = new DateTime(2026, 11, 10).ToUniversalTime();
        var endDate = startDate.AddDays(2);
        var updateRequest = DtoBuilders.BuildUpdateTourDto(
            identifier: tour.Identifier,
            name: "Updated Tour",
            startDate: startDate,
            endDate: endDate);

        // Act
        var response = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Tour_With_Negative_Price()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: tour.Identifier, name: "Updated Tour");
        updateRequest = updateRequest with { Price = -100.00m };

        // Act
        var response = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Can_Update_Tour_Multiple_Times_With_Same_Data()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var updateRequest = DtoBuilders.BuildUpdateTourDto(
            identifier: "UPDATED001",
            name: "Updated Tour Name",
            startDate: new DateTime(2026, 6, 1).ToUniversalTime(),
            endDate: new DateTime(2026, 6, 15).ToUniversalTime(),
            currency: CurrencyDto.Euro,
            basePrice: 3000.00m,
            doubleRoomSupplement: 400.00m,
            regularBikePrice: 200.00m,
            eBikePrice: 300.00m,
            includedServices: ["Hotel", "Breakfast"]);

        // Act
        var firstResponse = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);
        var secondResponse = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);

        var getResponse = await Client.GetAsync(new Uri($"/tours/{tour.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        var tourDto = await getResponse.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tourDto);
        Assert.Equal(updateRequest.Name, tourDto.Name);
        Assert.Equal(updateRequest.Price, tourDto.Price);
    }
}
