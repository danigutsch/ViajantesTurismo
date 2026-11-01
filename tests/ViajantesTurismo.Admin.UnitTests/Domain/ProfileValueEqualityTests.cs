using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Protects structural identity of validated profile and discount values.</summary>
public sealed class ProfileValueEqualityTests
{
    [Fact]
    public void Equivalent_contacts_share_value_identity_after_sanitization()
    {
        // Arrange
        var first = ContactInfo.Create(" person@example.com ", "+49 1234", null, null).Value;
        var second = ContactInfo.Create("person@example.com", "+49 1234", "", " ").Value;

        // Act
        var count = new HashSet<ContactInfo> { first, second }.Count;

        // Assert
        count.ShouldBe(1);
        first.Equals(second).ShouldBeTrue();
    }

    [Fact]
    public void Equivalent_addresses_share_value_identity()
    {
        // Arrange
        var first = Address.Create("Street 1", null, "Center", "12345", "Berlin", "Berlin", "Germany").Value;
        var second = Address.Create("Street 1", null, "Center", "12345", "Berlin", "Berlin", "Germany").Value;

        // Act
        var count = new HashSet<Address> { first, second }.Count;

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    public void Equivalent_discounts_share_identity_including_the_reason()
    {
        // Arrange
        var first = Discount.Create(DiscountType.Percentage, 10, "Early booking").Value;
        var same = Discount.Create(DiscountType.Percentage, 10, "Early booking").Value;
        var other = Discount.Create(DiscountType.Percentage, 10, "Loyal customer").Value;

        // Act
        var count = new HashSet<Discount> { first, same, other }.Count;

        // Assert
        count.ShouldBe(2);
    }
}
