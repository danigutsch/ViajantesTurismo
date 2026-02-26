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

## See Also

- [tests/README.md](../README.md) — Running tests, coverage, conventions
- [Playwright Docs](https://playwright.dev/dotnet/docs/intro)
