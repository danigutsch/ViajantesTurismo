namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class AdminApiIntegrationTestSet : ICollectionFixture<ApiFixture>
{
    public const string Name = "AdminApiIntegrationTest";
}
