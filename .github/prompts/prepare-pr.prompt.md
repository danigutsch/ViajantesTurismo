---
name: prepare-pr
description: Draft a pull request title and description for the current branch using the repository template and validation checklist.
agent: agent
---

Review the current branch changes and draft a pull request package that is ready to paste into GitHub.

Requirements:

- Produce a concise, outcome-focused PR title. Keep it under 100 characters when possible.
- Use the repository template from [`docs/pull_request_template.md`](../../docs/pull_request_template.md).
- Fill in `Summary`, `Changes`, `Validation`, and `Checklist`.
- Keep validation honest: list commands that actually ran, and use `Not run, with reason:` when needed.
- Preserve checklist items so the author can check them off.
- Call out follow-up work or open questions only when they are real.

Return:

1. `Title`
2. `Description` as markdown
3. `Open questions` (only if anything important is missing)
