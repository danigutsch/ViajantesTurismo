# Domain Model Audit

Owner: repository maintainers. Reviewed October 24, 2026 at commit `f2337a26`.
Scope: `src/ViajantesTurismo.Admin.Domain`, `src/ViajantesTurismo.Catalog.Domain`, and the application, API, and
Management Web code that calls them. This is a dated evidence record for the rules in the
[rich domain model guide](RICH_DOMAIN_MODEL.md); it also records the Admin and Catalog audits that
[ADR-031](../adr/20260703-rich-domain-behavior-and-state-exposure.md) asked for. Re-run the audit with the
`rich-domain-model` agent skill and update this page rather than adding a new one.

## Contents

- [Summary](#summary)
- [Findings](#findings)
- [Domain enum inventory](#domain-enum-inventory)
- [Follow-up work](#follow-up-work)
- [Related Documentation](#related-documentation)

## Summary

- **Purity**: clean. Both domain projects reference only domain-facing `PandaBear.SharedKernel.*` packages and
  `JetBrains.Annotations`; no EF Core, ASP.NET Core, serializer, DataAnnotations, logging, or HTTP usage. A new
  architecture test now also bans those framework assemblies.
- **Setters**: no non-private setters. Domain state uses `private set`, `private init`, or positional records.
- **Collections**: every exposed collection is a read-only view over a private field.
- **Aggregate boundary**: one leak, fixed in this change. `Booking` exposed `Create` and eight mutators publicly, and
  `Booking.Confirm` skipped the capacity check that `Tour.ConfirmBooking` applies.
- **Behavior placement**: the document decisions repeated across handlers, the non-atomic tour update, the identifier
  limit mismatch, and the unchecked customer mapping are fixed. The remaining handler, Razor, and Catalog findings are
  listed below as follow-ups.

## Findings

| ID | Location | Finding | Rule | Status |
| --- | --- | --- | --- | --- |
| A1 | `src/ViajantesTurismo.Admin.Domain/Tours/Booking.cs:247-526` | Child entity `Booking` exposed `Create` and eight mutators publicly; `Confirm` bypassed the tour capacity check | DDD006 | Fixed: members are `internal`; `DddConventionsTests` guards it |
| A2 | `src/ViajantesTurismo.Admin.Infrastructure/ModelConfigurations/TourConfiguration.cs:18` | The database caps `Tour.Identifier` at 64 characters while `Tour.cs:143` accepts up to 128, so 65 to 128 character identifiers fail at save time | Domain and persistence limits agree | Fixed: identifiers are limited to 64 characters; behavior specifications guard it |
| A3 | `src/ViajantesTurismo.Admin.Application/Mappings/CustomerMapper.cs:53,66,91,103` | Reads `.Value` from unchecked `Result` objects, which throws on invalid input instead of returning a validation result | Result pattern | Fixed: the mappers return `Result<T>` and the handlers collect the errors |
| A4 | `src/ViajantesTurismo.Admin.Application/Mappings/DocumentMapper.cs:36` and `src/ViajantesTurismo.Admin.Application/Documents/GetFinalizedDocumentArtifactHandler.cs:28` | Both decide "finalized artifact available" from the document status and artifact fields | DDD005 | Fixed: `DocumentDraft.HasAvailableFinalizedArtifact` |
| A5 | `src/ViajantesTurismo.Admin.Application/Documents/GenerateContractDraftCommandHandler.cs:47` | Decides contract eligibility from a read DTO's booking status | DDD005 | Follow-up |
| A6 | `src/ViajantesTurismo.Admin.Application/Customers/UpdateCustomer/UpdateCustomerCommandHandler.cs:86-93` | Drives `Customer` through eight setter-style `Update*` calls that return no `Result` | Behavior over state | Follow-up |
| A7 | `src/ViajantesTurismo.Admin.Domain/Customers/Customer.cs:23,58` | `Customer` has public constructors instead of a `Result` factory | ADR-001 | Follow-up |
| A8 | `src/ViajantesTurismo.Admin.Application/Tours/UpdateTour/UpdateTourCommandHandler.cs:36-70` | Six sequential mutator calls stop at the first failure and can leave the in-memory aggregate partly changed | Aggregate consistency | Fixed: `Tour.Revise` validates every value before applying any change |
| A9 | `src/ViajantesTurismo.Admin.Domain/Tours/TourCapacity.cs:43,48` | Hard-codes capacity bounds 1 and 20 instead of using the domain limit constants | Magic numbers | Fixed: uses `AdminDomainLimits.MinTourCustomers` and `MaxTourCustomers` |
| A10 | `src/ViajantesTurismo.Management.Web/Components/Pages/Tours/Index.razor:51,66` | Recomputes `IsFullyBooked` and available spots because the read DTO does not carry the domain facts | DDD005 | Follow-up |
| A11 | `src/ViajantesTurismo.Management.Web/Components/Pages/Bookings/Edit.razor:350` | Decides booking editability from the raw status DTO | DDD007 | Follow-up |
| A12 | `src/ViajantesTurismo.Admin.Application/Documents/*CommandHandler.cs` and `GetFinalizedDocumentArtifactHandler.cs:22` | Seven document handlers repeat `document.Type == DocumentType.PaymentInstructions` to route payment-instruction documents to their own workflow | DDD005, DDD007 | Fixed: `RequiresPaymentInstructionWorkflow` on the draft and lineage |
| A13 | `src/ViajantesTurismo.Admin.Domain/Customers/PhysicalInfo.cs` and `AccommodationPreferences.cs` | Customer preference factories must validate defined enum selections | Always-valid value objects | Fixed: factories return validation failures; domain and import regressions cover invalid selections and valid controls |
| C1 | `src/ViajantesTurismo.Catalog.Application/Tours/CatalogTourPresentationUpdate.cs` | Holds title, slug, and length rules that `CatalogTour.ChangePresentation` does not enforce | Rules in the domain | Follow-up |
| C2 | `src/ViajantesTurismo.Catalog.Domain/Tours/CatalogTour.cs:115,166,179,218` | Throws exceptions for expected business outcomes | ADR-002 | Follow-up |
| C3 | `src/ViajantesTurismo.Catalog.Application/Media/MediaImageOriginalStoredIntegrationHandler.cs:45-66` | Chooses the next media processing status in the handler | Behavior over state | Follow-up |

## Domain enum inventory

Twenty-five public enums live in the domain projects. Thirty branch sites exist outside the domain. Most are seed data,
query ordering, input guards, store filters, or rendering switches; the business decisions were findings A4 and A12, now fixed, and
the document-status checks in `src/ViajantesTurismo.Admin.Application/Payments/PaymentInstructionWorkflow.cs`.

| Category | Enums | Decision |
| --- | --- | --- |
| Stable labels | `BikeType`, `RoomType`, `BedType`, `Currency`, `PaymentMethod`, `DiscountType`, `DocumentType`, `DocumentAudience`, `DocumentPrivacyClassification`, `PublicContentLanguage`, `PaymentAccountKind`, `BrazilianAccountType`, `PaymentAccountVerificationMethod` | Keep as enums |
| Reasons and audit vocabulary | `DocumentAuditOutcome`, `DocumentAuditReasonCode`, `DocumentAuditOperation`, `FinancialReason`, `PaymentAccountReason`, `PaymentAccountReviewOutcome` | Keep as enums |
| Lifecycle states | `BookingStatus`, `PaymentStatus`, `DocumentStatus`, `PaymentAccountStatus`, `MediaImageProcessingStatus`, `PublicContentPublicationState` | Keep public for display, mapping, and filtering; move decisions behind domain predicates |

No enum analyzer is planned: risky usage is concentrated in a few handlers and is covered by review and the audit
(catalog rule DDD007).

## Follow-up work

Remaining follow-ups are tracked as roadmap items: A5 is RM-308, A6 and A7 are RM-309, A10 and A11 are
RM-310, C1 and C2 are RM-311, and C3 is RM-312. Prove each production gap with a failing test first. C1 and C2
move Catalog presentation rules and publication outcomes into `CatalogTour` as `Result` values, which also
changes the Catalog service and API error mapping.

## Related Documentation

Use the [documentation source-of-truth map](../README.md#source-of-truth-map) for related repository guidance.
