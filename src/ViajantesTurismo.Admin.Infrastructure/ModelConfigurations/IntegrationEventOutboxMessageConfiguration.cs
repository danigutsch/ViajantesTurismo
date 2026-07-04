using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

internal sealed class IntegrationEventOutboxMessageConfiguration : IEntityTypeConfiguration<IntegrationEventOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationEventOutboxMessage> entity)
    {
        entity.ToTable("IntegrationEventOutbox");
        entity.HasKey(message => message.Id);
        entity.Property(message => message.Id).ValueGeneratedNever();
        entity.HasIndex(message => message.EventId).IsUnique();
        entity.HasIndex(message => new { message.PublishedAt, message.EnqueuedAt });
        entity.Property(message => message.EventType).HasMaxLength(200).IsRequired();
        entity.Property(message => message.PayloadJson).IsRequired();
    }
}
