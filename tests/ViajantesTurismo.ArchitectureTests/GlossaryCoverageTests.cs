using System.Text.RegularExpressions;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.ArchitectureTests.Infrastructure;
using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.ArchitectureTests;

public sealed class GlossaryCoverageTests
{
    private static readonly Lazy<string> GlossaryText = new(LoadGlossaryText);

    [Fact]
    public void Domain_Entities_Should_Be_Present_In_Glossary()
    {
        var entities = GetDomainTypes(type => type is { IsClass: true, IsAbstract: false } && InheritsFromEntity(type));
        var missing = entities.Where(name => !IsTermDocumented(name)).ToArray();

        Assert.False(
            missing.Length > 0,
            $"Expected all domain entities to be documented in GLOSSARY.md, but missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Domain_Enums_Should_Be_Present_In_Glossary()
    {
        var enums = GetDomainTypes(type => type.IsEnum);
        var missing = enums.Where(name => !IsTermDocumented(name)).ToArray();

        Assert.False(
            missing.Length > 0,
            $"Expected all domain enums to be documented in GLOSSARY.md, but missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Shared_ValueObjects_Should_Be_Present_In_Glossary()
    {
        var valueObjects = ArchitectureProvider.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(InheritsFromValueObject)
            .Select(type => type.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var missing = valueObjects.Where(name => !IsTermDocumented(name)).ToArray();

        Assert.False(
            missing.Length > 0,
            $"Expected all value objects to be documented in GLOSSARY.md, but missing: {string.Join(", ", missing)}");
    }

    private static string[] GetDomainTypes(Func<Type, bool> predicate) =>
        typeof(Tour).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace is not null && type.Namespace.StartsWith(ArchitectureProvider.Namespaces.Domain, StringComparison.Ordinal))
            .Where(predicate)
            .Select(type => type.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static bool InheritsFromEntity(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InheritsFromValueObject(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current == typeof(ValueObject))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTermDocumented(string term)
    {
        var pattern = $@"^\|\s*{Regex.Escape(term)}\s*\|";
        return Regex.IsMatch(GlossaryText.Value, pattern, RegexOptions.Multiline);
    }

    private static string LoadGlossaryText()
    {
        var glossaryPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "domain", "GLOSSARY.md"));
        return File.ReadAllText(glossaryPath);
    }
}
