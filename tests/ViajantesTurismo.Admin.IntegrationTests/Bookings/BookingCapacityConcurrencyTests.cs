namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
public sealed class BookingCapacityConcurrencyTests(ApiFixture fixture)
{
    [Fact]
    public async Task Concurrent_confirmations_for_the_last_place_return_one_conflict()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var createTourRequest = DtoBuilders.BuildCreateTourDto(minCustomers: 1, maxCustomers: 1);
        using var createTourResponse = await fixture.Client.CreateTour(createTourRequest, ct);
        createTourResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tourLocation = createTourResponse.Headers.Location.ShouldNotBeNull();
        using var getCreatedTourResponse = await fixture.Client.GetAsync(tourLocation, ct);
        var tour = await getCreatedTourResponse.Content.ReadFromJsonAsync<GetTourDto>(ct);
        tour.ShouldNotBeNull();

        var firstCustomer = await fixture.Client.CreateTestCustomer("Capacity", "First", ct);
        var secondCustomer = await fixture.Client.CreateTestCustomer("Capacity", "Second", ct);
        var firstBooking = await fixture.Client.CreateTestBooking(tour.Id, firstCustomer.Id, null, ct);
        var secondBooking = await fixture.Client.CreateTestBooking(tour.Id, secondCustomer.Id, null, ct);
        await using var scenario = await fixture.CreateBookingCapacityConcurrencyScenario(
            firstBooking.Id,
            secondBooking.Id,
            ct);
        await scenario.HoldBookingWrites(ct);

        // Act
        var firstConfirmation = fixture.Client.ConfirmBooking(firstBooking.Id, ct);
        var secondConfirmation = fixture.Client.ConfirmBooking(secondBooking.Id, ct);
        await scenario.WaitForConcurrentRequests(ct);
        await scenario.ReleaseBookingWrites(ct);
        using var firstResponse = await firstConfirmation;
        using var secondResponse = await secondConfirmation;
        using var getTourResponse = await fixture.Client.GetAsync(
            new Uri($"/api/v1/tours/{tour.Id}", UriKind.Relative),
            ct);
        var persistedTour = await getTourResponse.Content.ReadFromJsonAsync<GetTourDto>(ct);

        // Assert
        HttpStatusCode[] statuses = [firstResponse.StatusCode, secondResponse.StatusCode];
        statuses.ShouldContain(HttpStatusCode.OK);
        statuses.ShouldContain(HttpStatusCode.Conflict);
        persistedTour.ShouldNotBeNull();
        persistedTour.CurrentCustomerCount.ShouldBe(1);
    }

    [Fact]
    public async Task Confirmation_in_progress_prevents_concurrent_deletion_of_the_confirmed_booking()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var createTourRequest = DtoBuilders.BuildCreateTourDto(minCustomers: 1, maxCustomers: 2);
        using var createTourResponse = await fixture.Client.CreateTour(createTourRequest, ct);
        createTourResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tourLocation = createTourResponse.Headers.Location.ShouldNotBeNull();
        using var getCreatedTourResponse = await fixture.Client.GetAsync(tourLocation, ct);
        var tour = await getCreatedTourResponse.Content.ReadFromJsonAsync<GetTourDto>(ct);
        tour.ShouldNotBeNull();

        var firstCustomer = await fixture.Client.CreateTestCustomer("Delete race", "First", ct);
        var secondCustomer = await fixture.Client.CreateTestCustomer("Delete race", "Second", ct);
        var targetBooking = await fixture.Client.CreateTestBooking(tour.Id, firstCustomer.Id, null, ct);
        var barrierBooking = await fixture.Client.CreateTestBooking(tour.Id, secondCustomer.Id, null, ct);
        await using var scenario = await fixture.CreateBookingCapacityConcurrencyScenario(
            targetBooking.Id,
            barrierBooking.Id,
            ct);
        await scenario.HoldBookingWrites(ct);

        // Act
        var confirmation = fixture.Client.ConfirmBooking(targetBooking.Id, ct);
        await scenario.WaitForBookingWrite(ct);
        var deletion = fixture.Client.DeleteBooking(targetBooking.Id, ct);
        await scenario.WaitForConcurrentRequests(ct);
        await scenario.ReleaseBookingWrites(ct);
        using var confirmationResponse = await confirmation;
        using var deletionResponse = await deletion;
        var persistedBooking = await fixture.Client.GetBookingAndRead(targetBooking.Id, ct);

        // Assert
        confirmationResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        deletionResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        persistedBooking.Status.ShouldBe(BookingStatusDto.Confirmed);
    }
}
