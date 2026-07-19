# Roadmap

This folder is the source of truth for roadmap intent and prioritization inputs.
GitHub Issues and Projects are execution views derived from this data, not the
canonical roadmap database.

## Layout

```text
roadmap/
├── README.md
├── config.json
├── items/
│   └── RM-001-roadmap-gitops.json
├── order.json
├── reconciliation/
│   └── open-issues-YYYY-MM-DD.json
├── schema/
│   ├── roadmap-config.schema.json
│   └── roadmap-item.schema.json
└── themes/
    └── repo-operations.json
```

## Open GitHub issue reconciliation

Exactly one [`reconciliation/open-issues-*.json`](reconciliation/) manifest defines the accepted open
snapshot, issue dispositions, exact blocker edges, closed endpoint states, and approved mechanical
priority policy. Intake requires `snapshotDigest` and rejects a missing or mismatched digest or
pull-request endpoint before writing any local roadmap file. Only exact
`integrations.github.issue` mappings affect canonical roadmap identity, dependencies, or priority.

Run `reconcile github --dry-run` before intake. The .NET tool preserves the reviewed policy, canonical
primary mappings, and explicit closure approvals, then derives every snapshot-owned disposition, exact
blocker edge, endpoint state, and integrity count from field-selected GitHub GraphQL data.
`reconcile github --apply` updates only this manifest. If a mapped active item closes without approval,
the error names the exact `closedItemTransitions` entry and manifest path to edit.

For a previously imported open item whose issue is now closed, the manifest must explicitly declare
the identity-preserving transition:

```json
"closedItemTransitions": [
  { "issue": 100, "roadmapItem": "RM-018" }
]
```

Intake then changes the existing item to closed support (`"status": "done"`,
`"triage": "untriaged"`), removes `order` and `scoring`, and removes it from `order.json`. Without
that exact declaration, intake rejects the transition rather than closing a reviewed item implicitly.

## Workflow

1. Add or edit roadmap items in a pull request.
2. Review priority inputs with the same care as implementation code.
3. Merge repository changes first.
4. Run `reconcile github --dry-run`, then review and run `reconcile github --apply` to update the
   structural GitHub snapshot.
5. Run `intake github --dry-run`, then review and run `intake github --apply` to write the accepted
   snapshot into repository-owned roadmap files.
6. Project roadmap items into GitHub Issues and Projects through a dedicated adapter.
7. Report drift when repository-owned fields and GitHub-owned fields conflict.

The repository owns item identity, outcome, explicit ordering and scoring for triaged
work, blockers, parent-child relationships, themes, tags, and labels. GitHub owns
discussion, day-to-day execution comments, and workflow details that are not
represented in the canonical model.

This roadmap intentionally avoids date-based planning. Use `order` for priority
and `blockedBy` for sequencing. Lower `order` values come first among feasible
work; open blockers are a gate, not a score adjustment.

`roadmap/order.json` is the canonical list of every triaged roadmap item ID. Its
`items` array must contain each triaged item exactly once, ordered by `order`
ascending, RICE score descending, then ID ascending.

Existing reviewed orders remain first. Imported work follows blocker-safe topological
order, then RICE score descending, then canonical ID. Intake sets `reach` to `1`,
`confidence` to `0.1`, `impact` to `1 + direct open blockers` capped at `5`, and
effort from the reconciliation manifest's approved type mapping.

## Prioritization

Roadmap items start with RICE scoring:

```text
score = reach * impact * confidence / effort
```

Keep the inputs reviewable instead of hand-editing rank:

- `reach`: estimated affected users, maintainers, or workflows.
- `impact`: expected outcome strength from `1` to `5`.
- `confidence`: estimate confidence from `0.1` to `1.0`.
- `effort`: implementation effort, where higher means more cost.

Use WSJF only after cost-of-delay inputs are reliable and shared across teams.

### Untriaged work

Use `"triage": "untriaged"` for an active item that needs canonical identity,
hierarchy, or GitHub projection before priority evidence exists. Untriaged items must
omit `order` and `scoring`; they are excluded from `order.json` and executable
priority queries. They may still block scored work and appear in `blockers-of` without
an invented order or score.

Record evidence before removing `triage` and assigning `order` and RICE inputs. Do not
use placeholder zeroes or arbitrary low values to make an item fit the triaged model.

Priority overrides are exceptional: security, privacy, data safety, legal/compliance,
fixed external deadlines, material risk reduction, or prerequisites that unlock multiple
higher-value items. Record the evidence, owner, review date, and displaced work in the
pull request that changes canonical roadmap data.

## GitHub projection policy

- Issues are execution records.
- Projects are planning views.
- Labels are stable facets such as area or type.
- GitHub issue bodies remain GitHub-owned; roadmap sync only adds mapped labels.
- Existing roadmap managed sections in issue bodies are left untouched.
- Milestones are not used by the roadmap model by default.
- Project fields may mirror score inputs and computed score, but they do not
  replace the repository model.
- Untriaged items can project their status and text fields, but sync leaves all
  Project numeric priority fields unchanged.
- Project-configured dry-runs authenticate only to validate target, schema,
  membership, proposed field writes, and report-only conflicts; they do not mutate
  GitHub.
- Sync automation must be driven by the .NET repo config tool, not shell or
  Python helper scripts.
- Intake retains only issue number, title, state, labels, and parent linkage; it
  does not import issue bodies or comments.
- Intake writes only manifest-declared blocker edges, creates closed support items or performs declared
  imported-item transitions when required, and is idempotent when the roadmap matches the manifest.

## Project queries

The repo config tool also exposes project-style queries over roadmap data:

```bash
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-priority
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-unblocked --type issue
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-work
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get blockers-of RM-004
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-blockers
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get low-hanging-fruit
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get tags
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get by-label "area: tooling"
```

Use `get next-work` for the local executable queue: it promotes unblocked work that
removes open blockers, then uses canonical order and RICE to break ties. Use
`next-priority` to inspect the strategic ranking, and `next-unblocked` or
`next-blockers` to diagnose sequencing. Run `verify` before trusting any generated view.

## Repository config tool

Use the .NET repo config tool for local setup and verification:

```bash
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- verify
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- diff
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- init
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- set github.repository danigutsch/ViajantesTurismo
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- sync github --dry-run
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- reconcile github --dry-run
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- reconcile github --apply
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- intake github --dry-run
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- intake github --apply
```

The first implementation avoids regular helper scripts and extra package
dependencies. It verifies the roadmap folder structure, JSON config, roadmap
items, scoring fields, ordering, blockers, and item dependencies.
