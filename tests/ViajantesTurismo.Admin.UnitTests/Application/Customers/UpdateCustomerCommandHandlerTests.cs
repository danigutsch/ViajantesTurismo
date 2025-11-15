using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers;

public sealed class UpdateCustomerCommandHandlerTests
{
    private static Customer CreateTestCustomer(TimeProvider timeProvider, string email = "original@example.com")
    {
        var personal = PersonalInfo
            .Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", timeProvider).Value;
        var identification = IdentificationInfo.Create("ID123", "USA").Value;
        var contact = ContactInfo.Create(email, "+1000000000", null, null).Value;
        var address = Address.Create("Street", "Comp", "Neighborhood", "12345", "City", "State", "Country").Value;
        var physical = PhysicalInfo.Create(70m, 180, BikeType.Regular).Value;
        var accommodation = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergency = EmergencyContact.Create("Jane Doe", "+1000000001").Value;
        var medical = MedicalInfo.Create("None", null).Value;
        return new Customer(personal, identification, contact, address, physical, accommodation, emergency, medical);
    }

    [Fact]
    public async Task Handle_Succeeds_For_Valid_Update()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var store = new FakeCustomerStore();
        var existing = CreateTestCustomer(timeProvider);
        store.Seed(existing);
        var uow = new FakeUnitOfWork();
        var handler = new UpdateCustomerCommandHandler(store, uow, timeProvider);

        var command = new UpdateCustomerCommand(
            existing.Id,
            new PersonalInfoDto
            {
                FirstName = "John",
                LastName = "Smith",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Gender = "Male",
                Nationality = "USA",
                Profession = "Engineer"
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
            { RoomType = RoomTypeDto.SingleRoom, BedType = BedTypeDto.SingleBed, CompanionId = null },
            new EmergencyContactDto { Name = "Jane Doe", Mobile = "+1000000001" },
            new MedicalInfoDto { Allergies = "None", AdditionalInfo = null }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, "Expected successful update.");
        var updated = await store.GetById(existing.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Smith", updated.PersonalInfo.LastName);
        Assert.Equal("updated@example.com", updated.ContactInfo.Email);
    }

    [Fact]
    public async Task Handle_Returns_NotFound_For_Missing_Customer()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var store = new FakeCustomerStore();
        var uow = new FakeUnitOfWork();
        var handler = new UpdateCustomerCommandHandler(store, uow, timeProvider);

        var command = new UpdateCustomerCommand(
            Guid.NewGuid(),
            new PersonalInfoDto
            {
                FirstName = "John",
                LastName = "Doe",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Gender = "Male",
                Nationality = "USA",
                Profession = "Engineer"
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
            { RoomType = RoomTypeDto.SingleRoom, BedType = BedTypeDto.SingleBed, CompanionId = null },
            new EmergencyContactDto { Name = "Jane Doe", Mobile = "+1000000001" },
            new MedicalInfoDto { Allergies = "None", AdditionalInfo = null }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_Returns_Invalid_For_Duplicate_Email()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var store = new FakeCustomerStore();
        var existing1 = CreateTestCustomer(timeProvider, email: "a@example.com");
        var existing2 = CreateTestCustomer(timeProvider, email: "dup@example.com");
        store.Seed(existing1);
        store.Seed(existing2);
        var uow = new FakeUnitOfWork();
        var handler = new UpdateCustomerCommandHandler(store, uow, timeProvider);

        var command = new UpdateCustomerCommand(
            existing1.Id,
            new PersonalInfoDto
            {
                FirstName = existing1.PersonalInfo.FirstName,
                LastName = existing1.PersonalInfo.LastName,
                BirthDate = existing1.PersonalInfo.BirthDate,
                Gender = existing1.PersonalInfo.Gender,
                Nationality = existing1.PersonalInfo.Nationality,
                Profession = existing1.PersonalInfo.Profession
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
            { RoomType = RoomTypeDto.SingleRoom, BedType = BedTypeDto.SingleBed, CompanionId = null },
            new EmergencyContactDto
            { Name = existing1.EmergencyContact.Name, Mobile = existing1.EmergencyContact.Mobile },
            new MedicalInfoDto
            { Allergies = existing1.MedicalInfo.Allergies, AdditionalInfo = existing1.MedicalInfo.AdditionalInfo }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveEntities(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeCustomerStore : ICustomerStore
    {
        private readonly List<Customer> _customers = new();
        public void Add(Customer customer) => _customers.Add(customer);

        public Task<Customer?> GetById(Guid id, CancellationToken ct) =>
            Task.FromResult(_customers.SingleOrDefault(c => c.Id == id));

        public void Delete(Customer customer) => _customers.Remove(customer);

        public Task<bool> EmailExists(string email, CancellationToken ct) =>
            Task.FromResult(_customers.Any(c => c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct) =>
            Task.FromResult(_customers.Any(c =>
                c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && c.Id != excludeCustomerId));

        public void Seed(Customer c) => _customers.Add(c);
    }
}
