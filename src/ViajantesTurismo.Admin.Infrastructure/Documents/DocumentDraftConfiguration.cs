using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// Configures persistence for generated document revisions and their classified fields.
/// </summary>
internal sealed class DocumentDraftConfiguration : IEntityTypeConfiguration<DocumentDraft>
{
    public void Configure(EntityTypeBuilder<DocumentDraft> entity)
    {
        entity.HasKey(document => document.Id);
        entity.Property(document => document.Id).ValueGeneratedNever();
        entity.Property(document => document.BookingId).IsRequired();
        entity.Property(document => document.Type).HasConversion<string>().IsRequired();
        entity.Property(document => document.Audience).HasConversion<string>().IsRequired();
        entity.Property(document => document.TemplateId).HasMaxLength(DocumentLimits.MaxTemplateIdLength).IsRequired();
        entity.Property(document => document.TemplateVersion).HasMaxLength(DocumentLimits.MaxTemplateVersionLength).IsRequired();
        entity.Property(document => document.Revision).IsRequired();
        entity.Property(document => document.SourceVersion).HasMaxLength(DocumentLimits.MaxSourceVersionLength).IsRequired();
        entity.Property(document => document.BrandingVersion).HasMaxLength(DocumentLimits.MaxBrandingVersionLength).IsRequired();
        entity.Property(document => document.BrandingName).HasMaxLength(DocumentLimits.MaxBrandingNameLength).IsRequired();
        entity.Property(document => document.BrandingLogoUri)
            .HasConversion(new ValueConverter<Uri?, string?>(
                uri => uri == null ? null : uri.OriginalString,
                value => ToSafeLogoUri(value)))
            .HasMaxLength(DocumentLimits.MaxBrandingLogoUriLength);
        entity.Property(document => document.Status).HasConversion<string>().IsRequired();
        entity.Property(document => document.CreatedAt).IsRequired();
        entity.Property(document => document.UpdatedAt).IsRequired();
        entity.Property(document => document.RetentionExpiresAt).IsRequired();
        entity.HasIndex(document => document.RetentionExpiresAt)
            .HasDatabaseName("IX_DocumentDrafts_RetentionExpiresAt_Unfinalized")
            .HasFilter("\"FinalizedAt\" IS NULL");
        entity.Property(document => document.FinalizedArtifactName).HasMaxLength(128);
        entity.Property(document => document.VoidReason).HasMaxLength(DocumentLimits.MaxVoidReasonLength);
        entity.Property<byte[]?>("_finalizedArtifactContent").HasColumnName("FinalizedArtifactContent");

        entity.OwnsMany(document => document.Fields, field =>
        {
            field.ToTable("DocumentDraftFields");
            field.WithOwner().HasForeignKey("DocumentDraftId");
            field.HasKey("DocumentDraftId", nameof(DocumentField.FieldId));
            field.Property(documentField => documentField.FieldId).HasMaxLength(DocumentLimits.MaxFieldIdLength).IsRequired();
            field.Property(documentField => documentField.Label).HasMaxLength(DocumentLimits.MaxFieldLabelLength).IsRequired();
            field.Property(documentField => documentField.Value).HasMaxLength(DocumentLimits.MaxFieldValueLength).IsRequired();
            field.Property(documentField => documentField.StaffOverride).HasMaxLength(DocumentLimits.MaxFieldValueLength);
            field.Property(documentField => documentField.PrivacyClassification).HasConversion<string>().IsRequired();
            field.Property(documentField => documentField.IsEditable).IsRequired();
        });
        entity.Navigation(document => document.Fields).Metadata.SetField("_fields");
        entity.Navigation(document => document.Fields).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static Uri? ToSafeLogoUri(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
        {
            return null;
        }

        if (uri.IsAbsoluteUri)
        {
            return uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) ? uri : null;
        }

        return value.StartsWith('/')
            && !value.StartsWith("//", StringComparison.Ordinal)
            && !value.Contains('\\', StringComparison.Ordinal)
            && !value.Any(static character => char.IsWhiteSpace(character) || char.IsControl(character))
            ? uri
            : null;
    }
}
