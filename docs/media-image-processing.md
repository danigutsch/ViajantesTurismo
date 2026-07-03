# Media image processing flow

This page documents the Catalog media processing flow.

## Current implemented flow

```mermaid
sequenceDiagram
    participant Upload as Upload intake
    participant Store as IMediaObjectStore
    participant Event as MediaImageOriginalStoredIntegrationEvent
    participant Handler as MediaImageOriginalStoredIntegrationHandler
    participant Images as IPublicMediaImageStore

    Upload->>Store: Put original object
    Upload->>Images: Save metadata as Pending
    Upload->>Event: Publish typed original-stored event
    Event->>Handler: Handle at least once
    Handler->>Store: OpenRead original object
    Handler->>Handler: Normalize and create variants
    Handler->>Store: Put deterministic variant keys
    Handler->>Images: Upsert Ready metadata with variants
```

## Retry and idempotency model

```mermaid
flowchart TD
    A[Processing event delivered] --> B{Already completed by idempotency key?}
    B -->|Yes| C[Skip]
    B -->|No| D[Open original]
    D --> E[Generate thumbnails, icons, and responsive variants]
    E --> F[Write deterministic keys: media/id/version/name.format]
    F --> G[Upsert variant metadata]
    G --> H[Mark processing Ready]
    D -->|Invalid image| I[Mark processing Failed]
```

The typed event id is the idempotency key for inbox-style consumers. Variant object keys use the
media id, processing version, variant name, and format so replay overwrites the same objects instead
of appending duplicate variants.

CloudEvents remains a transport envelope only; the application contract is the typed integration
event.
