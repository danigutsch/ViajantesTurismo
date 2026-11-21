# Reproduce a Catalog projection retry

The `background.catalog-projection-retry` fixture exercises the Catalog projection runner and its
tour read-model projection with in-memory stores. Each of the two cases creates a fresh event
store, checkpoint store, read model, and runner. It seeds one synthetic draft event at position
11 and a checkpoint at position 10. [Reproduction contract](reproduction-contract.md)

Run the fixture from the repository root with the SDK and
[pinned packages](../sharedkernel/development.md) staged.

```sh
dotnet test --project tests/ViajantesTurismo.Catalog.UnitTests -c Release -- \
  --filter-class '*CatalogProjectionRunnerReproductionTests'
```

The fixture makes the first read-model write fail and verifies that the draft is absent and
the checkpoint stays at 10. It repairs the test store and invokes the same runner again. The
runner reads from 10, projects the draft once, and saves position 11. A third invocation reads
from 11 and finds no remaining event. A ten-second cancellation budget covers each attempt.

Set `VIAJANTESTURISMO_REPRODUCTION_MANIFESTS=1` on the same command to keep one manifest per
attempt under `TestResults/reproduction`. The normal run round-trips each manifest in a
temporary directory. The fixture records the source revision stamped in the test assembly, an opaque attempt ID,
timing, and the trace ID observed on the projection runner's spans. It registers result `reproduced`,
outcome `checkpoint-advanced-after-retry`, and resource `catalog-projection` with state
`caught-up`. Synthetic tour identifiers, payload values, and the raw failure message remain
in test-owned state.
