using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class AdminEntityIdentityEqualityTests
{
    [Fact]
    public void Customer_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var first = EntityIdTestData.CreateCustomer();
        var second = EntityIdTestData.CreateCustomer();
        var different = EntityIdTestData.CreateCustomer();

        // Act
        EntityIdAssertions.AssertGeneratedIdentitySemantics(first, second, different);
    }

    [Fact]
    public void Tour_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var first = EntityBuilders.BuildTour();
        var second = EntityBuilders.BuildTour();
        var different = EntityBuilders.BuildTour();

        // Act
        EntityIdAssertions.AssertGeneratedIdentitySemantics(first, second, different);
    }

    [Fact]
    public void Booking_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var first = BookingDomainTestDataFactory.CreateSingleBooking();
        var second = BookingDomainTestDataFactory.CreateSingleBooking();
        var different = BookingDomainTestDataFactory.CreateSingleBooking();

        // Act
        EntityIdAssertions.AssertGeneratedIdentitySemantics(first, second, different);
    }

    [Fact]
    public void Payment_identity_equality_matches_existing_entity_semantics()
    {
        // Arrange
        var first = EntityIdTestData.CreatePayment();
        var second = EntityIdTestData.CreatePayment();
        var different = EntityIdTestData.CreatePayment();

        // Act
        EntityIdAssertions.AssertGeneratedIdentitySemantics(first, second, different);
    }
}
