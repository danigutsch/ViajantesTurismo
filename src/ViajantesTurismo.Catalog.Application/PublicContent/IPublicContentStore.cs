using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Application.PublicContent;

/// <summary>
/// Stores editable public content for Catalog management flows.
/// </summary>
public interface IPublicContentStore
{
    /// <summary>
    /// Lists all editable public content entries.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The stored content entries.</returns>
    Task<IReadOnlyCollection<EditablePublicContent>> ListContent(CancellationToken ct);

    /// <summary>
    /// Gets an editable public content entry by key.
    /// </summary>
    /// <param name="key">The stable content key.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The content entry, or <see langword="null" /> when missing.</returns>
    Task<EditablePublicContent?> GetContent(string key, CancellationToken ct);

    /// <summary>
    /// Saves an editable public content entry.
    /// </summary>
    /// <param name="content">The content entry to save.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that completes when the entry has been saved.</returns>
    Task SaveContent(EditablePublicContent content, CancellationToken ct);
}
