# Cloud media object storage

Cloud media storage uses AWS S3 as the first provider. The existing AWS SDK and S3-compatible
adapter already support application-owned object keys, object metadata, inventory, reads,
writes and deletion. Reusing that implementation avoids a second storage stack or dependency.
SeaweedFS remains available for local development.

Garage v2.4.1 was rejected on 2026-10-21: the current adapter's upload fails with
`Bad request: Invalid payload signature`. [Compatibility tests](../tests/ViajantesTurismo.Catalog.IntegrationTests/GarageMediaObjectStoreTests.cs)
retain the evidence. Its [AGPL-3.0 license](https://github.com/deuxfleurs-org/garage/blob/main-v2/LICENSE)
and [S3 policy/versioning gaps](https://garagehq.deuxfleurs.fr/documentation/reference-manual/s3-compatibility/)
also require review before reconsideration.

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

## Validation and operations

Offline tests sign URLs with test credentials and assert signed headers, expiry and invalid
request rejection. Existing adapter tests cover object operations, metadata, missing objects,
key validation and paging without a live cloud account. Deployment smoke testing remains
required for IAM, CORS, CDN origin access and provider-specific behavior. Cloud credentials,
service deployment and billable resources are outside the repository change.

References:

- [AWS SDK v4 presigned URL request](https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/S3/TGetPreSignedUrlRequest.html)
- [AWS SDK S3 examples](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/csharp_s3_code_examples.html)
- [S3 presigned URL behavior](https://docs.aws.amazon.com/AmazonS3/latest/userguide/using-presigned-url.html)
