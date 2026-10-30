using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Fixed in-memory branding projection store for payment tests; read members return a ready state and writes throw <see cref="NotSupportedException" />.</summary>
internal sealed class PaymentTestBrandingProjection : IBrandingProjectionStore
{
    private static readonly BrandingProjectionState Current = new(
        Guid.Parse("0199f0aa-0000-7000-8000-000000000003"),
        1,
        "Viajantes",
        "#102030",
        "#405060",
        "#fdfdfd",
        "#111111",
        "Montserrat",
        "Inter",
        null);

    /// <summary>Returns the fixed branding projection state.</summary>
    public Task<BrandingProjectionState?> GetCurrent(CancellationToken ct) => Task.FromResult<BrandingProjectionState?>(Current);

    /// <summary>Always reports the event as not yet processed.</summary>
    public Task<bool> WasProcessed(Guid eventId, CancellationToken ct) => Task.FromResult(false);

    /// <summary>Always reports the projection as ready.</summary>
    public Task<BrandingProjectionRecovery> GetRecovery(CancellationToken ct) => Task.FromResult(BrandingProjectionRecovery.Ready);

    /// <summary>Always reports the epoch as not retired.</summary>
    public Task<bool> IsRetiredEpoch(Guid sourceEpoch, CancellationToken ct) => Task.FromResult(false);

    /// <summary>Not supported in this test double.</summary>
    public void Replace(BrandingProjectionState state) => throw new NotSupportedException();

    /// <summary>Not supported in this test double.</summary>
    public void Record(BrandingInboxEntry entry) => throw new NotSupportedException();

    /// <summary>Not supported in this test double.</summary>
    public void SetRecovery(BrandingProjectionRecovery recovery) => throw new NotSupportedException();

    /// <summary>Not supported in this test double.</summary>
    public void AppendAudit(BrandingRecoveryAuditEntry entry) => throw new NotSupportedException();

    /// <summary>Not supported in this test double.</summary>
    public Task Save(CancellationToken ct) => throw new NotSupportedException();
}
