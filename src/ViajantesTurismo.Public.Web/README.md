# ViajantesTurismo.Public.Web

Public-facing website for customer discovery and interactions.

This project follows ADR-020 by keeping public audience concerns separate from internal management workflows. It should
not reference `ViajantesTurismo.Management.Web` or operator-only UI dependencies.

## Current scope

- Every page lives under a culture segment: `/en/...` (default) or `/pt/...`.
    - Landing page at `/{culture}/` with Catalog-backed hero content.
    - Published tour listing at `/{culture}/group-bike-tours`.
    - Published tour detail at `/{culture}/group-bike-tours/{Slug}` with gallery rendering.
    - Photo gallery at `/{culture}/gallery`.
- The URL culture maps to the Catalog content language at the boundary: `pt` requests `pt-BR` content and `en` requests
  `en-US` (`PublicSiteCultures`). The page `lang`, `hreflang` and `og:locale` use the content language.
- Configurable public website theme: Branding colours and fonts are served at `/theme.css` as overrides of the design tokens
  of `PandaBear.SharedKernel.Blazor.Site`, with safe defaults. The content security policy keeps `style-src 'self'`, so the
  host page renders no inline `<style>`.
- `/robots.txt` and `/sitemap.xml`; the sitemap lists every page once per culture with `hreflang` alternates and
  `x-default`.
- Standard service defaults and health endpoints through `MapDefaultEndpoints()`.
- Aspire AppHost registration as `ResourceNames.PublicWebApp`.

## Feature ownership

| Area | Source | Tests in `ViajantesTurismo.Public.WebTests` |
| --- | --- | --- |
| Home | `Components/Pages/Home` owns the localized landing page, published content fallback, and featured tours | `Components/Pages/Home` |
| Gallery | `Components/Pages/Gallery` owns published photo groups, captions, and empty or unavailable states | `Components/Pages/Gallery` |
| Tours | `Components/Pages/Tours` owns published tour listing and detail pages, including links and detail metadata | `Components/Pages/Tours` |
| Shared presentation | `Components/Shared` owns the tour card, rendition mapping, and common page-head rendering | `Components/Shared` and `SearchMetadata` |
| Catalog | `Catalog` owns the anonymous outbound client and its request pipeline. Catalog contracts own the DTOs and client implementation | `Catalog` |
| Branding | `Branding` owns the public client and theme mapping used by the layout and stylesheet | `Branding` |
| Hosting | `Program.cs`, host endpoints, culture and canonical links, cache, security, and the layout span features | `Hosting` and `SearchMetadata` |

The host composes the Catalog and Branding clients through their registration extensions. It also owns anonymous
routing, the local Catalog media proxy, canonical origin, and middleware order. Publication, validation, and locale
content remain in their owning Catalog and shared contracts. Test host and sample tour builders live in the test
project's `Infrastructure` folder.

## Addresses and redirects

| Request | Answer |
| --- | --- |
| A page without a culture (`/`, `/gallery`) | 302 to the remembered or `Accept-Language` culture, else `/en/...` (`Vary: Accept-Language, Cookie`) |
| A former culture query (`/gallery?culture=pt-BR`, `?language=en`) | 301 to `/pt/gallery` or `/en/gallery` |
| A culture in another spelling (`/EN/gallery`) | 301 to `/en/gallery` |
| `/Error`, `/health`, `/alive`, `/catalog/media/...` and files (`/robots.txt`, `/theme.css`) | Not localized |

## SharedKernel building blocks

- `PandaBear.SharedKernel.Blazor.Site`: `SiteHeader`, `SiteFooter`, `LanguageSwitcher`, `Hero`, `Section`,
  `Card`/`CardGrid`, `ButtonLink`, `PageHead` and `site.css`.
- `PandaBear.SharedKernel.AspNetCore.Localization`: `UseCulturePathRedirect`, `PathCultureRequestCultureProvider`, `CulturePath`.
- `PandaBear.SharedKernel.AspNetCore.Seo`: `MapRobotsTxt`, `SitemapXmlSerializer` with `SitemapAlternate`.
- `PandaBear.SharedKernel.AspNetCore`: `UsePermanentRedirects` for the former culture query addresses.
- `PandaBear.SharedKernel.HttpCaching.AspNetCore`: `UseDefaultPublicCaching` makes successful anonymous pages public for
  60 seconds; failures, the not-found page and `/Error` are `no-store`.

`TourImageRenditions` maps Catalog images to the site's named media route and supplies responsive sources to the
shared hero and image grid. Tour cards use `Card` with one JPEG rendition of at least 640 pixels when available.

The layout uses the shared skip link styling with a target on the current localized route.
See [search metadata and web accessibility](../../docs/testing/WEB_QUALITY.md) for the checks.
