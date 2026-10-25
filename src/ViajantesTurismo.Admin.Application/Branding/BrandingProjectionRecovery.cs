namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>The persisted recovery fence for the Admin Branding projection.</summary>
/// <param name="Status">The projection status.</param>
/// <param name="Reason">Why recovery started, when recovering.</param>
/// <param name="Since">When recovery started, when recovering.</param>
/// <param name="Authorization">The pending reseed authorization, if any.</param>
public sealed record BrandingProjectionRecovery(
    BrandingProjectionStatus Status,
    string? Reason,
    DateTimeOffset? Since,
    BrandingReseedAuthorization? Authorization)
{
    /// <summary>Gets the state of a projection that follows ordinary delivery.</summary>
    public static BrandingProjectionRecovery Ready { get; } = new(BrandingProjectionStatus.Ready, null, null, null);

    /// <summary>Gets whether document generation may read the projection.</summary>
    public bool IsReady => Status == BrandingProjectionStatus.Ready;

    /// <summary>Fences the projection.</summary>
    /// <param name="reason">Why recovery started.</param>
    /// <param name="since">When recovery started.</param>
    /// <returns>The recovering state.</returns>
    public static BrandingProjectionRecovery Start(string reason, DateTimeOffset since)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new BrandingProjectionRecovery(BrandingProjectionStatus.Recovering, reason, since, null);
    }

    /// <summary>Records the one-shot reseed authorization while recovering.</summary>
    /// <param name="authorization">The authorization.</param>
    /// <returns>The recovering state with the authorization.</returns>
    public BrandingProjectionRecovery Authorize(BrandingReseedAuthorization authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        if (IsReady)
        {
            throw new InvalidOperationException("A reseed can be authorized only while the Branding projection is recovering.");
        }

        return this with
        {
            Authorization = authorization
        };
    }
}
