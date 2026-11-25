using TestTraits = ViajantesTurismo.Catalog.ContractTests.Infrastructure.TestTraits;
using SharedKernel.Testing;


[assembly: TestScope(TestScope.Contract)]
[assembly: Trait(TestTraitNames.AreaName, TestTraits.CatalogArea)]
