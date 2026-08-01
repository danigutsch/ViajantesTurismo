using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Customers;
using ViajantesTurismo.Admin.Testing.Builders;

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
    public async Task Handle_succeeds_for_valid_update()
    {
        // Arrange
        var existing = EntityBuilders.BuildCustomer(new CustomerOptions(Email: "original@example.com"));
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
        (result.IsSuccess).ShouldBeTrue("Expected successful update.");
        var updated = await _store.GetById(existing.Id, CancellationToken.None);
        _ = (updated).ShouldNotBeNull();
        (updated.PersonalInfo.LastName).ShouldBe("Smith");
        (updated.ContactInfo.Email).ShouldBe("updated@example.com");
    }

    [Fact]
    public async Task Handle_returns_notfound_for_missing_customer()
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
        (result.IsFailure).ShouldBeTrue();
        (result.Status).ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_returns_invalid_for_duplicate_email()
    {
        // Arrange
        var existing1 = EntityBuilders.BuildCustomer(new CustomerOptions(Email: "a@example.com"));
        var existing2 = EntityBuilders.BuildCustomer(new CustomerOptions(Email: "dup@example.com"));
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
        (result.IsFailure).ShouldBeTrue();
        (result.Status).ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public async Task Handle_translates_a_persistence_email_conflict()
    {
        // Arrange
        var existing = EntityBuilders.BuildCustomer(new CustomerOptions(Email: "original@example.com"));
        var store = new FakeCustomerStore();
        store.Seed(existing);
        var dto = DtoBuilders.BuildUpdateCustomerDto(email: "updated@example.com");
        var command = new UpdateCustomerCommand(
            existing.Id,
            dto.PersonalInfo,
            dto.IdentificationInfo,
            dto.ContactInfo,
            dto.Address,
            dto.PhysicalInfo,
            dto.AccommodationPreferences,
            dto.EmergencyContact,
            dto.MedicalInfo);
        var handler = new UpdateCustomerCommandHandler(
            store,
            new ThrowingUnitOfWork(new CustomerEmailConflictException()),
            TimeProvider.System);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Conflict);
    }
}
