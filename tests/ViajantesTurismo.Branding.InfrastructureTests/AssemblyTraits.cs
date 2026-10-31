using PandaBear.Testing;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

[assembly: TestScope(TestScope.Integration)]
[assembly: Trait(TestTraitNames.AreaName, TestTraits.BrandingArea)]
