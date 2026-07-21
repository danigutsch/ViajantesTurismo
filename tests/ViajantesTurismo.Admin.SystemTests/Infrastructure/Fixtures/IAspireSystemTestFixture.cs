namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Fixtures;

public interface IAspireSystemTestFixture
{
    /// <summary>
    /// Creates an authenticated Admin API client owned by the calling test.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A client that the calling test must dispose.</returns>
    Task<HttpClient> CreateApiClient(CancellationToken ct);

    Uri WebAppUrl { get; }

    Uri PublicWebAppUrl { get; }

    string ConformanceUserPassword { get; }
}
