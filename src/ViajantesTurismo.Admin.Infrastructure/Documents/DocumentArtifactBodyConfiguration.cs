using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Maps sealed document bytes separately from revision metadata.</summary>
internal sealed class DocumentArtifactBodyConfiguration : IEntityTypeConfiguration<DocumentArtifactBody>
{
    private const string ContentLengthConstraint = "CK_DocumentArtifactBodies_ContentLength";

    /// <summary>The table holding the sealed artifact payload for a revision.</summary>
    internal const string TableName = "DocumentArtifactBodies";

    /// <summary>The database column holding the sealed artifact payload.</summary>
    internal const string ContentColumn = "Content";

    /// <summary>The mapped field holding the sealed byte payload.</summary>
    internal const string ContentField = "_content";

    /// <summary>Maps the artifact table, storage and size constraint.</summary>
    public void Configure(EntityTypeBuilder<DocumentArtifactBody> entity)
    {
        entity.ToTable(TableName, table => table.HasCheckConstraint(
            ContentLengthConstraint,
            string.Create(CultureInfo.InvariantCulture, $"octet_length(\"{ContentColumn}\") BETWEEN 1 AND {DocumentLimits.MaxFinalizedArtifactBytes}")));
        entity.HasKey(body => body.DocumentId);
        entity.Property(body => body.DocumentId).ValueGeneratedNever();
        entity.Property<byte[]>(ContentField).HasColumnName(ContentColumn).IsRequired();
    }
}
