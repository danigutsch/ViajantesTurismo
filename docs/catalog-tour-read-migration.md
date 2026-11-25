# Catalog tour read migration

The Catalog alpha API uses the bounded tour reads below. Update HTTP clients and generated models together.

| Previous request | Current request | Successful response |
| --- | --- | --- |
| `GET /api/v1/catalog/tours` | `GET /api/v1/catalog/tours/page?page=1&sort=title&descending=false` | `items` and `total`, with up to 10 tour summaries |
| `GET /api/v1/public/catalog/tours` | `GET /api/v1/public/catalog/tours/browse/page?page=1&pageSize=12` | `tours`, `page`, `pageSize`, and `total`, with at most one cover per tour |
| `GET /api/v1/public/catalog/tours/{slug}` | `GET /api/v1/public/catalog/tours/{slug}?page=1` | `tour`, `page`, `pageSize`, and `total` |
| `GET /api/v1/public/catalog/tours/{slug}/photo-page` | `GET /api/v1/public/catalog/tours/{slug}?page=1` | The same paged tour details |

The detail response puts the cover and up to six thumbnails in `tour.images`. Its `total` counts thumbnails
excluding the cover. An omitted `page` requests the first page. Unavailable public tours and invalid or out-of-range
public page numbers return HTTP 404. Public tour browse and detail responses use `Cache-Control: no-store`.
Use a tour card's `self` link for details and a detail's `collection` link for the public browse page.

Use `ICatalogToursApiClient.GetTourPage` for the management list and `GetTour` for a single management record.
Use `IPublicCatalogApiClient.GetPublishedTourPage` for public browsing and `GetPublishedTourDetailsPage` for a tour.
The [Catalog OpenAPI documents](../src/ViajantesTurismo.Catalog.Contracts.Http/OpenApi/) include the schemas and
parameters for each operation. Direct public media requests still require an eligible image associated with a
visible tour. This change requires no data migration.

See [API compatibility](API_COMPATIBILITY.md) and [API versioning](API_VERSIONING.md) for the alpha contract policy.
