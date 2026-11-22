namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Documents;

/// <summary>Runs the document scenarios in an Admin stack with its own mutation rate-limit budget.</summary>
public sealed class DocumentApiFixture(HostedTestFixture shared) : ApiFixture(shared)
{
    protected override string Stack => HostedTestFixture.DocumentApiStack;
}
