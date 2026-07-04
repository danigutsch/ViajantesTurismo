using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class IdempotencyEntryEntityConfiguration : IEntityTypeConfiguration<IdempotencyEntryEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyEntryEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CatalogIdempotencyInbox");
        builder.HasKey(entry => new { entry.Scope, entry.Key });
        builder.Property(entry => entry.Scope).HasMaxLength(200).IsRequired();
        builder.Property(entry => entry.Key).HasMaxLength(255).IsRequired();
        builder.Property(entry => entry.State).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entry => entry.ResultFingerprint).HasMaxLength(512);
    }
}
