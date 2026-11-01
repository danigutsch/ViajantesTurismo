using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// EF Core store for editable public content.
/// </summary>
/// <param name="dbContext">The Catalog context.</param>
internal sealed class EfPublicContentStore(CatalogDbContext dbContext) : IPublicContentStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<EditablePublicContent>> ListContent(CancellationToken ct)
    {
        return await dbContext.PublicContent
            .OrderBy(content => content.ContentKey)
            .ToArrayAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EditablePublicContent?> GetContent(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var contentKey = PublicContentKey.Create(key);
        if (contentKey.IsFailure)
        {
            return null;
        }

        return await GetContent(contentKey.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<EditablePublicContent?> GetContent(PublicContentKey key, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);

        return dbContext.PublicContent.SingleOrDefaultAsync(content => content.ContentKey == key, ct);
    }

    /// <inheritdoc />
    public async Task SaveContent(EditablePublicContent content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        var existing = await dbContext.PublicContent
            .SingleOrDefaultAsync(current => current.ContentKey == content.ContentKey, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var result = existing.ReplaceWith(content);
            if (result.IsFailure)
            {
                throw new InvalidOperationException("Stored public content replacement must be valid.");
            }

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        dbContext.PublicContent.Add(content);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
