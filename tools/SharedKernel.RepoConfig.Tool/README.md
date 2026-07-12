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
dotnet run --project tools/SharedKernel.RepoConfig.Tool/SharedKernel.RepoConfig.Tool.csproj -- sync github --dry-run
```

Pass `--root <path>` after the command to target another repository root.

## Commands

| Command | Behavior |
| --- | --- |
| `init` | Creates missing roadmap directories and default files. Existing files are not overwritten. |
| `verify` | Checks roadmap structure, config, item metadata, scoring values, and dependencies. |
| `diff` | Reports verification drift using the same checks as `verify`. |
| `set github.repository <owner/repo>` | Updates the GitHub projection repository in `roadmap/config.json`. |
| `get next-priority` | Lists open items by explicit order and RICE score. |
| `get next-unblocked` | Lists open items with no open blockers. Supports `--type <type>`. |
| `get blockers-of <id>` | Lists direct blockers for one roadmap item. |
| `get next-blockers` | Lists open blockers or items that unblock other work. |
| `get next-enablers` | Lists unblocked enabler items. |
| `get low-hanging-fruit` | Lists unblocked low-effort items first. |
| `get pareto` | Lists the top unblocked high-score slice. |
| `get tags` / `get labels` | Lists tag or label counts. |
| `get by-tag <tag>` / `get by-label <label>` | Lists items by taxonomy value. |
| `sync github --dry-run` | Prints the GitHub issue updates that would be applied. |
| `sync github --apply` | Updates existing mapped GitHub issues using `GH_TOKEN` or `GITHUB_TOKEN`. |

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Command completed successfully. |
| `1` | Verification failed or command execution failed. |
| `2` | Command syntax is invalid. |

GitHub sync preserves existing issue body content and only upserts the managed
roadmap section. Labels are additive. The repository remains the source of truth.
