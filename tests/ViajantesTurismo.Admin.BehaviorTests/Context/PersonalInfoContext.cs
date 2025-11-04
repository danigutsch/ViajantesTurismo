using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class PersonalInfoContext
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Gender { get; set; }
    public required DateTime BirthDate { get; set; }
    public required string Nationality { get; set; }
    public required string Profession { get; set; }
    public required Result<PersonalInfo> Result { get; set; }
}