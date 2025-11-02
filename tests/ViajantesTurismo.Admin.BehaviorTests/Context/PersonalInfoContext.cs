using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class PersonalInfoContext
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public DateTime BirthDate { get; set; }
    public string Nationality { get; set; } = null!;
    public string Profession { get; set; } = null!;
    public Result<PersonalInfo>? Result { get; set; }
}
