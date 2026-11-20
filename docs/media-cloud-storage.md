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

## SeaweedFS transport and workload identity

Use TLS for the S3 endpoint and for the gateway's connections to the OIDC issuer and its JWKS endpoint.
Issue the S3 leaf certificate from the platform's dedicated storage CA with the S3 service DNS name
in the subject alternative names. The platform manages the CA private key and rotation. The SeaweedFS
workload receives the leaf certificate and its private key through read-only secret mounts. Catalog
receives the public CA bundle in its trust store and verifies the certificate chain and DNS name.
SeaweedFS receives the public CA bundle for the issuer separately through the OIDC provider's
`tlsCaCert` setting when the issuer uses private trust. Roll the CA bundle out to verifying clients
before rotating the leaf to that CA. Rotate the leaf and key together. Check the new trust chain with
fresh connections before removing the previous CA.

The pinned 4.39 gateway reads the minimum protocol from `security.toml`, for example
`min_version = "TLS 1.2"` in `[tls]`. The evaluation runs `weed mini` with `-s3.cert.file` and
`-s3.key.file` pointing at the secret mounts. Standalone `weed s3` uses `-cert.file` and `-key.file`.
With this version, supplying the certificate and key on the normal S3 port and leaving the
separate HTTPS port unset serves HTTPS on the normal port. Configuring a separate HTTPS port
retains the HTTP listener. Publish only the verified S3 endpoint. Keep the SeaweedFS filer,
master, admin and volume interfaces private.
[Pinned S3 listener][seaweed-s3-source] · [SeaweedFS security configuration][seaweed-security]

Register a dedicated workload client with the application identity provider. The issuer must be the
exact HTTPS realm URL and the configured JWKS URI must be the issuer's HTTPS signing-key endpoint.
Give the S3 workload a distinct audience and client ID, such as `catalog-media-storage`. Limit the
accepted issuer and audience in the OIDC provider in `-s3.iam.config`, or `-iam.config` for standalone
`weed s3`. Require a trusted workload claim in the SeaweedFS role trust policy. The evaluation uses
the `roles` claim `catalog-media-writer` from a dedicated Keycloak service account, an exact issuer
condition and the configured federated provider. The deployment must bind that claim to the
authorized Catalog workloads. Ordinary user tokens and application login clients must have no
storage role.

Use `AssumeRoleWithWebIdentity` over HTTPS to exchange the workload token for an access key,
secret key, session token and expiration. Pass all three credential values to the S3 SDK for SigV4
signing. Request a 15-minute session and renew before the returned expiration. The test grants
`GetObject`, `PutObject`, and `DeleteObject` on its dedicated bucket's `catalog/` prefix. For the
application, scope these actions to its actual Catalog key prefixes and grant `ListBucket` only for
the prefixes the inventory and reconciliation jobs read. Pre-provision the bucket with a separate
infrastructure identity. Store any justified static credentials for non-OIDC infrastructure in the
deployment secret store and rotate them independently. The STS signing key is a separate server
secret. Never log workload tokens, STS credentials or presigned URLs. [SeaweedFS OIDC][seaweed-oidc] ·
[STS API][aws-sts]

The default Aspire development stack keeps the bundled SeaweedFS on private endpoints with generated
development-only credentials. Use the hosted test to exercise verified TLS locally without a
machine-wide trust change. It generates a disposable test CA and separate leaf certificates for
Keycloak and SeaweedFS. The test client trusts only that CA in its own certificate chain policy.
The disposable CA has no revocation service. In deployments, use the platform's certificate
revocation policy.
For an external development service or a cross-host deployment, use HTTPS and install the appropriate
public CA bundle. Supply certificates with the real service host names.

Run the evaluation with the pinned containers and the repository's .NET S3 SDK.

```bash
dotnet test --project tests/ViajantesTurismo.HostedTests -- --filter-class '*SeaweedFsWebIdentityTests'
```

The application adapter currently uses configured credentials. Enabling workload identity in a
deployment requires an approved credential provider that acquires and refreshes SeaweedFS sessions
for the adapter, the deployment's issuer and CA configuration, and a staging check from the actual
Catalog API and worker identities.

### ClamAV boundary

`clamd` has no OIDC authentication and its native TCP transport is unauthenticated and unencrypted.
Keep it on a private Aspire endpoint and restrict production ingress to designated scanning
callers. For cross-host traffic, terminate mutually authenticated TLS at a platform proxy or sidecar
and keep the plaintext hop local to the scanner. Apply platform network policy at both ends.
[ClamAV daemon documentation][clamav-scanning]

[seaweed-s3-source]: https://raw.githubusercontent.com/seaweedfs/seaweedfs/db42bb49757b459551607939807017d7a9d5a94a/weed/command/s3.go
[seaweed-security]: https://github.com/seaweedfs/seaweedfs/wiki/Security-Configuration
[seaweed-oidc]: https://github.com/seaweedfs/seaweedfs/wiki/OIDC-Integration
[aws-sts]: https://docs.aws.amazon.com/STS/latest/APIReference/API_AssumeRoleWithWebIdentity.html
[clamav-scanning]: https://docs.clamav.net/manual/Usage/Scanning.html

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
