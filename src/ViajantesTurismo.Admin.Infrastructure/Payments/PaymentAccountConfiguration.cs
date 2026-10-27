using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Maps payment account rows to the PaymentAccounts table.</summary>
internal sealed class PaymentAccountConfiguration : IEntityTypeConfiguration<PaymentAccountRow>
{
    /// <summary>Configures the key, concurrency token, status conversion and protected state.</summary>
    public void Configure(EntityTypeBuilder<PaymentAccountRow> entity)
    {
        entity.ToTable("PaymentAccounts");
        entity.HasKey(row => row.Id);
        entity.Property(row => row.Id).ValueGeneratedNever();
        entity.Property(row => row.Version).IsConcurrencyToken();
        entity.Property(row => row.Status).HasConversion<string>();
        entity.Property(row => row.ProtectedState).IsRequired();
    }
}
