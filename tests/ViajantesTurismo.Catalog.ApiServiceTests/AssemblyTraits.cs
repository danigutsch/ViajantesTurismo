using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

[assembly: Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ApiIntegrationScope)]
[assembly: Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
