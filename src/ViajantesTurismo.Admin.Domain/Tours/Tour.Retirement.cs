using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

public sealed partial class Tour
{
    /// <summary>Gets the stable identifier of the accepted retirement.</summary>
    public Guid? RetirementId
    {
        get; private set;
    }

    /// <summary>Gets the UTC instant when the tour stopped accepting active operations.</summary>
    public DateTimeOffset? RetiredAt
    {
        get; private set;
    }

    /// <summary>Gets the UTC instant when Catalog completed the visibility change.</summary>
    public DateTimeOffset? CatalogRetirementAppliedAt
    {
        get; private set;
    }

    /// <summary>Gets whether the tour has retired from active use.</summary>
    public bool IsRetired => RetirementId.HasValue;

    /// <summary>Retires the tour while preserving its booking history and relationships.</summary>
    /// <param name="retirementId">The stable identifier of the requested retirement.</param>
    /// <param name="actorId">The authenticated opaque actor identifier.</param>
    /// <param name="retiredAt">The server-captured retirement instant.</param>
    /// <returns>True for a new retirement, false for a replay, or a failure.</returns>
    public Result<bool> Retire(Guid retirementId, string actorId, DateTimeOffset retiredAt)
    {
        if (IsRetired)
        {
            return Result.Ok(false);
        }

        if (retirementId == Guid.Empty || string.IsNullOrWhiteSpace(actorId)
            || actorId.Length > TourRetirementAuditRecord.MaxActorIdLength)
        {
            return TourErrors.InvalidRetirementIdentity().ConvertError<bool>();
        }

        if (_bookings.Any(booking => booking.IsPending || booking.IsConfirmed))
        {
            return TourErrors.CannotRetireWithActiveBookings().ConvertError<bool>();
        }

        var retiredAtUtc = retiredAt.ToUniversalTime();
        RetirementId = retirementId;
        RetiredAt = retiredAtUtc;
        _domainEvents.Add(new TourRetiredDomainEvent(Id, retirementId, actorId, retiredAtUtc));
        return Result.Ok(true);
    }
}
