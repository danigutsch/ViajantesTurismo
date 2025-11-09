using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class IdentificationInfoContext
{
    public string? NationalId { get; set; }
    public string? IdNationality { get; set; }
    public required Result<IdentificationInfo> Result { get; set; }
    public IdentificationInfo IdentificationInfo => Result.Value;
}
