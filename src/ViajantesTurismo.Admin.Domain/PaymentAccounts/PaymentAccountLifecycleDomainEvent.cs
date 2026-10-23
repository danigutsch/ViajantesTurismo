using PandaBear.Domain;

namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Contains metadata-only account lifecycle audit evidence, without recipient details.</summary>
/// <param name="Id">The audit event identifier.</param>
/// <param name="AccountId">The account identifier.</param>
/// <param name="Revision">The affected recipient revision.</param>
/// <param name="ActorId">The authenticated actor identifier.</param>
/// <param name="OccurredAt">The controlled UTC operation instant.</param>
/// <param name="Reason">The required reason code.</param>
/// <param name="SelfApproved">Whether verification was performed by the revision creator.</param>
public sealed record PaymentAccountLifecycleDomainEvent(
    Guid Id, Guid AccountId, int Revision, string ActorId, DateTimeOffset OccurredAt,
    PaymentAccountReason Reason, bool SelfApproved) : IDomainEvent;
