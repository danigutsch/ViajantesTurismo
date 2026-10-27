using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using PandaBear.InputNormalization;

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
            .OrderBy(content => content.Key)
            .ToArrayAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EditablePublicContent?> GetContent(string key, CancellationToken ct)
    {
        var sanitizedKey = StringSanitizer.Sanitize(key).ToUpperInvariant();

        return await dbContext.PublicContent
            .SingleOrDefaultAsync(content => content.Key == sanitizedKey, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveContent(EditablePublicContent content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        var existing = await dbContext.PublicContent
            .SingleOrDefaultAsync(current => current.Key == content.Key, ct)
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
