using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class AccommodationPreferencesContext
{
    public AccommodationPreferences? AccommodationPreferences { get; set; }
    public RoomType RoomType { get; set; }
    public BedType BedType { get; set; }
    public Guid? CompanionId { get; set; }
    public required Result<AccommodationPreferences> Result { get; set; }
}
