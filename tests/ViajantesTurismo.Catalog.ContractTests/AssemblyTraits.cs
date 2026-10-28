using TestTraits = ViajantesTurismo.Catalog.ContractTests.Infrastructure.TestTraits;
using PandaBear.Testing;


[assembly: TestScope(TestScope.Contract)]
[assembly: Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
