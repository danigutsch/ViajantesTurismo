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
```

Pass `--root <path>` after the command to target another repository root.

`init` disables GitHub sync by default. Set `integrations.github.repository`, then set
`integrations.github.enabled` to `true` in `roadmap/config.json` before running `sync github`.

## Commands

| Command | Behavior |
| --- | --- |
| `init` | Creates missing roadmap directories and default files. Existing files are not overwritten. |
| `verify` | Checks roadmap structure, config, item metadata, scoring values, and dependencies. |
| `diff` | Reports verification drift using the same checks as `verify`. |
| `set github.repository <owner/repo>` | Updates the GitHub projection repository in `roadmap/config.json`. |
| `get next-priority` | Lists open items by explicit order and RICE score. |
| `get next-unblocked` | Lists open items with no open blockers. Supports `--type <type>`. |
| `get next-work` | Generates the executable queue: unblocked items that unblock open work first, then other unblocked items by explicit order and RICE score. Supports `--type <type>`. |
| `get blockers-of <id>` | Lists direct blockers for one roadmap item. |
| `get next-blockers` | Lists open blockers or items that unblock other work. |
| `get next-enablers` | Lists unblocked enabler items. |
| `get low-hanging-fruit` | Lists unblocked low-effort items first. |
| `get pareto` | Lists the top unblocked high-score slice. |
| `get blocking-overview` | Lists open items and their open direct blocker IDs. |
| `get tags` / `get labels` | Lists tag or label counts. |
| `get by-tag <tag>` / `get by-label <label>` | Lists items by taxonomy value. |
| `sync github --dry-run` | Previews issue creation and labels. When a Project target is configured, it authenticates to preflight target, schema, membership, field writes, and conflicts without mutation. |
| `sync github --apply` | Creates requested issues, persists their issue numbers, adds labels, and configures Project membership using `GH_TOKEN` or `GITHUB_TOKEN`. |

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

Project membership requires a user-owned Project target in `integrations.github.projectV2` and a
classic token with `repo` and `project` scopes. Fine-grained tokens cannot currently automate
user-owned Projects. A configured Project target requires that token for both `--dry-run` live
preflight and `--apply`.
