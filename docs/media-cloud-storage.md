# Cloud media object storage

Cloud media storage uses AWS S3 as the first provider. The S3-compatible adapter is the SharedKernel
package `PandaBear.SharedKernel.ObjectStorage.S3`, which implements the host-agnostic
`PandaBear.SharedKernel.ObjectStorage` contract (`IObjectStore`). It supports application-owned object
keys, object metadata, inventory, reads, writes and deletion. Catalog keeps the key policy
(`CatalogDomainLimits.MaxMediaObjectKeyLength`), reconciliation and publishing rules, and binds the
`Catalog:MediaObjectStorage` sections to the package options. SeaweedFS remains available for local development.

Garage v2.4.1 was rejected on 2026-10-21: the current adapter's upload fails with
`Bad request: Invalid payload signature`. The rejected-provider evaluation tests were removed when
hosted tests were consolidated; SeaweedFS remains covered by real storage integration tests. Its
[AGPL-3.0 license](https://github.com/deuxfleurs-org/garage/blob/main-v2/LICENSE)
and [S3 policy/versioning gaps](https://garagehq.deuxfleurs.fr/documentation/reference-manual/s3-compatibility/)
also require review before reconsideration.

## Storage and scanning responsibilities

| Flow | SharedKernel dependency | Application responsibility |
| --- | --- | --- |
| Upload body | `IO` supplies a bounded, seekable lease for streams that need several passes. | Catalog chooses upload bounds, accepted media types, scan ordering, and cleanup on failure. |
| Scan and health | `MalwareScanning` supplies the scan contract. `MalwareScanning.ClamAv` implements the clamd protocol, options, and health probe. | Catalog maps scanner results to media decisions through `MalwareScannerMediaUploadScanner` and keeps the upload and publication policy. |
| Object operations | `ObjectStorage` defines keys, metadata and stream operations. `ObjectStorage.S3` implements S3 authentication, transport and telemetry. | Catalog generates object keys, owns gallery metadata and associations, and controls variants, publication and reconciliation. |
| Local service topology | `Aspire.Hosting.ClamAv` and `Aspire.Hosting.SeaweedFs` configure the local service resources. | AppHost selects local or external infrastructure and supplies endpoints and credentials through configuration. |

The Catalog application consumes the stream and object contracts. Catalog infrastructure owns the concrete S3 and ClamAV
registrations. The Aspire hosting packages are used by AppHost. The same Catalog workflow and compatibility checks apply
when the deployment binds an external provider to those registrations.

## Configuration

Configure `Catalog:MediaObjectStorage:S3` on every Catalog API and worker that accesses media:

```json
{
  "Catalog": {
    "MediaObjectStorage": {
      "S3": {
        "Endpoint": "https://s3.eu-central-1.amazonaws.com",
        "AuthenticationRegion": "eu-central-1",
        "ForcePathStyle": true,
        "Bucket": "example-catalog-media",
        "AutoProvisionBucket": false,
        "PublicBaseUri": "https://media.example.com/"
      }
    }
  }
}
```

Supply `AccessKey` and `SecretKey` through the deployment secret store, never committed
configuration. The S3 section takes precedence over the existing `SeaweedFs` section.
Pre-provision the private bucket. Grant the application only bucket-prefix listing and object
read/write/delete rights; it needs no bucket-creation permission. Keep public access blocked
and expose published objects through a restricted CDN origin. `PublicBaseUri` is independent
of the authenticated storage endpoint and preserves escaped object-key segments.

## Direct upload tickets

The adapter supports presigned PUT URLs with a positive lifetime of at most seven days.
Tickets bind the application-owned object key and sign both `Content-Type` and exact
`Content-Length`; clients must send the returned required headers. Short lifetimes are
preferred. Expiration can occur sooner when underlying credentials expire. No credentials
are returned to the client. Signed URLs are credentials and must not be logged.

This capability does not introduce a public upload endpoint or bypass the existing server
upload validation, malware scanning, decoding, accessibility review or publication workflow.
A future direct-upload flow must stage objects privately and run those checks before making
objects publishable. Browser clients cannot set `Content-Length` manually; their HTTP stack
must send the known body length. Configure narrowly scoped bucket CORS only if enabling that
flow. Presigned PUT does not offer a size range policy; this adapter signs the exact length.

## Telemetry

The adapter emits `object_storage.*` activities from the `SharedKernel.ObjectStorage.S3` activity source, tagged with the
operation name only. Bucket, object key, URL and request identifiers are never tagged, and HttpClient
instrumentation is suppressed while an operation runs, so path-style URLs cannot reach trace exporters.
AWS SDK metrics stay registered.

## Validation and operations

Offline tests sign URLs with test credentials and assert signed headers, expiry and invalid
request rejection. Existing adapter tests cover object operations, metadata, missing objects,
key validation and paging without a live cloud account. Deployment smoke testing remains
required for IAM, CORS, CDN origin access and provider-specific behavior. Cloud credentials,
service deployment and billable resources are outside the repository change.

For an adapter or provider migration, restore the approved package pin and lock files using the
[SharedKernel compatibility workflow](sharedkernel/development.md). Run the existing SeaweedFS and Catalog
upload integration suites. Before changing the deployed endpoint, verify that the destination contains the
expected objects under the application-owned keys and that type, checksums and publication URLs still match.
Validate scan refusal, timeout, and cancellation before permitting uploads. Keep the previous provider available
for rollback until the new configuration and its object inventory are verified. Compare private inventories in
the deployment environment and use operation names and counts in routine telemetry.

```bash
dotnet test --project tests/ViajantesTurismo.HostedTests --filter-class '*MediaInfrastructureContainerTests'
dotnet test --project tests/ViajantesTurismo.Catalog.ApiServiceTests --filter-class '*MediaUploadOverKestrelTests'
```

References:

- [AWS SDK v4 presigned URL request](https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/S3/TGetPreSignedUrlRequest.html)
- [AWS SDK S3 examples](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/csharp_s3_code_examples.html)
- [S3 presigned URL behavior](https://docs.aws.amazon.com/AmazonS3/latest/userguide/using-presigned-url.html)
