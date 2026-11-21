# Admin system tests

These tests live in `ViajantesTurismo.HostedTests` and use the `ViajantesTurismo.HostedTests.Admin.System` namespace.

System browser tests using [Playwright for .NET](https://playwright.dev/dotnet/) against the full stack
(Blazor, API, PostgreSQL, Redis) managed by Aspire.

## Setup

Install Playwright browsers (first time only):

```powershell
dotnet build
pwsh bin/Debug/net11.0/playwright.ps1 install
```

On supported Ubuntu versions, `sharedkernel-ci` installs browser OS dependencies too, as CI does.
On Ubuntu 26.04, first set `PLAYWRIGHT_HOST_PLATFORM_OVERRIDE=ubuntu24.04-x64` (or `ubuntu24.04-arm64`),
because Playwright 1.60 has no Ubuntu 26.04 manifests yet:

```bash
dotnet tool run sharedkernel-ci -- install-playwright --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj \
    --browser chromium --with-dependencies
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
# All system tests (headless)
dotnet test --project tests/ViajantesTurismo.HostedTests --filter-namespace "ViajantesTurismo.HostedTests.Admin.System*"

# Visible browser
dotnet test --project tests/ViajantesTurismo.HostedTests --filter-namespace "ViajantesTurismo.HostedTests.Admin.System*" -- Playwright.LaunchOptions.Headless=false

# Slow motion (500ms between actions)
dotnet test --project tests/ViajantesTurismo.HostedTests --filter-namespace "ViajantesTurismo.HostedTests.Admin.System*" -- Playwright.LaunchOptions.Headless=false Playwright.LaunchOptions.SlowMo=500

# Specific test
dotnet test --project tests/ViajantesTurismo.HostedTests --filter-method "*TourCrudTests.CreateTour*"
```

## Notes

- `AspireSystemTestFixture` borrows the system stack cached by the assembly's `HostedTestFixture`; each test gets a fresh
  `BrowserContext` and fresh authenticated API clients that it owns and disposes.
- Tests seed their own data and don't depend on each other.
- `Shared.NavigationTests` is the canonical first parallel-safe Aspire-hosted migration slice for narrow reliability checks.
- `ViajantesTurismo.Management.Web` prerenders routes but keeps the layout inert until the
  Interactive Server circuit is available. Navigation uses `WaitUntil = WaitUntilState.DOMContentLoaded`
  and retries a circuit startup only while the layout remains inert. Then wait for the semantic
  heading, link, form control, or state under test. Do not use `NetworkIdle`, hydration sentinels,
  JavaScript readiness probes, or fixed delays as a Blazor readiness signal.
- Call `await Page.PauseAsync()` to open Playwright Inspector mid-test.

Repeat the migration slice locally with:

```bash
dotnet test --project tests/ViajantesTurismo.HostedTests \
  --filter-class ViajantesTurismo.HostedTests.Admin.System.Shared.NavigationTests
```

## Parallel safety guidance

- Default to `AspireSystemTestBase<TFixture>` when a test owns its own data and does not require DB resets.
- No serial system-test base or serial browser tests currently exist. Keep scenarios parallel-safe through owned data
  instead of destructive shared-state resets.

## Reproduce Management Tour schedule validation

From the repository root, install the pinned SDK, stage the package feed, and follow the [System browser
setup](#setup) for Chromium and the [HostedTests prerequisites](../../README.md) for the Aspire containers.
Run this fixture against the local test stack.

```sh
dotnet test --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj \
  -c Release -- --filter-class '*TourEditReproductionTests'
```

Each outcome runs twice. Every attempt seeds a uniquely named synthetic tour through Admin API and opens a fresh browser
context. The protected edit route challenges the anonymous browser. Signing in through the fixture's conformance
realm exposes the labelled Tour form. A valid schedule change reaches Tour Details and persists the new name
and end date. A short schedule shows the duration validation message and leaves the stored name and end date intact.

Set `VIAJANTESTURISMO_REPRODUCTION_MANIFESTS=1` for the same command to keep four validated manifests under
`TestResults/reproduction/` under the scenario `browser.tour-edit-duration`. Each records its source revision,
an opaque attempt ID, timing, and an approved state
for `management-web` and `admin-api`. The outcome codes are `edit-persisted` and `short-duration-rejected`.
The fixture leaves `traceIds` empty because it has no server-side trace observation. The manifest excludes
credentials, records, route parameters, screenshots, and browser state. The fixture exercises this named
Tour workflow with the test realm only. [Reproduction artifact contract](../../../../docs/testing/reproduction-contract.md)
- Prefer API-assisted setup (`ApiTestExtensions`) and navigate by known IDs (`/tours/{id}`, `/bookings/{id}`).
- Make assertions deterministic: prefer known routes, hrefs, unique identifiers,
  explicit search/filter state, or other stable semantic locators.
- For grid assertions, avoid assuming page 1 and avoid scanning pages until a row is found.
  If the behavior under test is not pagination, constrain the dataset or navigate directly by known ID.
- Prefer asserting semantic state (status text, details page values) over fragile CSS-class-only checks.
- For async upload or wizard flows, wait on the first stable user-visible state transition (for example
  success/warning text or an enabled primary action) before asserting secondary badges or decorative UI.

## Specialized helper classes

The system-test project uses a small set of specialized helper classes to keep tests readable
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

- [tests/README.md](../../../README.md) — Running tests, coverage, conventions
- [Playwright Docs](https://playwright.dev/dotnet/docs/intro)
