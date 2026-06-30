# ViajantesTurismo.Admin.SystemTests

End-to-end browser tests using [Playwright for .NET](https://playwright.dev/dotnet/) against the full stack
(Blazor, API, PostgreSQL, Redis) managed by Aspire.

## Setup

Install Playwright browsers (first time only):

```powershell
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

On supported Ubuntu versions in CI, the repository wrapper installs browser OS dependencies too:

```bash
bash scripts/install-playwright.sh
```

On unsupported Linux distributions such as Ubuntu 25.10, do not use
`install --with-deps`; Playwright falls back to an older Ubuntu dependency set
that may request unavailable packages.

On those distributions, install the browser runtime packages manually before
running the tests:

```bash
sudo apt-get update
sudo apt-get install -y libnspr4 libnss3 libasound2t64
```

## Running

```powershell
# All E2E tests (headless)
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests

# Visible browser
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests -- Playwright.LaunchOptions.Headless=false

# Slow motion (500ms between actions)
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests -- Playwright.LaunchOptions.Headless=false Playwright.LaunchOptions.SlowMo=500

# Specific test
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests --filter-method "*TourCrudTests.CreateTour*"
```

## Notes

- `AspireSystemTestFixture` launches the AppHost-managed stack once for the assembly; each test gets a fresh `BrowserContext`.
- Tests seed their own data and don't depend on each other.
- `Shared.NavigationTests` is the canonical first parallel-safe Aspire-hosted migration slice for narrow reliability checks.
- Blazor Server uses SignalR — use `WaitUntil = WaitUntilState.NetworkIdle` and web-first assertions
  for async rendering.
- Call `await Page.PauseAsync()` to open Playwright Inspector mid-test.

Repeat the migration slice locally with:

```bash
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests/ViajantesTurismo.Admin.SystemTests.csproj --filter-class ViajantesTurismo.Admin.SystemTests.Shared.NavigationTests
```

## Parallel safety guidance

- Default to `AspireSystemTestBase<TFixture>` when a test owns its own data and does not require DB resets.
- Use `AspireSerialSystemTestBase` when a test needs strict baseline isolation (for example exact counts or
  destructive shared-state operations). The serial base owns the reset automatically; test bodies should not call
  reset helpers directly.
- Current intentional browser serial cases are narrow: empty-state smoke checks that must verify a real clean database
  response after destructive reset.
- Prefer API-assisted setup (`ApiTestExtensions`) and navigate by known IDs (`/tours/{id}`, `/bookings/{id}`).
- Make assertions deterministic: prefer known routes, hrefs, unique identifiers,
  explicit search/filter state, or other stable semantic locators.
- For grid assertions, avoid assuming page 1 and avoid scanning pages until a row is found.
  If the behavior under test is not pagination, constrain the dataset or navigate directly by known ID.
- Prefer asserting semantic state (status text, details page values) over fragile CSS-class-only checks.
- For async upload or wizard flows, wait on the first stable user-visible state transition (for example
  success/warning text or an enabled primary action) before asserting secondary badges or decorative UI.

## Specialized helper classes

The E2E project uses a small set of specialized helper classes to keep tests readable
without hiding the behavior under test.

Common helper categories include:

- **API setup helpers** for owned test data and server-side state changes
- **Workflow helpers** for repeated multi-step UI flows
- **Page/list helpers** for deterministic interaction with specific screens or grids
- **Locator helpers** for shared semantic Playwright selectors

These helpers are intentionally lightweight. The README should make contributors aware
that these helper types exist, but it should not become a growing catalog of every helper class.
If you need a helper, look for an existing class in the project and extend it when the abstraction fits.

## Helper usage rules

- Prefer a dedicated helper class over private helper methods when the logic can apply in more than one test.
- Do not append the `Async` suffix to helper method names in this project.
- Keep scenario assertions in the test body whenever they explain why the test exists.
- Use direct route navigation by known IDs whenever the list/grid is not the behavior under test.
- If a helper would only hide unstable lookup or page scanning, fix the test design instead.

## See Also

- [tests/README.md](../README.md) — Running tests, coverage, conventions
- [Playwright Docs](https://playwright.dev/dotnet/docs/intro)
