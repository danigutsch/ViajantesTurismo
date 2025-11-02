using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class CustomerContext
{
    public required PersonalInfo PersonalInfo { get; set; }
    public required IdentificationInfo IdentificationInfo { get; set; }
    public required ContactInfo ContactInfo { get; set; }
    public required Address Address { get; set; }
    public required PhysicalInfo PhysicalInfo { get; set; }
    public required AccommodationPreferences AccommodationPreferences { get; set; }
    public required EmergencyContact EmergencyContact { get; set; }
    public required MedicalInfo MedicalInfo { get; set; }
    public required Customer Customer { get; set; }
    public required Result<PersonalInfo> PersonalInfoResult { get; set; }
}