namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded page of unfinished Admin-to-Catalog delivery metadata.</summary>
public sealed record GetCatalogDeliveryPageDto
{
    /// <summary>Gets the rows in descending timestamp and storage identifier order.</summary>
    public required IReadOnlyList<CatalogDeliveryRowDto> Rows
    {
        get; init;
    }

    /// <summary>Gets the timestamp to continue from with the same filters, when another row exists.</summary>
    public required DateTimeOffset? NextRecordedAt
    {
        get; init;
    }

    /// <summary>Gets the storage identifier to send with the continuation timestamp.</summary>
    public required Guid? NextStorageId
    {
        get; init;
    }
}
