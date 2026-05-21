# Assembly-wide Test Fixtures in xUnit

- Use `[assembly: AssemblyFixture(typeof(ApiFixture))]` for integration tests in this repo.
  - Place this attribute in any `.cs` file in the test assembly (already in AdminApiIntegrationTestBase.cs).
  - This assigns the ApiFixture to all tests in the assembly, so [Collection] is not required on each class.
- xUnit does not support sharing collection fixtures across assemblies; each test project/assembly must have its own fixture.
- To control collection scope, see xUnit's [CollectionBehavior] attribute.

# TODO: Named/Permanent Container Support

- Tests currently use ephemeral test containers (
  e.g., database, Redis) per run.
- To allow container/service reuse across test assemblies, research best practices for Aspire/distributed test containers.
- Track in: issue-permanent-named-container-support.md
