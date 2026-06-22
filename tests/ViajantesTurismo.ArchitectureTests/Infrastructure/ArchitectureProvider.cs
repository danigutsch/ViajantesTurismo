using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Infrastructure;
using SharedKernel.Results;
using Assembly = System.Reflection.Assembly;

namespace ViajantesTurismo.ArchitectureTests.Infrastructure;

internal static class ArchitectureProvider
{
    private static readonly Lazy<Architecture> LazyArchitecture = new(BuildArchitecture);

    internal static Architecture Architecture => LazyArchitecture.Value;

    internal static IReadOnlyCollection<Assembly> Assemblies { get; } =
    [
        typeof(Tour).Assembly,
        typeof(IUnitOfWork).Assembly,
        typeof(InfrastructureDependencyInjection).Assembly,
        ApiMarker.Assembly,
        typeof(Result).Assembly,
        typeof(UpdateTourDto).Assembly,
        CatalogApiMarker.Assembly,
        CatalogApplicationMarker.Assembly,
        CatalogContractsMarker.Assembly,
        CatalogDomainMarker.Assembly,
        CatalogInfrastructureMarker.Assembly
    ];

    private static Architecture BuildArchitecture()
    {
        var loader = new ArchLoader();
        loader.LoadAssemblies([.. Assemblies]);
        return loader.Build();
    }

    internal static class Namespaces
    {
        internal const string Domain = "ViajantesTurismo.Admin.Domain";
        internal const string Application = "ViajantesTurismo.Admin.Application";
        internal const string Infrastructure = "ViajantesTurismo.Admin.Infrastructure";
        internal const string Api = "ViajantesTurismo.Admin.ApiService";
        internal const string Contracts = "ViajantesTurismo.Admin.Contracts";
        internal const string Common = "ViajantesTurismo.Common";

        internal const string CatalogDomain = "ViajantesTurismo.Catalog.Domain";
        internal const string CatalogApplication = "ViajantesTurismo.Catalog.Application";
        internal const string CatalogInfrastructure = "ViajantesTurismo.Catalog.Infrastructure";
        internal const string CatalogApi = "ViajantesTurismo.Catalog.ApiService";
        internal const string CatalogContracts = "ViajantesTurismo.Catalog.Contracts";
    }
}
