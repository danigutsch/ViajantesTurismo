namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class UpdateTourTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private static DateTime UtcDate(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Can_Update_Tour()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);

        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: $"{tour.Identifier}-UPDATED", name: "Cuba Updated", startDate: UtcDate(2026, 11, 10),
            endDate: UtcDate(2026, 11, 20), currency: CurrencyDto.Real, basePrice: 2800.00m, singleRoomSupplement: 370.00m, regularBikePrice: 180.00m, eBikePrice: 280.00m,
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
    public async Task Update_Tour_Returns_Not_Found_For_Missing_Id()
    {
        // Arrange
        var missingTourId = Guid.NewGuid();

        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: "INVALID", name: "Invalid Tour", startDate: UtcDate(2027, 1, 1),
            endDate: UtcDate(2027, 1, 10), currency: CurrencyDto.Real, basePrice: 1000.00m, singleRoomSupplement: 100.00m, regularBikePrice: 50.00m, eBikePrice: 80.00m,
            includedServices: ["None"]);

        // Act
        var response = await Client.PutAsJsonAsync($"/tours/{missingTourId}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains(missingTourId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("was not found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_Tour_Returns_Conflict_For_Duplicate_Identifier()
    {
        // Arrange
        var originalTour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var tourToUpdate = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);

        var updateRequest = DtoBuilders.BuildUpdateTourDto(
            identifier: originalTour.Identifier,
            name: "Updated Duplicate Identifier Tour",
            startDate: tourToUpdate.StartDate,
            endDate: tourToUpdate.EndDate,
            currency: tourToUpdate.Currency,
            basePrice: tourToUpdate.Price,
            singleRoomSupplement: tourToUpdate.SingleRoomSupplementPrice,
            regularBikePrice: tourToUpdate.RegularBikePrice,
            eBikePrice: tourToUpdate.EBikePrice,
            includedServices: [.. tourToUpdate.IncludedServices]);

        // Act
        var response = await Client.PutAsJsonAsync($"/tours/{tourToUpdate.Id}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("already exists", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Can_Update_Tour_Multiple_Times_With_Same_Data()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var updatedIdentifier = TestDataGenerator.UniqueTourIdentifier("UPDATED");
        var updateRequest = DtoBuilders.BuildUpdateTourDto(
            identifier: updatedIdentifier,
            name: "Updated Tour Name",
            startDate: UtcDate(2026, 6, 1),
            endDate: UtcDate(2026, 6, 15),
            currency: CurrencyDto.Euro,
            basePrice: 3000.00m,
            singleRoomSupplement: 400.00m,
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

    [Fact]
    public async Task Can_Update_Tour_That_Has_Bookings()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("John", "Doe", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateRequest = DtoBuilders.BuildUpdateTourDto(
            identifier: tour.Identifier,
            name: "Updated Tour with Bookings",
            startDate: tour.StartDate,
            endDate: tour.EndDate,
            currency: tour.Currency,
            basePrice: 3500.00m,
            singleRoomSupplement: 450.00m,
            regularBikePrice: 200.00m,
            eBikePrice: 300.00m,
            includedServices: ["Hotel", "Breakfast", "Lunch"]);

        // Act
        var updateResponse = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getTourResponse = await Client.GetAsync(new Uri($"/tours/{tour.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        var updatedTour = await getTourResponse.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTour);
        Assert.Equal(updateRequest.Name, updatedTour.Name);
        Assert.Equal(updateRequest.Price, updatedTour.Price);

        var getBookingResponse = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        var updatedBooking = await getBookingResponse.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedBooking);
        Assert.Equal(tour.Id, updatedBooking.TourId);
        Assert.Equal(customer.Id, updatedBooking.CustomerId);
    }
}
