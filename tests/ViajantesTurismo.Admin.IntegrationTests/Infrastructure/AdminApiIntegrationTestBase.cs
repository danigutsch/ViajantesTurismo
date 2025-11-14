using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

[Collection("Admin API")]
public abstract class AdminApiIntegrationTestBase(ApiFixture fixture) : IntegrationTestBase<ApiMarker>(fixture);

[CollectionDefinition("Admin API")]
public sealed class AdminApiTests : ICollectionFixture<ApiFixture>;
