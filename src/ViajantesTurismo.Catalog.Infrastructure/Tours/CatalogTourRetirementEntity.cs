namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>The durable public-withdrawal marker for a retired source tour.</summary>
internal sealed class CatalogTourRetirementEntity
{
    /// <summary>The Catalog retirement table name.</summary>
    internal const string TableName = "CatalogTourRetirements";

    /// <summary>The source tour whose public visibility is withdrawn.</summary>
    public Guid AdminTourId
    {
        get; init;
    }

    /// <summary>The Admin retirement operation identifier.</summary>
    public Guid RetirementId
    {
        get; init;
    }

    /// <summary>The UTC instant when Catalog recorded the retirement.</summary>
    public DateTimeOffset WithdrawnAt
    {
        get; init;
    }

}
