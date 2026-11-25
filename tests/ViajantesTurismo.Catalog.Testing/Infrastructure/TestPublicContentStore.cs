using System.Collections.Concurrent;
using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Contracts.Application;
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

    /// <inheritdoc />
    public Task<PublicContentOverviewPageDto> ListOverviewPage(string? page, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var content = contentByKey.Values;
        var window = PageWindow.Create(page, PublicContentOverviewPageDto.ItemsPerPage, content.Count);
        var entries = content.OrderBy(entry => entry.ContentKey)
            .Skip(window.Skip)
            .Take(window.Take)
            .Select(entry => new PublicContentOverviewItemDto(entry.Key, entry.PublicationState.ToString()))
            .ToArray();
        return Task.FromResult(new PublicContentOverviewPageDto(entries, content.Count));
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
