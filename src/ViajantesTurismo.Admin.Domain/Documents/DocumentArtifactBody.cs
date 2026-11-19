namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Holds the sealed bytes for one document revision.</summary>
internal sealed class DocumentArtifactBody
{
    private byte[] _content = [];

    private DocumentArtifactBody()
    {
    }

    /// <summary>Seals an independent copy of the revision's artifact.</summary>
    /// <param name="documentId">The document revision identifier.</param>
    /// <param name="content">The finalized bytes.</param>
    internal DocumentArtifactBody(Guid documentId, ReadOnlySpan<byte> content)
    {
        DocumentId = documentId;
        _content = content.ToArray();
    }

    /// <summary>Gets the revision that sealed this artifact.</summary>
    public Guid DocumentId
    {
        get;
        private set;
    }

    /// <summary>Returns an independent copy of the sealed bytes.</summary>
    public ReadOnlyMemory<byte> CopyContent() => new((byte[])_content.Clone());
}
