using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Fixtures;

public interface IAspireSystemTestFixture
{
    HttpClient ApiClient { get; }

    Uri ApiBaseUri { get; }

    Uri WebAppUrl { get; }

    Uri PublicWebAppUrl { get; }

    ICatalogToursApiClient CatalogTours { get; }
}
