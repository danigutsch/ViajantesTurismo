namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

internal static class TourDetailsPageTestsHelper
{
    public static void SetupSuccessfulTourLoad(FakeToursApiClient fakeToursApi, GetTourDto tour)
    {
        fakeToursApi.AddTour(tour);
    }
}
