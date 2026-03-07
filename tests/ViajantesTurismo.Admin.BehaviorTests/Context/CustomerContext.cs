using JetBrains.Annotations;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class CustomerContext
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public DateTime BirthDate { get; set; }
    public string Nationality { get; set; } = null!;
    public string Occupation { get; set; } = null!;

    public Guid? CompanionId { get; set; }

    public required Result<PersonalInfo> PersonalInfoResult { get; set; }
    public required Result<IdentificationInfo> IdentificationInfoResult { get; set; }
    public required Result<ContactInfo> ContactInfoResult { get; set; }
    public required Result<Address> AddressResult { get; set; }
    public required Result<EmergencyContact> EmergencyContactResult { get; set; }
    public Result<PhysicalInfo>? PhysicalInfoResult { get; set; }
    public Result<MedicalInfo>? MedicalInfoResult { get; set; }
    public Result<AccommodationPreferences>? AccommodationPreferencesResult { get; set; }

    public required Customer Customer { get; set; }

    public ICollection<Customer> Customers { get; } = [];

    public FakeCustomerStore CustomerStore { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public CreateCustomerCommandHandler CommandHandler => new(CustomerStore, UnitOfWork, TimeProvider.System);

    public Result<Guid>? CommandResult { get; set; }
}
