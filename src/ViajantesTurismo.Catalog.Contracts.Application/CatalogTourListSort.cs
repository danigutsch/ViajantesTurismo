namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Fields supported by the Management tour list ordering.</summary>
public enum CatalogTourListSort
{
    /// <summary>Sort by public title.</summary>
    Title,

    /// <summary>Sort by visible identifier.</summary>
    Identifier,

    /// <summary>Sort by public slug.</summary>
    Slug,

    /// <summary>Sort by publication status.</summary>
    IsPublished,
}
