# Reproduce a Branding worker redelivery

The `background.branding-redelivery` fixture runs the normal Admin integration-event worker
with the pinned transport consumer and a fresh pair of Admin and Catalog schemas per attempt.
The test creates both disposable databases on the existing hosted fixture and removes them
on disposal. It uses a fixed clock for delivery eligibility and test-owned Branding revisions.
[Reproduction contract](reproduction-contract.md)

Use the [hosted test prerequisites](../../tests/ViajantesTurismo.HostedTests/README.md). With
the repository SDK and a local Docker or Podman runtime, run the fixture from the repository root.

```sh
dotnet test --project tests/ViajantesTurismo.HostedTests -c Release -- \
  --filter-class '*.BrandingWorkerReproductionTests'
```

The fixture queues one revision from the current lineage and one from a new lineage. It waits
for the worker to persist the recovery fence and a retry with the original projection intact.
It authorizes a synthetic reseed through the application service and advances the fixture clock
to the persisted retry date. The worker applies the event on retry and records it once in the
inbox. The fixture reopens that settled transport row to model a lost acknowledgement and verifies
that repeat delivery leaves the stored revision and inbox count unchanged. Each attempt has a
45-second cancellation boundary and uses its own database pair.

The fixture repeats the same outcome twice. It reads the trace ID from the worker's existing
retry log and writes only the verified `reproduced` and `replayed` outcome, the known worker
state, source revision, attempt timing and trace correlation. Synthetic event identifiers,
message content, the recovery operator and exception text stay outside the manifest.

Set `VIAJANTESTURISMO_REPRODUCTION_MANIFESTS=1` on that command to retain the manifests in
`TestResults/reproduction`. The default run round-trips the same manifest in a temporary
directory. Run the same command for a reset. The fixture exercises delivery and settlement
through the existing worker; the broader reproduction inventory records other background seams.
