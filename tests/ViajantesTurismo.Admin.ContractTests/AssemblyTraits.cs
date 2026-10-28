using TestTraits = ViajantesTurismo.Admin.ContractTests.Infrastructure.TestTraits;
using PandaBear.Testing;


[assembly: TestScope(TestScope.Contract)]
[assembly: Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AdminArea)]
