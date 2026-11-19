using System.Security.Cryptography;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Holds the sealed bytes for one document revision.</summary>
internal sealed class DocumentArtifactBody
{
    /// <summary>The length of the canonical hexadecimal SHA-256 digest.</summary>
    internal const int Sha256HexLength = SHA256.HashSizeInBytes * 2;

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

    /// <summary>Gets the exact length of the sealed bytes.</summary>
    internal int Length => _content.Length;

    /// <summary>Computes the digest of the sealed copy.</summary>
    internal string GetSha256() => ComputeSha256(_content);

    /// <summary>Computes the canonical lowercase SHA-256 digest of the supplied bytes.</summary>
    internal static string ComputeSha256(ReadOnlySpan<byte> content)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(content, hash);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Returns an independent copy of the sealed bytes.</summary>
    public ReadOnlyMemory<byte> CopyContent() => new((byte[])_content.Clone());
}
