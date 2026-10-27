namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

/// <summary>Test data helpers for the tour edit page tests.</summary>
internal static class EditPageTestsHelper
{
    /// <summary>Creates a tour through the fake API and returns it as stored.</summary>
    public static async Task<GetTourDto> CreateTestTour(FakeToursApiClient fakeToursApi)
    {
        var createDto = new CreateTourDto
        {
            Identifier = "CUBA2024",
            Name = "Cuba Adventure",
            StartDate = DateOnly.FromDateTime(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified)),
            EndDate = DateOnly.FromDateTime(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified)),
            Currency = CurrencyDto.Euro,
            Price = 1500.00m,
            SingleRoomSupplementPrice = 200.00m,
            RegularBikePrice = 50.00m,
            EBikePrice = 100.00m,
            IncludedServices = ["Hotel", "Breakfast", "Lunch"],
            MinCustomers = 5,
            MaxCustomers = 15
        };

        await fakeToursApi.CreateTour(createDto, CancellationToken.None);
        var outcome = await fakeToursApi.GetTours(CancellationToken.None);
        var tours = outcome.Value ?? throw new InvalidOperationException("The fake tours client did not return created tours.");
        return tours[0];
    }

    /// <summary>Adds a tour with three current customers to the fake API and returns it.</summary>
    public static GetTourDto CreateTestTourWithBookings(FakeToursApiClient fakeToursApi)
    {
        var tour = BuildTourDto(
            identifier: "CUBA2024",
            name: "Cuba Adventure",
            startDate: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            endDate: new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified),
            currency: CurrencyDto.Euro,
            price: 1500.00m,
            singleRoomSupplementPrice: 200.00m,
            regularBikePrice: 50.00m,
            eBikePrice: 100.00m,
            includedServices: ["Hotel", "Breakfast", "Lunch"],
            minCustomers: 5,
            maxCustomers: 15,
            currentCustomerCount: 3);

        fakeToursApi.AddTour(tour);
        return tour;
    }
}
