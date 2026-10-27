using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>EF Core mapping for <see cref="BrandingInboxRecord" /> in the branding projection schema.</summary>
internal sealed class BrandingInboxConfiguration : IEntityTypeConfiguration<BrandingInboxRecord>
{
    private const int MaxReasonCodeLength = 64;
    private const int MaxOutcomeLength = 16;

    /// <summary>Maps the inbox table, event id key, outcome string conversion and column lengths.</summary>
    public void Configure(EntityTypeBuilder<BrandingInboxRecord> entity)
    {
        entity.ToTable("inbox_messages", BrandingProjectionSchema.Name);
        entity.HasKey(message => message.EventId);
        entity.Property(message => message.EventId).ValueGeneratedNever();
        entity.Property(message => message.Outcome).HasConversion<string>().HasMaxLength(MaxOutcomeLength).IsRequired();
        entity.Property(message => message.ReasonCode).HasMaxLength(MaxReasonCodeLength);
        entity.HasIndex(message => message.Outcome);
    }
}
