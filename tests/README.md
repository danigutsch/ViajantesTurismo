# ViajantesTurismo Integration & E2E Test Guide

## Test Fixture Seam

All tests must use the unified test fixture seam:

```csharp
public interface IAdminTestHost : IAsyncDisposable, IDisposable
{
    HttpClient Client { get; }
    Uri BaseUri { get; }
    Task Seed(CancellationToken cancellationToken = default);
    Task Reset(CancellationToken cancellationToken = default);
}
```

### Using the Fixture in Tests

```csharp
public class UserTests(ApiFixture fixture)
{
    [Fact]
    public async Task Can_SmokeTheAPI()
    {
        var resp = await fixture.Client.GetAsync(new Uri("/tours", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
```

- All test helpers (seed, reset) must come from the fixture. No Async suffix for test-only helpers.
- Prefer fixture injection; only use base class if many helpers are needed.

### Fixture Implementation Responsibilities

- Expose `HttpClient Client { get; }` (uses WebApplicationFactory or similar).
- Provide `Uri BaseUri { get; }` (points at running SUT).
- Implement `Seed()` and `Reset()` using proper data lifecycle routines.
- Use `[assembly: AssemblyFixture(typeof(ApiFixture))]` as the canonical wiring pattern.
- Do not combine assembly fixture registration with `IClassFixture<ApiFixture>` in the same test class.

### Why?

- Ensures all test code is portable, parallel-safe, and ready for modern .NET/Aspire scenarios.
- Consistent with xUnit, Microsoft, Aspire, and test community best practices.

### Filtering

- Prefer MTP trait filters for fast slices, e.g. `dotnet test --project tests/ViajantesTurismo.Admin.IntegrationTests --filter-trait "Category=smoke"`.
- Combine traits when needed, e.g. `--filter-trait "Scope=integration" --filter-trait "Area=bookings"`.

### References

- tests/TEST_FIXTURE_SEAM.md (this repo)
- <https://xunit.net/docs/shared-context>
- <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests>
- <https://github.com/dotnet/aspire-samples>
