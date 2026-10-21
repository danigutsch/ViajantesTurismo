# Garage media storage evaluation

Garage was evaluated on 2026-10-21 against v2.4.1 and the repository-pinned AWS .NET SDK.
**Decision: reject Garage adoption for the current media deployment.** Keep SeaweedFS for local
development and the configured AWS S3 adapter for the first cloud deployment. No production Garage
resource or SharedKernel hosting package is introduced.

## Reproducible evidence

The integration fixture runs the official single-node workflow with generated credentials, an
ephemeral bucket, and the immutable image
`docker.io/dxflrs/garage:v2.4.1@sha256:0d7c74fc8ca6fef68a5a941c0e7558c8b1e92ba3588fa7505400e1350456c796`.
This is a test-only container supply-chain dependency; changing its version requires re-evaluation.
The test application owns and disposes its container and temporary configuration. No existing store
or production data is changed.

```sh
dotnet test --project tests/ViajantesTurismo.Catalog.IntegrationTests -- \
    --filter-class '*GarageMediaObjectStoreTests'
```

Four container-backed tests passed:

- The current `IMediaObjectStore.Put` request is rejected with HTTP 400 and
  `Bad request: Invalid payload signature`. The test preserves this incompatibility as evidence;
  success is not claimed for the production upload path.
- A test-only SDK seed using `UseChunkEncoding = false` succeeds. The unchanged media adapter can
  then read, check existence, list keys and modification times, map the independent CDN URI, and
  delete idempotently. Metadata checksum and content type survive the round trip.
- Unsigned writes are forbidden.
- Presigned PUT succeeds with the declared type and exact length; changing either header/body
  length is forbidden. Offline signer tests separately verify the bounded expiry.

The successful seed isolates the upload failure to the SDK request's chunked-payload path in this
combination. It does not establish a general claim that Garage lacks all streaming support. The
[Garage signature implementation](https://docs.rs/garage_api_common/latest/src/garage_api_common/signature/payload.rs.html)
handles several streaming forms, and the
[AWS request contract](https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/S3/TPutObjectRequest.html)
exposes the chunk-encoding setting. No checksum validation or payload signing was disabled to make
this evaluation pass. Production compatibility would require a separately reviewed request change,
including non-seekable stream and integrity tests, before reconsideration.

## Comparison and operational costs

| Concern | Garage evaluation | Existing SeaweedFS path |
| --- | --- | --- |
| Object operations | Read/list/delete pass after non-chunked seed; current adapter upload fails | Existing adapter and container CRUD tests cover the configured credentials |
| Access control | Per-key bucket permissions; no S3 ACL or bucket-policy compatibility | Existing authenticated development configuration remains the supported path |
| Versioning | Upstream reports no object versioning | Not required or enabled by the current application contract |
| Presigned uploads | Actual type/length tampering rejected; expiry signing tested offline | Adapter signing support shared; no public direct-upload endpoint added |
| Local lifecycle | New node layout, credentials, and ephemeral test resource | Existing Aspire resource, secrets, and reset procedure |
| Production readiness | Single-node test has no replication, failover, backup/restore, or load evidence | Local development support does not establish production suitability |

Garage's official [S3 compatibility table](https://garagehq.deuxfleurs.fr/documentation/reference-manual/s3-compatibility/)
documents its access-control and versioning limits. Those are upstream statements, separate from the
operations actually exercised here. The [quick start](https://garagehq.deuxfleurs.fr/documentation/quick-start/)
warns that a single-node deployment has no redundancy. A production assessment would also need
capacity planning, TLS termination, network isolation, key rotation, monitoring, recovery drills, and
evidence of the intended multi-node failure behavior.

## Licensing and reconsideration

Garage is [AGPL-3.0 licensed](https://github.com/deuxfleurs-org/garage/blob/main-v2/LICENSE).
Redistribution and modified network deployments need a product/legal review of applicable source
and license obligations before adoption. This evaluation grants no legal approval and does not
assume that a separate-process deployment removes those obligations. Rejecting adoption introduces
no production Garage license dependency.

Reconsider only if there is a concrete self-hosted production requirement, the upload incompatibility
has a verified integrity-preserving solution, and product/legal/operations owners accept the costs.
No implementation follow-up or adoption ADR is created because Garage is not selected.
