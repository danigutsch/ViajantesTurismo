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
- [Related Documentation](#related-documentation)

## Summary

- **Purity**: clean. Both domain projects reference only domain-facing `PandaBear.SharedKernel.*` packages and
  `JetBrains.Annotations`; no EF Core, ASP.NET Core, serializer, DataAnnotations, logging, or HTTP usage. A new
  architecture test also bans those framework assemblies.
- **Setters**: no non-private setters. Domain state uses `private set`, `private init`, or positional records.
- **Collections**: every exposed collection is a read-only view over a private field.
- **Aggregate boundary**: one leak, fixed in this change. `Booking` exposed `Create` and eight mutators publicly, and
  `Booking.Confirm` skipped the capacity check that `Tour.ConfirmBooking` applies.
- **Behavior placement**: the document decisions repeated across handlers, the non-atomic tour update, the identifier
  limit mismatch, and the unchecked customer mapping are fixed. The handler, Razor, and Catalog follow-ups (RM-308
  to RM-312) are fixed too. A second pass on `main` after those fixes found A14 to A16; they are fixed as well,
  and every finding below is closed.

## Findings

| ID | Location | Finding | Rule | Status |
| --- | --- | --- | --- | --- |
| A1 | `src/ViajantesTurismo.Admin.Domain/Tours/Booking.cs:247-526` | Child entity `Booking` exposed `Create` and eight mutators publicly; `Confirm` bypassed the tour capacity check | DDD006 | Fixed: members are `internal`; `DddConventionsTests` guards it |
| A2 | `src/ViajantesTurismo.Admin.Infrastructure/ModelConfigurations/TourConfiguration.cs:18` | The database caps `Tour.Identifier` at 64 characters while `Tour.cs:143` accepts up to 128, so 65 to 128 character identifiers fail at save time | Domain and persistence limits agree | Fixed: identifiers are limited to 64 characters; behavior specifications guard it |
| A3 | `src/ViajantesTurismo.Admin.Application/Mappings/CustomerMapper.cs:53,66,91,103` | Reads `.Value` from unchecked `Result` objects, which throws on invalid input instead of returning a validation result | Result pattern | Fixed: the mappers return `Result<T>` and the handlers collect the errors |
| A4 | `src/ViajantesTurismo.Admin.Application/Mappings/DocumentMapper.cs:36` and `src/ViajantesTurismo.Admin.Application/Documents/GetFinalizedDocumentArtifactHandler.cs:28` | Both decide "finalized artifact available" from the document status and artifact fields | DDD005 | Fixed: `DocumentDraft.HasAvailableFinalizedArtifact` |
| A5 | `src/ViajantesTurismo.Admin.Application/Documents/GenerateContractDraftCommandHandler.cs:47` | Decides contract eligibility from a read DTO's booking status | DDD005 | Fixed: `Tour.CanReceiveCustomerDocuments`; generation and regeneration ask the aggregate |
| A6 | `src/ViajantesTurismo.Admin.Application/Customers/UpdateCustomer/UpdateCustomerCommandHandler.cs:86-93` | Drives `Customer` through eight setter-style `Update*` calls that return no `Result` | Behavior over state | Fixed: `Customer.Revise` validates every value before applying any change |
| A7 | `src/ViajantesTurismo.Admin.Domain/Customers/Customer.cs:23,58` | `Customer` has public constructors instead of a `Result` factory | ADR-001 | Fixed: `Customer.Create` returns `Result<Customer>`; `DddConventionsTests` rejects public entity constructors |
| A8 | `src/ViajantesTurismo.Admin.Application/Tours/UpdateTour/UpdateTourCommandHandler.cs:36-70` | Six sequential mutator calls stop at the first failure and can leave the in-memory aggregate partly changed | Aggregate consistency | Fixed: `Tour.Revise` validates every value before applying any change |
| A9 | `src/ViajantesTurismo.Admin.Domain/Tours/TourCapacity.cs:43,48` | Hard-codes capacity bounds 1 and 20 instead of using the domain limit constants | Magic numbers | Fixed: uses `AdminDomainLimits.MinTourCustomers` and `MaxTourCustomers` |
| A10 | `src/ViajantesTurismo.Management.Web/Components/Pages/Tours/Index.razor:51,66` | Recomputes `IsFullyBooked` and available spots because the read DTO does not carry the domain facts | DDD005 | Fixed: `GetTourDto` carries `AvailableSpots`, `IsFullyBooked`, and `IsAtMinimumCapacity` from `Tour` |
| A11 | `src/ViajantesTurismo.Management.Web/Components/Pages/Bookings/Edit.razor:350` | Decides booking editability from the raw status DTO | DDD007 | Fixed: `GetBookingDto.CanModifyBookedServices` comes from `Booking` |
| A12 | `src/ViajantesTurismo.Admin.Application/Documents/*CommandHandler.cs` and `GetFinalizedDocumentArtifactHandler.cs:22` | Seven document handlers repeat `document.Type == DocumentType.PaymentInstructions` to route payment-instruction documents to their own workflow | DDD005, DDD007 | Fixed: `RequiresPaymentInstructionWorkflow` on the draft and lineage |
| A13 | `src/ViajantesTurismo.Admin.Domain/Customers/PhysicalInfo.cs` and `AccommodationPreferences.cs` | Customer preference factories must validate defined enum selections | Always-valid value objects | Fixed: factories return validation failures; domain and import regressions cover invalid selections and valid controls |
| A14 | `src/ViajantesTurismo.Admin.Application/Payments/GeneratePaymentInstructionsCommandHandler.cs:63` | Repeats `lineage.Type != DocumentType.PaymentInstructions`, a call site that finding A12 missed | DDD005, DDD007 | Fixed: uses `DocumentLineage.RequiresPaymentInstructionWorkflow` |
| A15 | `src/ViajantesTurismo.Admin.Application/Payments/PaymentInstructionWorkflow.cs:64,94,173,237` | Decides from the raw `DocumentStatus` whether approval begins review, whether withdrawing approval voids or requests changes, whether a revision is finalized for download, and whether it is retired | DDD005, DDD007 | Fixed: `DocumentDraft.CanBeginReview`, `IsFinalized`, `IsRetired`, and `DocumentLineage.WithdrawApproval` |
| A16 | `src/ViajantesTurismo.Management.Web/Components/Pages/Bookings/BookingEditActionPanel.razor` | Always shows Delete Booking, although `Tour.RemoveBooking` removes only pending bookings without financial history | DDD005 | Fixed: `Booking.CanBeRemoved` on `GetBookingDto` drives the button |
| C1 | `src/ViajantesTurismo.Catalog.Application/Tours/CatalogTourPresentationUpdate.cs` | Holds title, slug, and length rules that `CatalogTour.ChangePresentation` does not enforce | Rules in the domain | Fixed: the `CatalogTourPresentation` value object enforces them in `ChangePresentation` |
| C2 | `src/ViajantesTurismo.Catalog.Domain/Tours/CatalogTour.cs:115,166,179,218` | Throws exceptions for expected business outcomes | ADR-002 | Fixed: `ChangePresentation` and `Publish` return `Result` values from `CatalogTourErrors` |
| C3 | `src/ViajantesTurismo.Catalog.Application/Media/MediaImageOriginalStoredIntegrationHandler.cs:45-66` | Chooses the next media processing status in the handler | Behavior over state | Fixed: `PublicMediaImage.CompleteProcessing` and `FailProcessing` |

### Value-object follow-up, October 31, 2026

`ContactInfo`, `Address`, and `Discount` compare their sanitized fields structurally through the existing
SharedKernel `ValueObject` base. Their factories, private construction, persisted fields, and string comparisons stay
unchanged. `ProfileValueEqualityTests` first demonstrated separate equivalent instances occupying distinct hash-set
entries.

`BookingPriceBreakdown` owns component price limits and the positive discounted-total invariant. The three
`BookingPricingInvariantTests` first demonstrated room, companion, and bike changes accepting an existing absolute
discount that exceeded the revised subtotal. Every service change validates the complete candidate price, and
`Tour.UpdateBookingDetails` applies the complete selection atomically. `TourPricingInvariantTests` covers both a valid
combined change whose intermediate room price would fail and rejection without partial changes.

`TourPricing.Create` rejects undefined currency enum values while retaining BRL, EUR, USD, and the existing
round-away-from-zero price policy. `TourPricingValidationTests` first demonstrated acceptance of undefined values;
valid-currency and midpoint-rounding cases guard the unchanged behavior.

The Management forms use `BookingPricePreview` to capture their incomplete draft inputs as one immutable display
value. The preview retains its existing zero clamp and tolerance for unselected options; server-side booking
acceptance remains a domain responsibility. This distinction avoids a Management-to-Domain dependency and does not
change the existing tour-price rounding or exact payment-money policies.

## Domain enum inventory

Twenty-five public enums live in the domain projects. Thirty branch sites exist outside the domain. Most are seed data,
query ordering, input guards, store filters, or rendering switches; the business decisions were findings A4, A12, A14,
and A15, all fixed.

| Category | Enums | Decision |
| --- | --- | --- |
| Stable labels | `BikeType`, `RoomType`, `BedType`, `Currency`, `PaymentMethod`, `DiscountType`, `DocumentType`, `DocumentAudience`, `DocumentPrivacyClassification`, `PublicContentLanguage`, `PaymentAccountKind`, `BrazilianAccountType`, `PaymentAccountVerificationMethod` | Keep as enums |
| Reasons and audit vocabulary | `DocumentAuditOutcome`, `DocumentAuditReasonCode`, `DocumentAuditOperation`, `FinancialReason`, `PaymentAccountReason`, `PaymentAccountReviewOutcome` | Keep as enums |
| Lifecycle states | `BookingStatus`, `PaymentStatus`, `DocumentStatus`, `PaymentAccountStatus`, `MediaImageProcessingStatus`, `PublicContentPublicationState` | Keep public for display, mapping, and filtering; move decisions behind domain predicates |

No enum analyzer is planned: risky usage is concentrated in a few handlers and is covered by review and the audit
(catalog rule DDD007).

## Related Documentation

Use the [documentation source-of-truth map](../README.md#source-of-truth-map) for related repository guidance.
