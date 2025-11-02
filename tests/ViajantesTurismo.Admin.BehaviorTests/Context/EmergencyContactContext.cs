using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class EmergencyContactContext
{
    public EmergencyContact EmergencyContact { get; set; } = null!;
    public required string Name { get; set; }
    public required string Mobile { get; set; }
    public required Result<EmergencyContact> Result { get; set; }
}