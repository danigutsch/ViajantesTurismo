using TestTraits = ViajantesTurismo.Admin.ContractTests.Infrastructure.TestTraits;
using SharedKernel.Testing;


[assembly: TestScope(TestScope.Contract)]
[assembly: Trait(TestTraitNames.AreaName, TestTraits.AdminArea)]
