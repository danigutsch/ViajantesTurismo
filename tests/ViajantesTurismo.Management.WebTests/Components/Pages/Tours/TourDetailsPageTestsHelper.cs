namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

/// <summary>Setup helpers for the tour details page tests.</summary>
internal static class TourDetailsPageTestsHelper
{
    /// <summary>Registers the given tour in the fake API so the page loads it successfully.</summary>
    public static void SetupSuccessfulTourLoad(FakeToursApiClient fakeToursApi, GetTourDto tour)
    {
        fakeToursApi.AddTour(tour);
    }
}
