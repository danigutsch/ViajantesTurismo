# Integration/E2E Test Fixture Seam Best Practices

## Unified Fixture Interface

```csharp
public interface IAdminTestHost : IAsyncLifetime, IDisposable
{
    HttpClient Client { get; }
    Uri BaseUri { get; }
    Task Seed();
    Task Reset();
}
```

- No "Async" suffix for test-only helpers.
- All test wiring unified on this seam.

## Usage Pattern

```csharp
public class BookingTests(ApiFixture fixture)
{
    [Fact]
    public async Task Can_GetTours_Smoke()
    {
        var resp = await fixture.Client.GetAsync(new Uri("/tours", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
```

- Prefer constructor injection from the assembly fixture over base classes.
- Do not also implement `IClassFixture<ApiFixture>` when assembly fixture wiring is used.
- All helpers come from the fixture, never from new/scattered clients.

## xUnit Wiring

- Prefer `[assembly: AssemblyFixture(typeof(ApiFixture))]` (in one `.cs` per test assembly).
- Eliminate `[Collection]` and `[CollectionDefinition]` unless true cross-assembly serial resources are needed.

## Fixture Implementation Musts

- Expose `HttpClient Client { get; }` (from `CreateClient()` in WebApplicationFactory or equivalent).
- Expose `Uri BaseUri { get; }` for constructing relative URIs.
- Implement `Task Seed()` and `Task Reset()` for DB/test-data prep & teardown.
- Implement xUnit's `IAsyncLifetime`, `DisposeAsync` for infra lifecycle.

## Guidelines

- All test helpers (seeding, reset, client) MUST come from the injected fixture.
- Never append `Async` to test plumbing methods (except infrastructure frameworks requiring it, e.g., DisposeAsync).
- Add new helpers to the fixture and keep the interface up to date.

## Filtering

- Prefer MTP trait filters for organization and targeted runs.
- Example: `dotnet test --project tests/ViajantesTurismo.Admin.IntegrationTests --filter-trait "Category=smoke"`.

## References

- xUnit: <https://xunit.net/docs/shared-context>
- Microsoft: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests>
- Aspire samples: <https://github.com/dotnet/aspire-samples>
- Community pattern: Stephen Cleary, Martin Fowler
