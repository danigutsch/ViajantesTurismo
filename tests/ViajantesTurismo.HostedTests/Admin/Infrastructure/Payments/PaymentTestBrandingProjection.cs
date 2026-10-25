using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

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

    public Task<BrandingProjectionState?> GetCurrent(CancellationToken ct) => Task.FromResult<BrandingProjectionState?>(Current);

    public Task<bool> WasProcessed(Guid eventId, CancellationToken ct) => Task.FromResult(false);

    public Task<BrandingProjectionRecovery> GetRecovery(CancellationToken ct) => Task.FromResult(BrandingProjectionRecovery.Ready);

    public Task<bool> IsRetiredEpoch(Guid sourceEpoch, CancellationToken ct) => Task.FromResult(false);

    public void Replace(BrandingProjectionState state) => throw new NotSupportedException();

    public void Record(BrandingInboxEntry entry) => throw new NotSupportedException();

    public void SetRecovery(BrandingProjectionRecovery recovery) => throw new NotSupportedException();

    public void AppendAudit(BrandingRecoveryAuditEntry entry) => throw new NotSupportedException();

    public Task Save(CancellationToken ct) => throw new NotSupportedException();
}
