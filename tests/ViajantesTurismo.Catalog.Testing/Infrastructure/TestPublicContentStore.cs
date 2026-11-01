using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestPublicContentStore : IPublicContentStore
{
    private readonly ConcurrentDictionary<PublicContentKey, EditablePublicContent> contentByKey = new();

    public Task<IReadOnlyCollection<EditablePublicContent>> ListContent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<EditablePublicContent>>([.. contentByKey.Values]);
    }

    public Task<EditablePublicContent?> GetContent(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var contentKey = PublicContentKey.Create(key);
        var content = contentKey.IsSuccess ? contentByKey.GetValueOrDefault(contentKey.Value) : null;

        return Task.FromResult(content);
    }

    public Task SaveContent(EditablePublicContent content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        ct.ThrowIfCancellationRequested();

        contentByKey[content.ContentKey] = content;

        return Task.CompletedTask;
    }
}
