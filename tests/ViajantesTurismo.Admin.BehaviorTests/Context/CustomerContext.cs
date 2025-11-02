using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class CustomerContext
{
    public PersonalInfo PersonalInfo { get; set; } = null!;
    public IdentificationInfo IdentificationInfo { get; set; } = null!;
    public ContactInfo ContactInfo { get; set; } = null!;
    public Address Address { get; set; } = null!;
    public PhysicalInfo PhysicalInfo { get; set; } = null!;
    public AccommodationPreferences AccommodationPreferences { get; set; } = null!;
    public EmergencyContact EmergencyContact { get; set; } = null!;
    public MedicalInfo MedicalInfo { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Result<PersonalInfo> PersonalInfoResult { get; set; }
}