using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;

[Collection(E2ETestCollections.Serial)]
public abstract class AspireSerialSystemTestBase(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await Fixture.ResetToKnownBaseline(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await Fixture.ResetToKnownBaseline(cts.Token);

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

[ExcludeFromCodeCoverage]
[global::SharedKernel.Testing.SerialTestJustification("Clean-slate browser workflows reset shared backend state before and after each test.")]
[CollectionDefinition(E2ETestCollections.Serial, DisableParallelization = true)]
public sealed class AspireSerialSystemTests;
