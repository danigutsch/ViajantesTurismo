# ViajantesTurismo Integration & E2E Test Guide

## Test Fixture Seam

All tests must use the unified test fixture seam:

```csharp
public interface ITestHost : IAsyncLifetime, IDisposable
{
    HttpClient Client { get; }
    Uri BaseUri { get; }
    Task Seed();
    Task Reset();
}
```

### Using the Fixture in Tests

```csharp
public class UserTests : IClassFixture<ApiFixture> {
    private readonly ApiFixture _fixture;
    public UserTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Can_SmokeTheAPI() {
        var url = new Uri(_fixture.BaseUri, "/api/users");
        var resp = await _fixture.Client.GetAsync(url);
        // ...assertions...
    }
}
```

- All test helpers (seed, reset) must come from the fixture. No Async suffix for test-only helpers.
- Prefer fixture injection; only use base class if many helpers are needed.

### Fixture Implementation Responsibilities

- Expose `HttpClient Client { get; }` (uses WebApplicationFactory or similar).
- Provide `Uri BaseUri { get; }` (points at running SUT).
- Implement `Seed()` and `Reset()` using proper data lifecycle routines.
- Use only `[assembly: AssemblyFixture(typeof(ApiFixture))]` for xUnit-based wiring.

### Why?

- Ensures all test code is portable, parallel-safe, and ready for modern .NET/Aspire scenarios.
- Consistent with xUnit, Microsoft, Aspire, and test community best practices.

### References

- tests/TEST_FIXTURE_SEAM.md (this repo)
- <https://xunit.net/docs/shared-context>
- <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests>
- <https://github.com/dotnet/aspire-samples>
