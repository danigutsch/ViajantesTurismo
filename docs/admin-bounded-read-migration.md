# Migrate to bounded Admin reads

The Admin alpha API replaces five unpaged reads in the active v1 contract.

| Retired GET | Current GET | Response |
| --- | --- | --- |
| `/api/v1/customers` | `/api/v1/customers/page` | Customer summaries in `items` and a `total` |
| `/api/v1/tours` | `/api/v1/tours/page` | Active tour summaries in `items` and a `total` |
| `/api/v1/bookings` | `/api/v1/bookings/page` | Bookings in `items` and overview `counts` |
| `/api/v1/bookings/tour/{tourId}` | `/api/v1/bookings/tour/{tourId}/page` | Tour history in `items` and a `total` |
| `/api/v1/bookings/customer/{customerId}` | `/api/v1/bookings/customer/{customerId}/page` | Customer history in `items` and a `total` |

Read the `items` property instead of parsing a top-level array. Request a one-based
`page` and, when needed, the existing `sort` and `descending` query parameters. For
full customer or tour details, follow the summary's `self` link. The `collection`
link on page items, detail responses, and create responses points to the unfiltered
page. Create responses still return the detail address in both `self` and `Location`.

Use `GetCustomerPage`, `GetTourPage`, `GetBookingPage`, `GetTourBookingPage`, or
`GetCustomerBookingPage` in typed Admin callers. Use `SearchCustomerOptions` or
`SearchTourOptions` when a form needs a choice from a large collection.

Collection POSTs, detail routes and the existing read grants keep their behavior.
The change requires no data migration.
[Alpha compatibility policy](API_VERSIONING.md#alpha-evolution)
