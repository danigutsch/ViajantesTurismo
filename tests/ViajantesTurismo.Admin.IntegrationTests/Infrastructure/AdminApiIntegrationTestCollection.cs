namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class AdminApiIntegrationTestSet : ICollectionFixture<ApiFixture>
{
    public const string Name = "AdminApiIntegrationTestSet";

    public AdminApiIntegrationTestSet(ApiFixture _)
    {
    }
}

