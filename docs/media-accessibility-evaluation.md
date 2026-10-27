# Media accessibility evaluation

Run `dotnet test --project tests/ViajantesTurismo.Catalog.UnitTests -- --filter-class ViajantesTurismo.Catalog.UnitTests.MediaAccessibilityEvaluationTests`
for the deterministic reference baseline and rubric regression checks. These synthetic reference
answers test the gate; they are not human evaluation evidence and do not prove a deployed model passes.

Copy the baseline score sheet outside source control for a candidate run. Generate text for every
versioned image/context/language case through the configured `PandaBear.AI.IImageTextGenerator`.
Rasterize the SVG fixtures for providers that require PNG/JPEG; use the same rendered bytes for
baseline and candidate. Record provider, actual model, prompt version, configuration version, reviewer,
and review date in the candidate sheet. Replace the reference-fixture provenance with actual run and
human-review evidence. Never include credentials or private production images.
The committed fixtures are original synthetic illustrations with no personal data.

A human reviewer scores each dimension from 0 (unacceptable) to 2 (fully meets the requirement):

- Accessibility: conveys the image's purpose in the supplied page context; excludes redundant wording.
- Grounding: describes only visible facts and explicitly supplied context.
- Concision: uses a short, useful alternative rather than marketing prose or a scene inventory.
- Localization: uses the requested language naturally, including appropriate place-name conventions.
- Unsupported claims: 2 means no invented location, identity, accessibility facility, or other fact;
  any unsupported claim fails the case.

Every dimension must score at least 1, unsupported claims must score 2, and no dimension may regress
against the baseline. Missing cases, duplicated cases, missing provenance, and incomplete score sheets
fail validation. Set `MEDIA_ACCESSIBILITY_EVALUATION_CANDIDATE` to the candidate JSON path and run the same C# test.
The test fails for rubric regression or invalid evidence. Golden changes require human
review; never overwrite the baseline simply to accept a failing candidate. A passing evaluation never
approves or publishes generated accessibility text: ordinary human review still applies.

The rubric follows [W3C informative-image guidance](https://www.w3.org/WAI/tutorials/images/informative/).
Provider/prompt comparisons must use identical fixture hashes and rubric versions. The current
SharedKernel generator returns text only: actual routed model, prompt, token usage and cost are not
exposed. Capture those from the provider's controlled evaluation records; do not label guessed values
as measured provenance. Runtime traces distinguish the configured LiteLLM adapter and requested model from the actual model
(which remains `unreported`). The prompt implementation version is the immutable SharedKernel AI
artifact version; the configuration fingerprint covers non-secret model, completion path, and endpoint
origin. It excludes credentials. These fields do not claim knowledge of proxy routing or provider usage.

## Observability

Generation and review instruments use the existing `ViajantesTurismo.Catalog` meter and ActivitySource,
which ServiceDefaults already exports through OpenTelemetry. Metric tags contain bounded outcomes and
supported language tags only; traces additionally record controlled generation provenance. Neither records
image identifiers, object keys, coordinates, context, generated text,
reviewer identities, or exception messages. Latency uses seconds.

For evaluation trends, optionally set `MEDIA_ACCESSIBILITY_EVALUATION_OTLP_ENDPOINT=http://localhost:4318/v1/metrics`
for an explicit candidate evaluation run, targeting the environment's existing OTLP/HTTP receiver.
Reference self-tests do not emit model evaluation trends. The runner exports only aggregate
pass/fail case counts on `catalog.media_accessibility.evaluation.cases`; full text and reviewer records
remain local. No endpoint is contacted unless explicitly
provided; check collector health and exporter diagnostics before relying on a trend. Configure receiver authentication through existing exporter configuration.

Grafana can chart generation outcomes, latency percentiles, review outcomes, and evaluation pass/fail
counts from those instruments. Review approval/edit/rejection ratios use their decision counts
as denominator; failed persistence and missing-alt validation counts are separate failure signals.
The public media delivery gate records images withheld for missing alternative text or pending review
on `catalog.media_accessibility.publication.blocked`, including list filtering and direct image requests.
Generation stores a per-language pending draft timestamp using the injected `TimeProvider`. Successful
review clears only that language's pending timestamp. Queue age is measured after persistence using
the prior draft timestamp; clock skew clamps negative age to zero. Drafts without timestamps
do not report an age. The nullable timestamp migration does not invent dates for existing rows.
Token and cost panels are unavailable because the shared provider
contract supplies no measured usage. See [.NET metrics guidance](https://learn.microsoft.com/dotnet/core/diagnostics/metrics-instrumentation).
