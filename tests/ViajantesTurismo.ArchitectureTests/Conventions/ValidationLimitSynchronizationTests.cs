using System.Reflection;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Catalog.Domain;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;
using CatalogContractConstants = ViajantesTurismo.Catalog.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class ValidationLimitSynchronizationTests
{
    public static TheoryData<Type, Type> LimitPairs => new()
    {
        { typeof(AdminDomainLimits), typeof(AdminContractConstants) },
        { typeof(CatalogDomainLimits), typeof(CatalogContractConstants) },
    };

    [Theory]
    [MemberData(nameof(LimitPairs))]
    public void Contract_constants_match_the_domain_limits_they_mirror(Type domainLimits, Type contractConstants)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(domainLimits);
        ArgumentNullException.ThrowIfNull(contractConstants);
        const BindingFlags constants = BindingFlags.Public | BindingFlags.Static;
        var contractValues = contractConstants.GetFields(constants)
            .Where(field => field.IsLiteral)
            .ToDictionary(field => field.Name, field => field.GetRawConstantValue(), StringComparer.Ordinal);

        // Act
        var shared = domainLimits.GetFields(constants)
            .Where(field => field.IsLiteral && contractValues.ContainsKey(field.Name))
            .ToArray();
        var mismatches = shared
            .Where(field => !Equals(field.GetRawConstantValue(), contractValues[field.Name]))
            .Select(field => $"{field.Name}: domain {field.GetRawConstantValue()}, contract {contractValues[field.Name]}")
            .ToArray();

        // Assert
        shared.ShouldNotBeEmpty();
        mismatches.ShouldBeEmpty();
    }
}
