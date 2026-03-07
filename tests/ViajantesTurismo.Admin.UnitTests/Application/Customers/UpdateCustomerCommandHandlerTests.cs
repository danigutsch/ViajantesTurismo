using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers;

public sealed class UpdateCustomerCommandHandlerTests
{
    private readonly FakeCustomerStore _store;
    private readonly UpdateCustomerCommandHandler _sut;

    public UpdateCustomerCommandHandlerTests()
    {
        _store = new FakeCustomerStore();
        _sut = new UpdateCustomerCommandHandler(_store, new FakeUnitOfWork(), TimeProvider.System);
    }

    [Fact]
    public async Task Handle_Succeeds_For_Valid_Update()
    {
        // Arrange
        var existing = EntityBuilders.BuildCustomer(email: "original@example.com");
        _store.Seed(existing);

        var command = new UpdateCustomerCommand(
            existing.Id,
            new PersonalInfoDto
            {
                FirstName = "John",
                LastName = "Smith",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Gender = "Male",
                Nationality = "USA",
                Occupation = "Engineer"
            },
            new IdentificationInfoDto { NationalId = "ID123", IdNationality = "USA" },
            new ContactInfoDto
            { Email = "updated@example.com", Mobile = "+1000000000", Instagram = null, Facebook = null },
            new AddressDto
            {
                Street = "Street",
                Complement = "Comp",
                Neighborhood = "Neighborhood",
                PostalCode = "12345",
                City = "City",
                State = "State",
                Country = "Country"
            },
            new PhysicalInfoDto { WeightKg = 70m, HeightCentimeters = 180, BikeType = BikeTypeDto.Regular },
            new AccommodationPreferencesDto
            { RoomType = RoomTypeDto.DoubleOccupancy, BedType = BedTypeDto.SingleBed, CompanionId = null },
            new EmergencyContactDto { Name = "Jane Doe", Mobile = "+1000000001" },
            new MedicalInfoDto { Allergies = "None", AdditionalInfo = null }
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, "Expected successful update.");
        var updated = await _store.GetById(existing.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Smith", updated.PersonalInfo.LastName);
        Assert.Equal("updated@example.com", updated.ContactInfo.Email);
    }

    [Fact]
    public async Task Handle_Returns_NotFound_For_Missing_Customer()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Guid.NewGuid(),
            new PersonalInfoDto
            {
                FirstName = "John",
                LastName = "Doe",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Gender = "Male",
                Nationality = "USA",
                Occupation = "Engineer"
            },
            new IdentificationInfoDto { NationalId = "ID123", IdNationality = "USA" },
            new ContactInfoDto
            { Email = "nonexistent@example.com", Mobile = "+1000000000", Instagram = null, Facebook = null },
            new AddressDto
            {
                Street = "Street",
                Complement = "Comp",
                Neighborhood = "Neighborhood",
                PostalCode = "12345",
                City = "City",
                State = "State",
                Country = "Country"
            },
            new PhysicalInfoDto { WeightKg = 70m, HeightCentimeters = 180, BikeType = BikeTypeDto.Regular },
            new AccommodationPreferencesDto
            { RoomType = RoomTypeDto.DoubleOccupancy, BedType = BedTypeDto.SingleBed, CompanionId = null },
            new EmergencyContactDto { Name = "Jane Doe", Mobile = "+1000000001" },
            new MedicalInfoDto { Allergies = "None", AdditionalInfo = null }
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_Returns_Invalid_For_Duplicate_Email()
    {
        // Arrange
        var existing1 = EntityBuilders.BuildCustomer(email: "a@example.com");
        var existing2 = EntityBuilders.BuildCustomer(email: "dup@example.com");
        _store.Seed(existing1);
        _store.Seed(existing2);

        var command = new UpdateCustomerCommand(
            existing1.Id,
            new PersonalInfoDto
            {
                FirstName = existing1.PersonalInfo.FirstName,
                LastName = existing1.PersonalInfo.LastName,
                BirthDate = existing1.PersonalInfo.BirthDate,
                Gender = existing1.PersonalInfo.Gender,
                Nationality = existing1.PersonalInfo.Nationality,
                Occupation = existing1.PersonalInfo.Occupation
            },
            new IdentificationInfoDto
            {
                NationalId = existing1.IdentificationInfo.NationalId,
                IdNationality = existing1.IdentificationInfo.IdNationality
            },
            new ContactInfoDto
            { Email = "dup@example.com", Mobile = existing1.ContactInfo.Mobile, Instagram = null, Facebook = null },
            new AddressDto
            {
                Street = "Street",
                Complement = "Comp",
                Neighborhood = "Neighborhood",
                PostalCode = "12345",
                City = "City",
                State = "State",
                Country = "Country"
            },
            new PhysicalInfoDto
            {
                WeightKg = existing1.PhysicalInfo.WeightKg,
                HeightCentimeters = existing1.PhysicalInfo.HeightCentimeters,
                BikeType = BikeTypeDto.Regular
            },
            new AccommodationPreferencesDto
            { RoomType = RoomTypeDto.DoubleOccupancy, BedType = BedTypeDto.SingleBed, CompanionId = null },
            new EmergencyContactDto
            { Name = existing1.EmergencyContact.Name, Mobile = existing1.EmergencyContact.Mobile },
            new MedicalInfoDto
            { Allergies = existing1.MedicalInfo.Allergies, AdditionalInfo = existing1.MedicalInfo.AdditionalInfo }
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }
}
