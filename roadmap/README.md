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
├── schema/
│   ├── roadmap-config.schema.json
│   └── roadmap-item.schema.json
└── themes/
    └── repo-operations.json
```

## Workflow

1. Add or edit roadmap items in a pull request.
2. Review priority inputs with the same care as implementation code.
3. Merge repository changes first.
4. Project roadmap items into GitHub Issues and Projects through a dedicated
   adapter.
5. Report drift when repository-owned fields and GitHub-owned fields conflict.

The repository owns item identity, outcome, explicit ordering, scoring inputs,
blockers, parent-child relationships, themes, tags, and labels. GitHub owns
discussion, day-to-day execution comments, and workflow details that are not
represented in the canonical model.

This roadmap intentionally avoids date-based planning. Use `order` for priority
and `blockedBy` for sequencing. Lower `order` values come first.

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

## GitHub projection policy

- Issues are execution records.
- Projects are planning views.
- Labels are stable facets such as area or type.
- Milestones are not used by the roadmap model by default.
- Project fields may mirror score inputs and computed score, but they do not
  replace the repository model.
- Sync automation must be driven by the .NET repo config tool, not shell or
  Python helper scripts.

## Project queries

The repo config tool also exposes project-style queries over roadmap data:

```bash
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-priority
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-unblocked --type issue
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get blockers-of RM-004
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-blockers
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get low-hanging-fruit
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get tags
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get by-label "area: tooling"
```

Use these commands for the local "what should we do next?" view before opening
GitHub Projects.

## Repository config tool

Use the .NET repo config tool for local setup and verification:

```bash
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- verify
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- diff
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- init
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- set github.repository danigutsch/ViajantesTurismo
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- sync github --dry-run
```

The first implementation avoids regular helper scripts and extra package
dependencies. It verifies the roadmap folder structure, JSON config, roadmap
items, scoring fields, ordering, blockers, and item dependencies.
