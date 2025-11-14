using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.HasKey(customer => customer.Id);
        entity.Property(customer => customer.Id).ValueGeneratedNever();

        entity.OwnsOne(customer => customer.PersonalInfo, builder => builder.ToTable("CustomerPersonalInfo"));
        entity.OwnsOne(customer => customer.IdentificationInfo, builder => builder.ToTable("CustomerIdentificationInfo"));
        entity.OwnsOne(customer => customer.ContactInfo, builder => builder.ToTable("CustomerContactInfo"));
        entity.OwnsOne(customer => customer.Address, builder => builder.ToTable("CustomerAddress"));
        entity.OwnsOne(customer => customer.PhysicalInfo, builder => builder.ToTable("CustomerPhysicalInfo"));
        entity.OwnsOne(customer => customer.AccommodationPreferences, builder => builder.ToTable("CustomerAccommodationPreferences"));
        entity.OwnsOne(customer => customer.EmergencyContact, builder => builder.ToTable("CustomerEmergencyContact"));
        entity.OwnsOne(customer => customer.MedicalInfo, builder => builder.ToTable("CustomerMedicalInfo"));
    }
}
