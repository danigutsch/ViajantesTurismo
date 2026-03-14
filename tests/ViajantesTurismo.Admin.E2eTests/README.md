# ViajantesTurismo.Admin.E2ETests

End-to-end browser tests using [Playwright for .NET](https://playwright.dev/dotnet/) against the full stack
(Blazor, API, PostgreSQL, Redis) managed by Aspire.

## Setup

Install Playwright browsers (first time only):

```powershell
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

## Running

```powershell
# All E2E tests (headless)
dotnet test --project tests/ViajantesTurismo.Admin.E2ETests

# Visible browser
dotnet test --project tests/ViajantesTurismo.Admin.E2ETests -- Playwright.LaunchOptions.Headless=false

# Slow motion (500ms between actions)
dotnet test --project tests/ViajantesTurismo.Admin.E2ETests -- Playwright.LaunchOptions.Headless=false Playwright.LaunchOptions.SlowMo=500

# Specific test
dotnet test --project tests/ViajantesTurismo.Admin.E2ETests --filter-method "*TourCrudTests.CreateTour*"
```

## Notes

- `E2eFixture` launches infrastructure once per session; each test gets a fresh `BrowserContext`.
- Tests seed their own data and don't depend on each other.
- Blazor Server uses SignalR — use `WaitUntil = WaitUntilState.NetworkIdle` and web-first assertions
  for async rendering.
- Call `await Page.PauseAsync()` to open Playwright Inspector mid-test.

## Parallel safety guidance

- Default to `E2ETestBase` when a test owns its own data and does not require DB resets.
- Use `E2ESerialTestBase` when a test needs strict baseline isolation (for example exact counts,
  per-test `ClearDatabase`, or destructive shared-state operations).
- Prefer API-assisted setup (`ApiTestHelper`) and navigate by known IDs (`/tours/{id}`, `/bookings/{id}`).
- Make assertions deterministic: prefer known routes, hrefs, unique identifiers,
  explicit search/filter state, or other stable semantic locators.
- For grid assertions, avoid assuming page 1 and avoid scanning pages until a row is found.
  If the behavior under test is not pagination, constrain the dataset or navigate directly by known ID.
- Prefer asserting semantic state (status text, details page values) over fragile CSS-class-only checks.

## See Also

- [tests/README.md](../README.md) — Running tests, coverage, conventions
- [Playwright Docs](https://playwright.dev/dotnet/docs/intro)
