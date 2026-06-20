using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiService;

internal static class ErrorDocumentationCatalog
{
    private static readonly Lazy<GetErrorDocumentationDto[]> EntriesCache = new(CreateEntries);

    public static IReadOnlyList<GetErrorDocumentationDto> GetEntries()
    {
        return EntriesCache.Value;
    }

    private static GetErrorDocumentationDto[] CreateEntries()
    {
        var entries = new Dictionary<string, GetErrorDocumentationDto>(StringComparer.Ordinal);

        AddAssemblyEntries(typeof(TourErrors).Assembly, entries);
        AddAssemblyEntries(typeof(IQueryService).Assembly, entries);

        return entries.Values.OrderBy(static entry => entry.Identifier, StringComparer.Ordinal).ToArray();
    }

    private static void AddAssemblyEntries(Assembly assembly, Dictionary<string, GetErrorDocumentationDto> entries)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var attribute in assembly.GetCustomAttributes<ResultErrorCatalogProviderAttribute>())
        {
            foreach (var entry in GetProviderEntries(attribute.ProviderType))
            {
                var dto = new GetErrorDocumentationDto
                {
                    Identifier = entry.Identifier,
                    DocumentationPath = entry.DocumentationPath,
                    ProviderType = entry.ProviderType,
                    MemberName = entry.MemberName,
                    Status = entry.Status.ToString(),
                    HttpStatusCode = entry.HttpStatusCode,
                    Code = entry.Code,
                    DetailTemplate = entry.DetailTemplate,
                    Summary = entry.Summary,
                };

                if (!entries.TryAdd(entry.Identifier, dto))
                {
                    throw new InvalidOperationException(
                        $"Duplicate error documentation identifier '{entry.Identifier}' was discovered while aggregating generated catalogs.");
                }
            }
        }
    }

    private static IEnumerable<ResultErrorCatalogEntry> GetProviderEntries(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicProperties)] Type providerType)
    {
        ArgumentNullException.ThrowIfNull(providerType);

        var entriesProperty = providerType.GetProperty("Entries", BindingFlags.Instance | BindingFlags.Public);
        return entriesProperty?.GetValue(Activator.CreateInstance(providerType)) is IEnumerable<ResultErrorCatalogEntry> providerEntries
            ? providerEntries
            : [];
    }
}
