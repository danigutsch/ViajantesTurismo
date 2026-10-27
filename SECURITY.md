# Security Policy

## Supported Versions

This repository is under active development. Security fixes are applied to the current `main`
branch only.

| Output | Supported for security fixes |
| --- | --- |
| Application (`main`) | Current `main` only |
| Local or alpha/beta prerelease packages | Not backported; upgrade to the current `main` or newest prerelease |
| Stable releases and release branches | None exist yet |

Prerelease channels and the stable promotion rules are defined in
[ADR-032](docs/adr/20260704-versioning-and-release-governance.md). Support windows for
`PandaBear.SharedKernel.*` packages are owned by the SharedKernel repository's own security policy.

When the application publishes its first stable release or creates a release branch, this policy
must be updated to list which versions continue to receive security fixes and for how long.

## Reporting a Vulnerability

Please **do not** open a public issue for suspected security vulnerabilities.

Instead, use one of these private reporting paths:

1. Open a private vulnerability report through GitHub's security reporting flow for this repository.
2. If private reporting is not available, contact the repository owner privately through GitHub.

When reporting a vulnerability, please include:

- a clear description of the issue
- affected component or file paths
- reproduction steps or proof of concept, if available
- potential impact
- any suggested mitigation or fix

You can expect an initial acknowledgement as soon as reasonably possible. After triage, fixes will
be prepared privately when appropriate and disclosed in a coordinated manner.

For non-sensitive bugs or hardening suggestions that are **not** security vulnerabilities, use the
normal issue tracker.
