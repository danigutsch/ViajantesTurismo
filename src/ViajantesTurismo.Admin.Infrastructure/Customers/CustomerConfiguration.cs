using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PandaBear.EntityFrameworkCore;
using PandaBear.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Infrastructure.Customers;

/// <summary>Maps the customer aggregate and its owned value objects to separate tables.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <summary>Configures the customer key, owned tables and unique email index.</summary>
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.HasKey(customer => customer.Id);
        entity.Property(customer => customer.Id).ValueGeneratedNever();

        entity.OwnsOne(customer => customer.PersonalInfo, builder =>
        {
            builder.ToTable("CustomerPersonalInfo");
            builder.Property(info => info.BirthDate)
                .HasConversion(birthDate => birthDate.Value, value => BirthDate.Restore(value))
                .HasColumnType("date");
        });
        entity.OwnsOne(customer => customer.IdentificationInfo, builder => builder.ToTable("CustomerIdentificationInfo"));
        entity.OwnsOne(customer => customer.ContactInfo, builder =>
        {
            builder.ToTable("CustomerContactInfo");
            builder.Property(contactInfo => contactInfo.Email)
                .HasConversion(email => email.Value, value => EmailAddress.Restore(value))
                .HasColumnType("citext");
            builder.Property(contactInfo => contactInfo.Mobile)
                .HasConversion(phone => phone.Value, value => PhoneNumber.Restore(value))
                .HasMaxLength(PhoneNumber.MaxDigits + 1);
            builder.HasIndex(contactInfo => contactInfo.Email)
                .IsUnique()
                .HasDatabaseName(CustomerSchema.EmailUniqueIndex)
                .HasUniqueRule(CustomerSchema.EmailUniqueRule);
        });
        entity.OwnsOne(customer => customer.Address, builder =>
        {
            builder.ToTable("CustomerAddress");
            builder.Property(address => address.Country)
                .HasConversion(country => country.Value, value => CountryCode.Restore(value))
                .HasMaxLength(2)
                .IsFixedLength();
        });
        entity.OwnsOne(customer => customer.PhysicalInfo, builder => builder.ToTable("CustomerPhysicalInfo"));
        entity.OwnsOne(customer => customer.AccommodationPreferences, builder => builder.ToTable("CustomerAccommodationPreferences"));
        entity.OwnsOne(customer => customer.EmergencyContact, builder =>
        {
            builder.ToTable("CustomerEmergencyContact");
            builder.Property(contact => contact.Mobile)
                .HasConversion(phone => phone.Value, value => PhoneNumber.Restore(value))
                .HasMaxLength(PhoneNumber.MaxDigits + 1);
        });
        entity.OwnsOne(customer => customer.MedicalInfo, builder => builder.ToTable("CustomerMedicalInfo"));
    }
}
