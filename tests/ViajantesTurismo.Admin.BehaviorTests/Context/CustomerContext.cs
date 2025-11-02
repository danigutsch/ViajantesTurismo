using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class CustomerContext
{
    public required Result<PersonalInfo> PersonalInfoResult { get; set; }
    public PersonalInfo PersonalInfo => PersonalInfoResult.Value;

    public required Result<IdentificationInfo> IdentificationInfoResult { get; set; }
    public IdentificationInfo IdentificationInfo => IdentificationInfoResult.Value;

    public required Result<ContactInfo> ContactInfoResult { get; set; }
    public ContactInfo ContactInfo => ContactInfoResult.Value;

    public required Result<Address> AddressResult { get; set; }
    public Address Address => AddressResult.Value;

    public required Result<EmergencyContact> EmergencyContactResult { get; set; }
    public EmergencyContact EmergencyContact => EmergencyContactResult.Value;

    public required PhysicalInfo PhysicalInfo { get; set; }
    public required AccommodationPreferences AccommodationPreferences { get; set; }
    public required MedicalInfo MedicalInfo { get; set; }
    public required Customer Customer { get; set; }
}