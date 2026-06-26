using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// In-memory editable public content store for the current Catalog service runtime.
/// </summary>
public sealed class InMemoryPublicContentStore : IPublicContentStore
{
    private readonly ConcurrentDictionary<string, EditablePublicContent> contentByKey = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<IReadOnlyCollection<EditablePublicContent>> ListContent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<EditablePublicContent>>(contentByKey.Values.OrderBy(content => content.Key, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    /// <inheritdoc />
    public Task<EditablePublicContent?> GetContent(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        contentByKey.TryGetValue(StringSanitizer.Sanitize(key), out var content);
        return Task.FromResult(content);
    }

    /// <inheritdoc />
    public Task SaveContent(EditablePublicContent content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        ct.ThrowIfCancellationRequested();
        contentByKey[content.Key] = content;
        return Task.CompletedTask;
    }
}
