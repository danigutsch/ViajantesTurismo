using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class PhysicalInfoContext
{
    public PhysicalInfo PhysicalInfo { get; set; } = null!;
    public decimal WeightKg { get; set; }
    public int HeightCentimeters { get; set; }
    public BikeType BikeType { get; set; }
    public required Result<PhysicalInfo> Result { get; set; }
}
