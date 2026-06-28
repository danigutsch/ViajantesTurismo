using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class TestPublicContentStore : IPublicContentStore
{
    private readonly ConcurrentDictionary<string, EditablePublicContent> contentByKey = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyCollection<EditablePublicContent>> ListContent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<EditablePublicContent>>(contentByKey.Values.ToArray());
    }

    public Task<EditablePublicContent?> GetContent(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var sanitizedKey = StringSanitizer.Sanitize(key).ToUpperInvariant();
        contentByKey.TryGetValue(sanitizedKey, out var content);

        return Task.FromResult(content);
    }

    public Task SaveContent(EditablePublicContent content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        ct.ThrowIfCancellationRequested();

        contentByKey[content.Key] = content;

        return Task.CompletedTask;
    }
}
