using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class MedicalInfoContext
{
    public MedicalInfo MedicalInfo { get; set; } = null!;
    public string? Allergies { get; set; }
    public string? AdditionalInfo { get; set; }
    public required Result<MedicalInfo> Result { get; set; }
}
