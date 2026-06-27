using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class EfPublicContentStore(CatalogDbContext dbContext) : IPublicContentStore
{
    public async Task<IReadOnlyCollection<EditablePublicContent>> ListContent(CancellationToken ct)
    {
        return await dbContext.PublicContent
            .OrderBy(content => content.Key)
            .ToArrayAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<EditablePublicContent?> GetContent(string key, CancellationToken ct)
    {
        var sanitizedKey = StringSanitizer.Sanitize(key);

        return await dbContext.PublicContent
            .SingleOrDefaultAsync(content => content.Key == sanitizedKey, ct)
            .ConfigureAwait(false);
    }

    public async Task SaveContent(EditablePublicContent content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        var existing = await dbContext.PublicContent
            .SingleOrDefaultAsync(current => current.Key == content.Key, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var result = existing.ReplaceVariants(content.SourceLanguage, content.EnUs, content.PtBr);
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
