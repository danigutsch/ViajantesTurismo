using PandaBear.ObjectStorage;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Deletes stored media objects when a later step failed or the request was cancelled. The delete runs on a short, bounded token that the request does not own,
/// so a cancelled request still removes the object it wrote; the reconciliation service remains the backstop for a delete that fails.
/// </summary>
internal static class MediaObjectCompensation
{
    /// <summary>How long a compensating delete may run after the request has been cancelled.</summary>
    internal static readonly TimeSpan DeleteTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Deletes the object on a token that is independent of the request and stops after <see cref="DeleteTimeout" />.</summary>
    /// <param name="objectStore">The object store.</param>
    /// <param name="objectKey">The key of the object to remove.</param>
    internal static async ValueTask Delete(IObjectStore objectStore, string objectKey)
    {
        using var timeout = new CancellationTokenSource(DeleteTimeout);
        await objectStore.Delete(objectKey, timeout.Token).ConfigureAwait(false);
    }
}
