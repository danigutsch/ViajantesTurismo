using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class CustomerCreateTests
{
    [Fact]
    public void Create_returns_a_customer_holding_every_validated_value()
    {
        // Arrange
        var definition = CustomerMapper.MapToDefinition(
            EntityBuilders.BuildCustomer(new CustomerOptions(FirstName: "Ana", City: "Lisbon")));
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

        // Act
        var result = Customer.Create(definition, today);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var customer = result.Value;
        customer.Id.ShouldNotBe(Guid.Empty);
        customer.PersonalInfo.FirstName.ShouldBe("Ana");
        customer.Address.City.ShouldBe("Lisbon");
        var accommodation = customer.AccommodationPreferences;
        accommodation.RoomType.ShouldBe(definition.AccommodationPreferences.RoomType);
        accommodation.BedType.ShouldBe(definition.AccommodationPreferences.BedType);
        accommodation.CompanionId.ShouldBe(definition.AccommodationPreferences.CompanionId);
    }

    [Fact]
    public void Create_rejects_a_definition_with_an_undefined_room_type()
    {
        // Arrange
        var validDefinition = CustomerMapper.MapToDefinition(EntityBuilders.BuildCustomer(null));
        var definition = validDefinition with
        {
            AccommodationPreferences = validDefinition.AccommodationPreferences with
            {
                RoomType = (RoomType)999
            }
        };
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

        // Act
        var result = Customer.Create(definition, today);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors.ContainsKey("RoomType").ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_a_definition_with_an_invalid_value()
    {
        // Arrange
        var validDefinition = CustomerMapper.MapToDefinition(EntityBuilders.BuildCustomer(null));
        var definition = validDefinition with
        {
            ContactInfo = validDefinition.ContactInfo with
            {
                Email = "not-an-email"
            }
        };
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

        // Act
        var result = Customer.Create(definition, today);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
}
