# SharedKernel repo config tool

`sharedkernel-repo` verifies and maintains repository-owned planning and configuration
structure without adding regular helper scripts.

## Usage

```bash
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- verify
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- diff
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- init
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- set github.repository owner/repository
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-priority
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get next-work
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- get blocking-overview
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- sync github --dry-run
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- reconcile github --dry-run
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- reconcile github --apply
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- intake github --dry-run
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- intake github --apply
```

Pass `--root <path>` after the command to target another repository root.

`init` disables GitHub sync by default. Set `integrations.github.repository`, then set
`integrations.github.enabled` to `true` in `roadmap/config.json` before running `sync github`.

## Commands

| Command | Behavior |
| --- | --- |
| `init` | Creates missing roadmap directories and default files. Existing files are not overwritten. |
| `verify` | Checks roadmap structure, config, item metadata, triage state, scoring values, and dependencies. |
| `diff` | Reports verification drift using the same checks as `verify`. |
| `set github.repository <owner/repo>` | Updates the GitHub projection repository in `roadmap/config.json`. |
| `get next-priority` | Lists open triaged items by explicit order and RICE score. |
| `get next-unblocked` | Lists open triaged items with no open blockers. Supports `--type <type>`. |
| `get next-work` | Generates the executable queue from unblocked triaged items, prioritizing items that unblock open work, then explicit order and RICE score. Supports `--type <type>`. |
| `get blockers-of <id>` | Lists direct blockers for one roadmap item. |
| `get next-blockers` | Lists open triaged blockers or triaged items that unblock other work. |
| `get next-enablers` | Lists unblocked triaged enabler items. |
| `get low-hanging-fruit` | Lists unblocked triaged low-effort items first. |
| `get pareto` | Lists the top unblocked triaged high-score slice. |
| `get blocking-overview` | Lists open triaged items and their open direct blocker IDs. |
| `get tags` / `get labels` | Lists tag or label counts. |
| `get by-tag <tag>` / `get by-label <label>` | Lists triaged items by taxonomy value. |
| `sync github --dry-run` | Previews issue creation and labels. When a Project target is configured, it authenticates to preflight target, schema, membership, field writes, and conflicts without mutation. |
| `sync github --apply` | Creates requested issues, persists their issue numbers, adds labels, and configures Project membership using `GH_TOKEN` or `GITHUB_TOKEN`. |
| `reconcile github --dry-run` | Default. Regenerates the manifest in memory from restricted structural GitHub metadata and previews the local manifest update. |
| `reconcile github --apply` | Atomically updates only the existing reconciliation manifest; it never mutates GitHub or roadmap items. |
| `intake github --dry-run` | Default. Reads the reconciliation snapshot's restricted GitHub metadata and previews local roadmap changes without writing. |
| `intake github --apply` | Writes only local roadmap items and `roadmap/order.json`; it never mutates GitHub. |

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Command completed successfully. |
| `1` | Verification failed or command execution failed. |
| `2` | Command syntax is invalid. |

Set `integrations.github.issue` to `"create"` to create an issue; `sync github --apply` replaces it
with the created issue number.

GitHub sync never modifies issue bodies. Labels are additive. Existing labels and conflicting Project
field values are report-only drift. The repository remains the source of truth.

GitHub reconciliation requires exactly one seed manifest at
`roadmap/reconciliation/open-issues-*.json`. The only reviewed inputs are:

- `mechanicalPriorityOverride`: approved scoring and ordering policy;
- `directCanonicalPrimaries`: exact existing issue-to-roadmap mappings that define canonical roots;
- `closedItemTransitions`: explicit approvals to change mapped open items to closed support.

The tool derives the snapshot date and commit, issue dispositions, parent-chain exits, structural roots,
exact blocker edges and endpoint states, and all integrity counts. When a mapped active item closes,
the command fails with the exact `#issue -> RM-ID` entry and manifest path to add. It requests only issue
number, title, state, labels, official parent/subissue relationships, exact blocker relationships, and
commit metadata. It never requests bodies, comments, tokens, or mutations. A digest of allowed open-issue
metadata lets intake reject changes made after reconciliation.

GitHub intake requires exactly one `roadmap/reconciliation/open-issues-*.json` manifest. It rejects
snapshot drift and pull requests before writing. It reads only issue number, title, state, labels, and
parent number; it never reads or persists bodies, comments, or tokens. Re-running a matching apply is
idempotent. `closedItemTransitions` can transition an existing mapped open item to closed support while
retaining its canonical roadmap item ID; without that explicit manifest declaration, intake rejects the
transition.

An item with `"triage": "untriaged"` omits `order` and `scoring`. The tool accepts it as
canonical identity and hierarchy, excludes it from executable priority queries and `order.json`,
and leaves Project numeric priority fields unchanged during sync. Status and text projection remain
available.

Project membership requires a user-owned Project target in `integrations.github.projectV2` and a
classic token with `repo` and `project` scopes. Fine-grained tokens cannot currently automate
user-owned Projects. A configured Project target requires that token for both `--dry-run` live
preflight and `--apply`.

`Roadmap status` is a safety gate: missing, ambiguous, incompatible, or unaddressable status
schema/options block all Project membership and field writes. Other incompatible Project fields
remain report-only drift.
