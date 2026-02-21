# ViajantesTurismo.Admin.E2ETests

End-to-end UI tests for the ViajantesTurismo Admin application using
[Playwright for .NET](https://playwright.dev/dotnet/), Aspire-managed infrastructure, and xUnit v3.

## Purpose

Browser-driven tests that exercise the full stack — Blazor Server frontend, API service, PostgreSQL, and Redis — to
verify user-facing workflows end-to-end. The `E2eFixture` spins up real infrastructure via Aspire and both the API and
Web apps via `WebApplicationFactory` on real Kestrel; Playwright drives Chromium against the Web app's URL.

## Technology

- **Framework**: [Playwright for .NET](https://playwright.dev/dotnet/) via `Microsoft.Playwright.Xunit.v3`
- **Test Runner**: xUnit v3 with Microsoft Testing Platform (MTP)
- **Infrastructure**: .NET Aspire for PostgreSQL and Redis containers
- **Pattern**: Page Object Model with user-facing locators (`GetByRole`, `GetByLabel`, `GetByText`)

## Setup

Install Playwright browsers after building (first time only):

```powershell
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

For CI, use `--with-deps` to also install OS-level dependencies on Linux.

## Running Tests

```powershell
# All E2E tests (headless)
dotnet test

# With visible browser for debugging
dotnet test -- Playwright.LaunchOptions.Headless=false

# Specific test
dotnet test --filter "FullyQualifiedName~TourCrudTests.CreateTour"

# Slow motion (500ms between actions)
dotnet test -- Playwright.LaunchOptions.Headless=false Playwright.LaunchOptions.SlowMo=500
```

## Debugging

- **Headed mode**: `-- Playwright.LaunchOptions.Headless=false`
- **Slow-mo**: `-- Playwright.LaunchOptions.SlowMo=500`
- **Pause (Inspector)**: Call `await Page.PauseAsync()` in a test
- **Trace Viewer**: Record traces with `await Context.Tracing.StartAsync(...)`, then view with
  `pwsh playwright.ps1 show-trace trace.zip`

## Notes

- Infrastructure (PostgreSQL, Redis) is launched once per test session via `E2eFixture`.
- Each test gets a fresh `BrowserContext` (isolated session) with its own `Page`.
- Tests seed their own data through the fixture and do not depend on other tests.
- Blazor Server uses SignalR — use `WaitUntil = WaitUntilState.NetworkIdle` on `GotoAsync` and web-first assertions
  (`Expect(...).ToBeVisibleAsync()`) to handle async rendering.

## Related Documentation

- [Test Guidelines](../../docs/TEST_GUIDELINES.md) — Project-wide testing strategy
- [Playwright Docs](https://playwright.dev/dotnet/docs/intro) — Playwright for .NET guide
