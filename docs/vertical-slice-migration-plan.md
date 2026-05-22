# Admin Bounded Context: Vertical Slice Migration Plan

## Goal

Migrate from separate `ViajantesTurismo.Admin.Domain` and `ViajantesTurismo.Admin.Application` projects to a single `ViajantesTurismo.Admin` bounded-context project using vertical slice architecture.

## Steps

### 1. Audit

- Inventory all existing references to `Admin.Domain` and `Admin.Application` in solution, app host, infra, API, and tests.
- Catalog all features/use cases (slices), public APIs, and internal contracts.

### 2. Propose Target Structure

- New project: `src/ViajantesTurismo.Admin/`
- Each use case in own folder under the owning business domain (e.g., `Bookings/RecordPayment/`, `Customers/ImportCustomers/`)
- Inside each slice: handler, validator, types, tests.
- Keep boundary between domain model, orchestrators/use-cases, and internal read/write contracts clear but local.
- Example:
    - `src/ViajantesTurismo.Admin/Bookings/RecordPayment/RecordPaymentHandler.cs`
    - `src/ViajantesTurismo.Admin/Bookings/RecordPayment/RecordPaymentValidator.cs`
    - `src/ViajantesTurismo.Admin/Bookings/RecordPayment/Command.cs`
    - `src/ViajantesTurismo.Admin/Bookings/RecordPayment/Tests/RecordPaymentHandlerTests.cs`

### 3. Design Migration Workflow

- Start by creating project, copying 1-2 simplest slices.
- Update API/infra/test refs for those.
- Validate build and tests.
- Repeat for remaining slices, keeping old projects until confident.
- Remove `Admin.Domain` and `Admin.Application` at the end, after full migration and validation.

### 4. Validation & Safeguards

- After each moved slice: build, run all tests, verify feature works.
- Document migration steps and decisions in `/docs/ARCHITECTURE_DECISIONS.md` or a new migration decision record.

### 5. Aftercare

- Enforce business/domain slice ownership for all new features.
- Review shared abstractions regularly to ensure they're truly cross-cutting.

---
Need approval/feedback before starting phase 1. Can split tasks and track progress.
