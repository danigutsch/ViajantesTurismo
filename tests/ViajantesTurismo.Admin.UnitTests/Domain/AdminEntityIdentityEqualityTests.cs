using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class AdminEntityIdentityEqualityTests
{
    [Fact]
    public void Customer_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = EntityIdTestData.CreateCustomer();
        var second = EntityIdTestData.CreateCustomer();
        EntityIdAssertions.SetId(first, id);
        EntityIdAssertions.SetId(second, id);

        // Act
        var equalsOther = first.Equals(second);
        var equalsDifferentType = first.Equals(new object());

        // Assert
        Assert.True(equalsOther);
        Assert.False(equalsDifferentType);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Tour_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = EntityBuilders.BuildTour();
        var second = EntityBuilders.BuildTour();
        EntityIdAssertions.SetId(first, id);
        EntityIdAssertions.SetId(second, id);

        // Act
        var equalsOther = first.Equals(second);
        var equalsDifferentType = first.Equals(new object());

        // Assert
        Assert.True(equalsOther);
        Assert.False(equalsDifferentType);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Booking_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = BookingDomainTestDataFactory.CreateSingleBooking();
        var second = BookingDomainTestDataFactory.CreateSingleBooking();
        EntityIdAssertions.SetId(first, id);
        EntityIdAssertions.SetId(second, id);

        // Act
        var equalsOther = first.Equals(second);
        var equalsDifferentType = first.Equals(new object());

        // Assert
        Assert.True(equalsOther);
        Assert.False(equalsDifferentType);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Payment_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = EntityIdTestData.CreatePayment();
        var second = EntityIdTestData.CreatePayment();
        EntityIdAssertions.SetId(first, id);
        EntityIdAssertions.SetId(second, id);

        // Act
        var equalsOther = first.Equals(second);
        var equalsDifferentType = first.Equals(new object());

        // Assert
        Assert.True(equalsOther);
        Assert.False(equalsDifferentType);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
