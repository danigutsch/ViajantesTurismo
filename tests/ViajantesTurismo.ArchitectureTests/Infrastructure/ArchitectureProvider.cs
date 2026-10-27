using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Infrastructure;
using PandaBear.Results;
using AdminInfrastructure = ViajantesTurismo.Admin.Infrastructure;
using Assembly = System.Reflection.Assembly;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.ArchitectureTests.Infrastructure;

/// <summary>Provides the lazily built ArchUnitNET architecture and the assemblies it loads.</summary>
internal static class ArchitectureProvider
{
    private static readonly Lazy<Architecture> LazyArchitecture = new(BuildArchitecture);

    /// <summary>The architecture built from <see cref="Assemblies"/> on first use.</summary>
    internal static Architecture Architecture => LazyArchitecture.Value;

    /// <summary>The Admin and Catalog assemblies, plus the results assembly, that the architecture tests load.</summary>
    internal static IReadOnlyCollection<Assembly> Assemblies
    {
        get;
    } =
    [
        typeof(Tour).Assembly,
        typeof(IUnitOfWork).Assembly,
        typeof(AdminInfrastructure.InfrastructureDependencyInjection).Assembly,
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

    /// <summary>Root namespaces of the Admin and Catalog layers.</summary>
    internal static class Namespaces
    {
        /// <summary>Root namespace of the Admin domain layer.</summary>
        internal const string Domain = "ViajantesTurismo.Admin.Domain";
        /// <summary>Root namespace of the Admin application layer.</summary>
        internal const string Application = "ViajantesTurismo.Admin.Application";
        /// <summary>Root namespace of the Admin infrastructure layer.</summary>
        internal const string Infrastructure = "ViajantesTurismo.Admin.Infrastructure";
        /// <summary>Root namespace of the Admin API service.</summary>
        internal const string Api = "ViajantesTurismo.Admin.ApiService";
        /// <summary>Root namespace of the Admin contracts.</summary>
        internal const string Contracts = "ViajantesTurismo.Admin.Contracts";
        /// <summary>Root namespace of the Catalog domain layer.</summary>
        internal const string CatalogDomain = "ViajantesTurismo.Catalog.Domain";
        /// <summary>Root namespace of the Catalog application layer.</summary>
        internal const string CatalogApplication = "ViajantesTurismo.Catalog.Application";
        /// <summary>Root namespace of the Catalog infrastructure layer.</summary>
        internal const string CatalogInfrastructure = "ViajantesTurismo.Catalog.Infrastructure";
        /// <summary>Root namespace of the Catalog API service.</summary>
        internal const string CatalogApi = "ViajantesTurismo.Catalog.ApiService";
        /// <summary>Root namespace of the Catalog contracts.</summary>
        internal const string CatalogContracts = "ViajantesTurismo.Catalog.Contracts";
    }
}
