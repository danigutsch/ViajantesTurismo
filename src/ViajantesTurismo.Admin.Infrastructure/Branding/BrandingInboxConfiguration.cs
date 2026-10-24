using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

internal sealed class BrandingInboxConfiguration : IEntityTypeConfiguration<BrandingInboxRecord>
{
    private const int MaxReasonCodeLength = 64;
    private const int MaxOutcomeLength = 16;

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
