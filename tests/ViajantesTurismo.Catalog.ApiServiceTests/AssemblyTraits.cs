using PandaBear.Testing;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

[assembly: TestScope(TestScope.Integration)]
[assembly: Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
