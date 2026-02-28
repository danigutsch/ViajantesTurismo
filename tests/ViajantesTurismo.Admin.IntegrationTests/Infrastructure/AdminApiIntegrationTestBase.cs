using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

[assembly: AssemblyFixture(typeof(ApiFixture))]

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public abstract class AdminApiIntegrationTestBase(ApiFixture fixture) : IntegrationTestBase<ApiMarker>(fixture);
