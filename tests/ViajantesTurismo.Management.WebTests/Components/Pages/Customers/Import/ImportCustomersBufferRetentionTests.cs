using PandaBear.Testing;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Import;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class ImportCustomersBufferRetentionTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(8, true)]
    public async Task Pending_conflicts_retain_one_upload_buffer_per_circuit(int circuitCount, bool remapHeaders)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        using var circuits = new ConcurrentImportCustomersBufferScenario(circuitCount, remapHeaders);

        // Act
        await circuits.ConfirmConcurrently(ct);
        var retainedBytes = circuits.RetainedCsvBytes();
        output.WriteLine($"Circuits={circuitCount} remap={remapHeaders} retained={retainedBytes.Sum():N0} bytes " +
            $"per circuit=[{string.Join(", ", retainedBytes)}]");

        // Assert
        circuits.AwaitingConflictResolution.ShouldBeTrue();
        var expectedBytes = (int)AdminContractConstants.CustomerImportMaxFileBytes;
        retainedBytes.ShouldBe([.. Enumerable.Repeat(expectedBytes, circuitCount)]);
    }
}
