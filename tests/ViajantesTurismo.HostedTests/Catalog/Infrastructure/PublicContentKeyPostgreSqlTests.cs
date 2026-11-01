using Microsoft.EntityFrameworkCore;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure;

/// <summary>Protects typed content identity against the actual PostgreSQL schema and converter.</summary>
[TestScope(TestScope.Integration)]
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class PublicContentKeyPostgreSqlTests(HostedTestFixture shared)
{
    [Theory]
    [InlineData(" Home.Hero ", "HOME.HERO")]
    [InlineData("home.i\u0307", "HOME.I\u0307")]
    public async Task Keys_round_trip_through_the_existing_column_and_match_canonical_lookups(string input, string expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dbContext = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        dbContext.Database.SetConnectionString(database.ConnectionString);
        await dbContext.Database.MigrateAsync(ct);
        var enUs = PublicContentVariant.Create(PublicContentLanguage.EnUs, "Welcome", "Tour information", null, null, null, false).Value;
        var ptBr = PublicContentVariant.Create(PublicContentLanguage.PtBr, "Welcome", "Tour information", null, null, null, false).Value;
        var content = EditablePublicContent.Create(input, PublicContentLanguage.EnUs, [enUs, ptBr]).Value;
        var store = new EfPublicContentStore(dbContext);
        await store.SaveContent(content, ct);
        dbContext.ChangeTracker.Clear();

        // Act
        var loaded = await store.GetContent(input, ct);
        var storedKey = await dbContext.Database.SqlQueryRaw<string>("SELECT \"Key\" AS \"Value\" FROM \"PublicContent\"").SingleAsync(ct);

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(content.Id);
        loaded.ContentKey.ShouldBe(content.ContentKey);
        loaded.Key.ShouldBe(expected);
        storedKey.ShouldBe(expected);
    }
}
