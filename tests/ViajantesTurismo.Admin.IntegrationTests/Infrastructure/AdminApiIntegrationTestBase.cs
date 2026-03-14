using ViajantesTurismo.Admin.ApiService;

[assembly: AssemblyFixture(typeof(ApiFixture))]

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public abstract class AdminApiIntegrationTestBase(ApiFixture fixture) : IntegrationTestBase<ApiMarker>(fixture);
