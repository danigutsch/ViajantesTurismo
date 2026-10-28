using PandaBear.Testing;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

[assembly: TestScope(TestScope.Integration)]
[assembly: Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.BrandingArea)]
