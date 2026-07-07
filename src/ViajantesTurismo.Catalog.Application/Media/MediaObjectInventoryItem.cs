namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes a stored media object during reconciliation.
/// </summary>
/// <param name="ObjectKey">The application-owned object key.</param>
/// <param name="LastModifiedAt">The last observed object modification time.</param>
public sealed record MediaObjectInventoryItem(string ObjectKey, DateTimeOffset LastModifiedAt);
