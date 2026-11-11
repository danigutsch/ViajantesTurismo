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

## Addresses and redirects

| Request | Answer |
| --- | --- |
| A page without a culture (`/`, `/gallery`) | 302 to the remembered or `Accept-Language` culture, else `/en/...` (`Vary: Accept-Language, Cookie`) |
| A former culture query (`/gallery?culture=pt-BR`, `?language=en`) | 301 to `/pt/gallery` or `/en/gallery` |
| A culture in another spelling (`/EN/gallery`) | 301 to `/en/gallery` |
| `/Error`, `/health`, `/alive`, `/catalog/media/...` and files (`/robots.txt`, `/theme.css`) | Not localized |

## SharedKernel building blocks

- `PandaBear.SharedKernel.Blazor.Site`: `SkipLink`, `SiteHeader`, `SiteFooter`, `LanguageSwitcher`, `Hero`, `Section`,
  `Card`/`CardGrid`, `ButtonLink`, `PageHead` and `site.css`.
- `PandaBear.SharedKernel.AspNetCore.Localization`: `UseCulturePathRedirect`, `PathCultureRequestCultureProvider`, `CulturePath`.
- `PandaBear.SharedKernel.AspNetCore.Seo`: `MapRobotsTxt`, `SitemapXmlSerializer` with `SitemapAlternate`.
- `PandaBear.SharedKernel.AspNetCore`: `UsePermanentRedirects` for the former culture query addresses.
- `PandaBear.SharedKernel.HttpCaching.AspNetCore`: `UseDefaultPublicCaching` makes successful anonymous pages public for
  60 seconds; failures, the not-found page and `/Error` are `no-store`.

Catalog images keep a local `ResponsiveTourImage` (`<picture>` with AVIF, WebP and JPEG `srcset`), because the package's
`Hero`, `Card` and `ImageGrid` render a single `src`. Tour cards use `Card` with one JPEG rendition of at least 640 pixels.
