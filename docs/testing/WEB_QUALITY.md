# Search metadata and web accessibility

## Public search metadata

Search and sharing markup is rendered on the server. Verify it with the existing Public Web HTTP
suite. The tests use a synthetic catalog and a configured canonical origin.

| Page | Metadata |
| --- | --- |
| Home | Published SEO title and description, with the localized hero title and body as fallbacks. Built-in English and Brazilian Portuguese text covers missing Catalog content. |
| Group bike tours | Localized list title and description. |
| Tour detail | Published Catalog SEO title and description, with the tour title and summary as fallbacks. A published cover supplies the sharing image when available. |
| Gallery | Localized gallery title and description. |

Canonical URLs, language alternates, the sitemap advertised in `robots.txt`, and sitemap entries
use the configured Public Web origin. Each language has a distinct URL with reciprocal `hreflang`
and an `x-default` for language negotiation. The sitemap includes stable public pages and canonical
slugs from the published Catalog list, within the protocol size limit. Missing tours return HTTP
404 and unavailable tours return HTTP 503. Both omit published page metadata.
[Google canonical URL guidance](https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls)
and [robots.txt guidance](https://developers.google.com/search/docs/crawling-indexing/robots/intro).

Run the deterministic markup checks with the following command. Lighthouse can support an advisory
review of a trusted local page in an approved pinned environment. Browser settings and run conditions
affect its scores. [Chrome Lighthouse documentation](https://developer.chrome.com/docs/lighthouse/overview).

```bash
dotnet test --project tests/ViajantesTurismo.Public.WebTests/ViajantesTurismo.Public.WebTests.csproj
```

Published tour detail pages emit one Schema.org `TouristTrip` JSON-LD data block with the visible
title, summary and localized canonical URL. The HTTP tests parse the rendered document and verify
those fields in English and Portuguese. They also verify that other public pages and missing or
unavailable tours omit the tour data block. The itinerary stays visible as text until the public
contract supplies ordered places. Evaluate `BreadcrumbList` when a visible breadcrumb trail
follows the site hierarchy. Search-result treatments depend on the search provider.
[Schema.org TouristTrip](https://schema.org/TouristTrip) and
[Google breadcrumb markup](https://developers.google.com/search/docs/appearance/structured-data/breadcrumb).

## Web accessibility

Run the existing Playwright system tests for specific keyboard outcomes on authenticated
Management pages. The local browser hosts run with synthetic authentication and fake API clients.
The Public Web suite guards language, navigation labels, skip link targets and content landmarks
on its server-rendered pages.

```bash
dotnet test --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj \
  --filter-namespace ViajantesTurismo.HostedTests.Management.Browser
```

The Management browser tests check that the skip link reaches the current form, that customer
wizard steps have a page heading and an ordered step heading, and that country selectors can be
searched and used by keyboard. The country selector keeps address codes separate from nationality
names. The deployed selector catalog covers the 249 assigned codes in SharedKernel and also
contains Kosovo for nationality. Nineteen of the assigned codes have different English labels in
the two sources. Management Web owns the presentation catalog and nationality aliases.
SharedKernel validates assigned codes. The decorative flag class uses the lowercase country code
of the pinned flag stylesheet. [Country and currency ownership](../architecture/README.md#country-and-currency-reference-data).

Use a synthetic published tour and synthetic customer data for these manual checks. Record the route,
input method and observable failure. [W3C quick accessibility checks](https://www.w3.org/WAI/test-evaluate/preliminary/).

| Area | Review |
| --- | --- |
| Navigation | Tab to the skip link and activate it. Confirm useful focus on the new route and a logical heading and landmark order. Switch languages and listen for the expected pronunciation. |
| Forms | Read the accessible label and current value. Trigger a required-field failure, follow the error summary link and confirm focus on the invalid control. |
| Focus and contrast | Check visible focus in desktop and compact layouts. Test text, controls and states against each active theme. |
| Dynamic content | Check how loading, success and error status changes are announced. Use Escape to cancel a dropdown or dialog and confirm focus returns to its trigger. |
| Media and uploads | Check informative image alternatives, decorative image treatment, upload progress, cancellation and retry with a keyboard and screen reader. |

The hosted browser check runs the pinned IBM Equal Access engine 4.0.35 with its WCAG 2.2 A/AA ruleset.
It scans the English public tour list at `/en/group-bike-tours` with a synthetic published tour and
the authenticated customer form at `/customers/create/personal-info`. A missing alternative on a
synthetic image proved detection in Chromium. The smoke tests report confirmed violations by route and
rule ID. Keep the targeted Playwright assertions and the manual keyboard and screen-reader checks above.
[Engine usage](https://github.com/IBMa/equal-access/blob/4.0.35/accessibility-checker-engine/README.md)

The archive declares Apache-2.0 and no runtime package dependencies. The browser bundle includes its
own Apache-2.0 notice. CI checks the archive digest before extracting only the browser bundle. The
SharedKernel Playwright helper also verifies the bundle digest before executing it. The engine remains
a test input. [Pinned release](https://github.com/IBMa/equal-access/releases/tag/4.0.35)

To run the same two routes locally, provision the verified test bundle and run this class through the
existing hosted harness.

```bash
(
  set -euo pipefail
  archive="$(mktemp)"
  bundle="$(mktemp)"
  trap 'rm -f -- "$archive" "$bundle"' EXIT
  curl --fail --silent --show-error --location --proto '=https' --max-time 60 --retry 3 \
    --output "$archive" \
    https://registry.npmjs.org/accessibility-checker-engine/-/accessibility-checker-engine-4.0.35.tgz
  printf '%s  %s\n' 'e0d00253f68180a1064fa36467012fe8ed1682fffa0b2072b66a9cacaaff702e' "$archive" \
    | sha256sum --check --strict
  tar -xOzf "$archive" package/ace.js > "$bundle"
  VT_WCAG_ENGINE_PATH="$bundle" dotnet test \
    --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj \
    --filter-class '*BrowserAccessibilitySmokeTests'
)
```
