---
goal: Retire ViajantesTurismo.Common by migrating remaining logic to SharedKernel
version: 1.0
date_created: 2026-07-03
last_updated: 2026-07-03
owner: ViajantesTurismo maintainers
status: 'Completed'
tags: [refactor, architecture, migration, sharedkernel]
---

# Introduction

![Status: Completed](https://img.shields.io/badge/status-Completed-green)

Retire `src/ViajantesTurismo.Common` by moving active logic into focused owners and deleting the obsolete project.

## 1. Requirements & Constraints

- **REQ-001**: Remove all references to `ViajantesTurismo.Common` source and test projects.
- **REQ-002**: Reuse `SharedKernel.BuildingBlocks` for `ValueObject` and `DateRange`.
- **REQ-003**: Move cross-context sanitizers to `SharedKernel.InputNormalization`.
- **REQ-004**: Move cross-contract HTTP validation helpers to `SharedKernel.HttpClients`.
- **REQ-005**: Move Admin-only `Currency` to `ViajantesTurismo.Admin.Domain.Tours`.
- **CON-001**: No compatibility facade; ending Common means no remaining project, namespace, or docs dependency.
- **CON-002**: Preserve sanitizer and validation behavior with moved tests.

## 2. Implementation Steps

### Implementation Phase 1

- GOAL-001: Move code to focused owners.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Move sanitizers to `src/SharedKernel/SharedKernel.InputNormalization`. | ✅ | 2026-07-03 |
| TASK-002 | Move contract validation helpers to `src/SharedKernel/SharedKernel.HttpClients`. | ✅ | 2026-07-03 |
| TASK-003 | Move `Currency` to `src/ViajantesTurismo.Admin.Domain/Tours/Currency.cs`. | ✅ | 2026-07-03 |
| TASK-004 | Replace building-block consumers with `SharedKernel.BuildingBlocks`. | ✅ | 2026-07-03 |

### Implementation Phase 2

- GOAL-002: Move tests and delete obsolete projects.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-005 | Move sanitizer tests to `tests/SharedKernel.InputNormalization.Tests`. | ✅ | 2026-07-03 |
| TASK-006 | Move contract helper tests to `tests/SharedKernel.HttpClients.Tests`. | ✅ | 2026-07-03 |
| TASK-007 | Move building-block tests to `tests/SharedKernel.BuildingBlocks.Tests`. | ✅ | 2026-07-03 |
| TASK-008 | Move ServiceDefaults telemetry tests to `tests/ViajantesTurismo.ServiceDefaults.Tests`. | ✅ | 2026-07-03 |

### Implementation Phase 3

- GOAL-003: Validate and document.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-009 | Remove Common project references from `.csproj` and `ViajantesTurismo.slnx`. | ✅ | 2026-07-03 |
| TASK-010 | Update docs that listed Common as active structure. | ✅ | 2026-07-03 |
| TASK-011 | Run build and targeted tests. | ✅ | 2026-07-03 |

## 3. Alternatives

- **ALT-001**: Keep a Common compatibility facade. Rejected because it preserves the retired boundary.
- **ALT-002**: Create `SharedKernel.Common`. Rejected because it recreates the vague project.

## 4. Dependencies

- **DEP-001**: `SharedKernel.BuildingBlocks`.
- **DEP-002**: `SharedKernel.Results` through existing `DateRange`.
- **DEP-003**: Issue #563.

## 5. Files

- **FILE-001**: `src/SharedKernel/SharedKernel.InputNormalization/*`.
- **FILE-002**: `src/SharedKernel/SharedKernel.HttpClients/*`.
- **FILE-003**: `src/ViajantesTurismo.Admin.Domain/Tours/Currency.cs`.
- **FILE-004**: `ViajantesTurismo.slnx`.
- **FILE-005**: `README.md` and docs under `docs/`.

## 6. Testing

- **TEST-001**: `dotnet build ViajantesTurismo.slnx`.
- **TEST-002**: `dotnet test --solution ViajantesTurismo.slnx`.

## 7. Risks & Assumptions

- **RISK-001**: Project-reference edits can expose missing transitive references.
- **ASSUMPTION-001**: No external package compatibility surface is required for Common.

## 8. Related Specifications / Further Reading

- [Issue #563: Retire ViajantesTurismo.Common and migrate remaining logic to SharedKernel](https://github.com/danigutsch/ViajantesTurismo/issues/563)
- [ADR: split SharedKernel Domain and BuildingBlocks primitives](../docs/adr/20260621-split-sharedkernel-domain-and-building-blocks.md)
