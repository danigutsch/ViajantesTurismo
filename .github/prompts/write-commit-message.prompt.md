---
name: write-commit-message
description: Generate a commit message for the current staged or pending changes using this repository's commit rules.
agent: agent
---

Review the current staged or pending changes in this repository and propose the best commit message.

Requirements:

- Use Conventional Commits: `<type>[optional scope]: <description>`.
- Allowed types: `build`, `chore`, `ci`, `docs`, `feat`, `fix`, `perf`, `refactor`, `revert`, `style`, `test`.
- Keep the header at or under 100 characters.
- If a body is included, wrap every body line to 100 characters or less.
- If a body adds value, insert one blank line after the header before the body.
- Prefer one message that captures the primary intent of the change. Do not stack multiple headers.

Useful references:

- [`commitlint.config.mjs`](../../commitlint.config.mjs)
- [`CONTRIBUTING.md`](../../CONTRIBUTING.md)

Return:

1. `Best message`
2. `Why this fits`
3. `Alternative message` (optional)
